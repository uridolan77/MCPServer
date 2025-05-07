using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient; // Changed from System.Data.SqlClient to Microsoft.Data.SqlClient
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MCPServer.Core.Services.Interfaces; // Added for IConnectionStringResolverService

namespace MCPServer.Core.Services.DataTransfer
{
    /// <summary>
    /// Defines the access level for a database connection
    /// </summary>
    public enum ConnectionAccessLevel
    {
        /// <summary>
        /// Connection can be used for reading data only
        /// </summary>
        ReadOnly,

        /// <summary>
        /// Connection can be used for writing data only
        /// </summary>
        WriteOnly,

        /// <summary>
        /// Connection can be used for both reading and writing data
        /// </summary>
        ReadWrite
    }
    public class DataTransferService
    {
        private readonly ILogger<DataTransferService> _logger;
        private readonly IConnectionStringResolverService _connectionStringResolverService; // Added

        // Constructor updated to inject IConnectionStringResolverService
        public DataTransferService(
            ILogger<DataTransferService> logger, 
            IConnectionStringResolverService connectionStringResolverService) // Added
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _connectionStringResolverService = connectionStringResolverService ?? throw new ArgumentNullException(nameof(connectionStringResolverService)); // Added
        }

        public async Task<List<DataTransferConfigurationDto>> GetAllConfigurationsAsync(string connectionString)
        {
            var configurations = new List<DataTransferConfigurationDto>();

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string sql = @"
                    SELECT c.ConfigurationId, c.ConfigurationName, c.Description,
                           c.BatchSize, c.ReportingFrequency, c.IsActive,
                           src.ConnectionId AS SourceConnectionId,
                           src.ConnectionName AS SourceConnectionName,
                           src.ConnectionString AS SourceConnectionString,
                           dst.ConnectionId AS DestinationConnectionId,
                           dst.ConnectionName AS DestinationConnectionName,
                           dst.ConnectionString AS DestinationConnectionString
                    FROM dbo.DataTransferConfigurations c
                    INNER JOIN dbo.DataTransferConnections src ON c.SourceConnectionId = src.ConnectionId
                    INNER JOIN dbo.DataTransferConnections dst ON c.DestinationConnectionId = dst.ConnectionId
                    ORDER BY c.ConfigurationName";

                using (var command = new SqlCommand(sql, connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            configurations.Add(new DataTransferConfigurationDto
                            {
                                ConfigurationId = reader.GetInt32(reader.GetOrdinal("ConfigurationId")),
                                ConfigurationName = reader.GetString(reader.GetOrdinal("ConfigurationName")),
                                Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                                BatchSize = reader.GetInt32(reader.GetOrdinal("BatchSize")),
                                ReportingFrequency = reader.GetInt32(reader.GetOrdinal("ReportingFrequency")),
                                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                                SourceConnection = new ConnectionDto
                                {
                                    ConnectionId = reader.GetInt32(reader.GetOrdinal("SourceConnectionId")),
                                    ConnectionName = reader.GetString(reader.GetOrdinal("SourceConnectionName")),
                                    ConnectionString = reader.GetString(reader.GetOrdinal("SourceConnectionString"))
                                },
                                DestinationConnection = new ConnectionDto
                                {
                                    ConnectionId = reader.GetInt32(reader.GetOrdinal("DestinationConnectionId")),
                                    ConnectionName = reader.GetString(reader.GetOrdinal("DestinationConnectionName")),
                                    ConnectionString = reader.GetString(reader.GetOrdinal("DestinationConnectionString"))
                                }
                            });
                        }
                    }
                }

                // Load table mappings for each configuration
                foreach (var config in configurations)
                {
                    config.TableMappings = await GetTableMappingsAsync(connectionString, config.ConfigurationId);
                    config.Schedules = await GetSchedulesAsync(connectionString, config.ConfigurationId);
                }
            }

            return configurations;
        }

        public async Task<DataTransferConfigurationDto> GetConfigurationAsync(string connectionString, int configurationId)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                // First, get the basic configuration data
                DataTransferConfigurationDto config = null;

                string sql = @"
                    SELECT c.ConfigurationId, c.ConfigurationName, c.Description,
                           c.BatchSize, c.ReportingFrequency, c.IsActive,
                           src.ConnectionId AS SourceConnectionId,
                           src.ConnectionName AS SourceConnectionName,
                           src.ConnectionString AS SourceConnectionString,
                           dst.ConnectionId AS DestinationConnectionId,
                           dst.ConnectionName AS DestinationConnectionName,
                           dst.ConnectionString AS DestinationConnectionString
                    FROM dbo.DataTransferConfigurations c
                    INNER JOIN dbo.DataTransferConnections src ON c.SourceConnectionId = src.ConnectionId
                    INNER JOIN dbo.DataTransferConnections dst ON c.DestinationConnectionId = dst.ConnectionId
                    WHERE c.ConfigurationId = @configId";

                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@configId", configurationId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            config = new DataTransferConfigurationDto
                            {
                                ConfigurationId = reader.GetInt32(reader.GetOrdinal("ConfigurationId")),
                                ConfigurationName = reader.GetString(reader.GetOrdinal("ConfigurationName")),
                                Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                                BatchSize = reader.GetInt32(reader.GetOrdinal("BatchSize")),
                                ReportingFrequency = reader.GetInt32(reader.GetOrdinal("ReportingFrequency")),
                                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                                SourceConnection = new ConnectionDto
                                {
                                    ConnectionId = reader.GetInt32(reader.GetOrdinal("SourceConnectionId")),
                                    ConnectionName = reader.GetString(reader.GetOrdinal("SourceConnectionName")),
                                    ConnectionString = reader.GetString(reader.GetOrdinal("SourceConnectionString"))
                                },
                                DestinationConnection = new ConnectionDto
                                {
                                    ConnectionId = reader.GetInt32(reader.GetOrdinal("DestinationConnectionId")),
                                    ConnectionName = reader.GetString(reader.GetOrdinal("DestinationConnectionName")),
                                    ConnectionString = reader.GetString(reader.GetOrdinal("DestinationConnectionString"))
                                }
                            };
                        }
                    }
                }

                // If we found the configuration, load the table mappings and schedules
                if (config != null)
                {
                    // Load table mappings in a separate query
                    config.TableMappings = await GetTableMappingsAsync(connectionString, config.ConfigurationId);

                    // Load schedules in a separate query
                    config.Schedules = await GetSchedulesAsync(connectionString, config.ConfigurationId);
                }

                return config;
            }
        }

        private async Task<List<TableMappingDto>> GetTableMappingsAsync(string connectionString, int configurationId)
        {
            var mappings = new List<TableMappingDto>();

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string sql = @"
                    SELECT MappingId, ConfigurationId, SchemaName, TableName,
                           TimestampColumnName, OrderByColumn, CustomWhereClause,
                           IsActive, Priority
                    FROM dbo.DataTransferTableMappings
                    WHERE ConfigurationId = @configId
                    ORDER BY Priority, SchemaName, TableName";

                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@configId", configurationId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            mappings.Add(new TableMappingDto
                            {
                                MappingId = reader.GetInt32(reader.GetOrdinal("MappingId")),
                                ConfigurationId = reader.GetInt32(reader.GetOrdinal("ConfigurationId")),
                                SchemaName = reader.GetString(reader.GetOrdinal("SchemaName")),
                                TableName = reader.GetString(reader.GetOrdinal("TableName")),
                                TimestampColumnName = reader.GetString(reader.GetOrdinal("TimestampColumnName")),
                                OrderByColumn = reader.IsDBNull(reader.GetOrdinal("OrderByColumn")) ? null : reader.GetString(reader.GetOrdinal("OrderByColumn")),
                                CustomWhereClause = reader.IsDBNull(reader.GetOrdinal("CustomWhereClause")) ? null : reader.GetString(reader.GetOrdinal("CustomWhereClause")),
                                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                                Priority = reader.GetInt32(reader.GetOrdinal("Priority"))
                            });
                        }
                    }
                }
            }

            return mappings;
        }

        private async Task<List<ScheduleDto>> GetSchedulesAsync(string connectionString, int configurationId)
        {
            var schedules = new List<ScheduleDto>();

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string sql = @"
                    SELECT ScheduleId, ConfigurationId, ScheduleType, StartTime,
                           Frequency, FrequencyUnit, WeekDays, MonthDays,
                           IsActive, LastRunTime, NextRunTime
                    FROM dbo.DataTransferSchedule
                    WHERE ConfigurationId = @configId
                    ORDER BY IsActive DESC, NextRunTime";

                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@configId", configurationId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            schedules.Add(new ScheduleDto
                            {
                                ScheduleId = reader.GetInt32(reader.GetOrdinal("ScheduleId")),
                                ConfigurationId = reader.GetInt32(reader.GetOrdinal("ConfigurationId")),
                                ScheduleType = reader.GetString(reader.GetOrdinal("ScheduleType")),
                                StartTime = reader.IsDBNull(reader.GetOrdinal("StartTime")) ? (TimeSpan?)null : reader.GetTimeSpan(reader.GetOrdinal("StartTime")),
                                Frequency = reader.IsDBNull(reader.GetOrdinal("Frequency")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("Frequency")),
                                FrequencyUnit = reader.IsDBNull(reader.GetOrdinal("FrequencyUnit")) ? null : reader.GetString(reader.GetOrdinal("FrequencyUnit")),
                                WeekDays = reader.IsDBNull(reader.GetOrdinal("WeekDays")) ? null : reader.GetString(reader.GetOrdinal("WeekDays")),
                                MonthDays = reader.IsDBNull(reader.GetOrdinal("MonthDays")) ? null : reader.GetString(reader.GetOrdinal("MonthDays")),
                                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                                LastRunTime = reader.IsDBNull(reader.GetOrdinal("LastRunTime")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("LastRunTime")),
                                NextRunTime = reader.IsDBNull(reader.GetOrdinal("NextRunTime")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("NextRunTime"))
                            });
                        }
                    }
                }
            }

            return schedules;
        }

        public async Task<ConnectionDto> GetConnectionAsync(string connectionString, int connectionId)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                // Check if the ConnectionAccessLevel column exists
                bool hasAccessLevelColumn = false;
                bool hasLastTestedOnColumn = false;

                string checkColumnsSql = @"
                    SELECT
                        COUNT(*) AS ColumnCount
                    FROM
                        INFORMATION_SCHEMA.COLUMNS
                    WHERE
                        TABLE_NAME = 'DataTransferConnections'
                        AND COLUMN_NAME = 'ConnectionAccessLevel'";

                using (var checkCommand = new SqlCommand(checkColumnsSql, connection))
                {
                    var result = await checkCommand.ExecuteScalarAsync();
                    hasAccessLevelColumn = Convert.ToInt32(result) > 0;
                }

                checkColumnsSql = @"
                    SELECT
                        COUNT(*) AS ColumnCount
                    FROM
                        INFORMATION_SCHEMA.COLUMNS
                    WHERE
                        TABLE_NAME = 'DataTransferConnections'
                        AND COLUMN_NAME = 'LastTestedOn'";

                using (var checkCommand = new SqlCommand(checkColumnsSql, connection))
                {
                    var result = await checkCommand.ExecuteScalarAsync();
                    hasLastTestedOnColumn = Convert.ToInt32(result) > 0;
                }

                string sql;

                if (hasAccessLevelColumn)
                {
                    // Use the new schema with ConnectionAccessLevel
                    sql = @"
                        SELECT ConnectionId, ConnectionName, ConnectionString, Description,
                               ConnectionAccessLevel, IsActive, LastTestedOn, CreatedOn, CreatedBy,
                               LastModifiedOn, LastModifiedBy,
                               ISNULL(MaxPoolSize, 100) AS MaxPoolSize,
                               ISNULL(MinPoolSize, 5) AS MinPoolSize,
                               ISNULL(Timeout, 30) AS Timeout,
                               ISNULL(Encrypt, 1) AS Encrypt,
                               ISNULL(TrustServerCertificate, 1) AS TrustServerCertificate
                        FROM dbo.DataTransferConnections
                        WHERE ConnectionId = @connectionId";
                }
                else
                {
                    // Use the old schema with IsSource and IsDestination
                    sql = @"
                        SELECT ConnectionId, ConnectionName, ConnectionString, Description,
                               IsSource, IsDestination, IsActive
                        FROM dbo.DataTransferConnections
                        WHERE ConnectionId = @connectionId";
                }

                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@connectionId", connectionId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            // Get the connection string directly - it's either a template with Key Vault placeholders
                            // or a direct connection string
                            string connectionStringValue = reader.GetString(reader.GetOrdinal("ConnectionString"));

                            var connectionDto = new ConnectionDto
                            {
                                ConnectionId = reader.GetInt32(reader.GetOrdinal("ConnectionId")),
                                ConnectionName = reader.GetString(reader.GetOrdinal("ConnectionName")),
                                ConnectionString = connectionStringValue,
                                Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? string.Empty : reader.GetString(reader.GetOrdinal("Description")),
                                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
                            };

                            if (hasAccessLevelColumn)
                            {
                                string accessLevel = reader.GetString(reader.GetOrdinal("ConnectionAccessLevel"));
                                connectionDto.ConnectionAccessLevel = Enum.Parse<ConnectionAccessLevel>(accessLevel);

                                if (hasLastTestedOnColumn && !reader.IsDBNull(reader.GetOrdinal("LastTestedOn")))
                                {
                                    connectionDto.LastTestedOn = reader.GetDateTime(reader.GetOrdinal("LastTestedOn"));
                                }

                                // Read additional fields if they exist
                                if (!reader.IsDBNull(reader.GetOrdinal("CreatedOn")))
                                {
                                    connectionDto.CreatedOn = reader.GetDateTime(reader.GetOrdinal("CreatedOn"));
                                }

                                if (!reader.IsDBNull(reader.GetOrdinal("CreatedBy")))
                                {
                                    connectionDto.CreatedBy = reader.GetString(reader.GetOrdinal("CreatedBy"));
                                }

                                if (!reader.IsDBNull(reader.GetOrdinal("LastModifiedOn")))
                                {
                                    connectionDto.LastModifiedOn = reader.GetDateTime(reader.GetOrdinal("LastModifiedOn"));
                                }

                                if (!reader.IsDBNull(reader.GetOrdinal("LastModifiedBy")))
                                {
                                    connectionDto.LastModifiedBy = reader.GetString(reader.GetOrdinal("LastModifiedBy"));
                                }

                                // Read connection pool settings
                                connectionDto.MaxPoolSize = reader.GetInt32(reader.GetOrdinal("MaxPoolSize"));
                                connectionDto.MinPoolSize = reader.GetInt32(reader.GetOrdinal("MinPoolSize"));
                                connectionDto.Timeout = reader.GetInt32(reader.GetOrdinal("Timeout"));
                                connectionDto.Encrypt = reader.GetBoolean(reader.GetOrdinal("Encrypt"));
                                connectionDto.TrustServerCertificate = reader.GetBoolean(reader.GetOrdinal("TrustServerCertificate"));
                            }
                            else
                            {
                                // Set ConnectionAccessLevel based on IsSource and IsDestination
                                bool isSourceValue = reader.GetBoolean(reader.GetOrdinal("IsSource"));
                                bool isDestinationValue = reader.GetBoolean(reader.GetOrdinal("IsDestination"));

                                if (isSourceValue && isDestinationValue)
                                    connectionDto.ConnectionAccessLevel = ConnectionAccessLevel.ReadWrite;
                                else if (isSourceValue)
                                    connectionDto.ConnectionAccessLevel = ConnectionAccessLevel.ReadOnly;
                                else if (isDestinationValue)
                                    connectionDto.ConnectionAccessLevel = ConnectionAccessLevel.WriteOnly;
                            }

                            return connectionDto;
                        }
                    }
                }
            }

            return null;
        }

        public async Task<List<ConnectionDto>> GetConnectionsAsync(string connectionString, bool? isSource = null, bool? isDestination = null, bool? isActive = true)
        {
            var connections = new List<ConnectionDto>();

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                // Check if the ConnectionAccessLevel column exists
                bool hasAccessLevelColumn = false;
                bool hasLastTestedOnColumn = false;

                string checkColumnsSql = @"
                    SELECT
                        COUNT(*) AS ColumnCount
                    FROM
                        INFORMATION_SCHEMA.COLUMNS
                    WHERE
                        TABLE_NAME = 'DataTransferConnections'
                        AND COLUMN_NAME = 'ConnectionAccessLevel'";

                using (var checkCommand = new SqlCommand(checkColumnsSql, connection))
                {
                    var result = await checkCommand.ExecuteScalarAsync();
                    hasAccessLevelColumn = Convert.ToInt32(result) > 0;
                }

                checkColumnsSql = @"
                    SELECT
                        COUNT(*) AS ColumnCount
                    FROM
                        INFORMATION_SCHEMA.COLUMNS
                    WHERE
                        TABLE_NAME = 'DataTransferConnections'
                        AND COLUMN_NAME = 'LastTestedOn'";

                using (var checkCommand = new SqlCommand(checkColumnsSql, connection))
                {
                    var result = await checkCommand.ExecuteScalarAsync();
                    hasLastTestedOnColumn = Convert.ToInt32(result) > 0;
                }

                string sql;

                if (hasAccessLevelColumn)
                {
                    // Use the new schema with ConnectionAccessLevel
                    sql = @"
                        SELECT ConnectionId, ConnectionName, ConnectionString, Description,
                               ConnectionAccessLevel, IsActive, LastTestedOn, CreatedOn, CreatedBy,
                               LastModifiedOn, LastModifiedBy,
                               ISNULL(MaxPoolSize, 100) AS MaxPoolSize,
                               ISNULL(MinPoolSize, 5) AS MinPoolSize,
                               ISNULL(Timeout, 30) AS Timeout,
                               ISNULL(Encrypt, 1) AS Encrypt,
                               ISNULL(TrustServerCertificate, 1) AS TrustServerCertificate
                        FROM dbo.DataTransferConnections
                        WHERE (@isActive IS NULL OR IsActive = @isActive)";

                    // Add filters for source/destination based on ConnectionAccessLevel
                    if (isSource.HasValue)
                    {
                        if (isSource.Value)
                        {
                            sql += " AND (ConnectionAccessLevel = 'ReadOnly' OR ConnectionAccessLevel = 'ReadWrite')";
                        }
                        else
                        {
                            sql += " AND (ConnectionAccessLevel = 'WriteOnly')";
                        }
                    }

                    if (isDestination.HasValue)
                    {
                        if (isDestination.Value)
                        {
                            sql += " AND (ConnectionAccessLevel = 'WriteOnly' OR ConnectionAccessLevel = 'ReadWrite')";
                        }
                        else
                        {
                            sql += " AND (ConnectionAccessLevel = 'ReadOnly')";
                        }
                    }

                    sql += " ORDER BY ConnectionName";
                }
                else
                {
                    // Use the old schema with IsSource and IsDestination
                    sql = @"
                        SELECT ConnectionId, ConnectionName, ConnectionString, Description,
                               IsSource, IsDestination, IsActive
                        FROM dbo.DataTransferConnections
                        WHERE (@isSource IS NULL OR IsSource = @isSource)
                          AND (@isDestination IS NULL OR IsDestination = @isDestination)
                          AND (@isActive IS NULL OR IsActive = @isActive)
                        ORDER BY ConnectionName";
                }

                using (var command = new SqlCommand(sql, connection))
                {
                    if (!hasAccessLevelColumn)
                    {
                        command.Parameters.AddWithValue("@isSource", isSource ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@isDestination", isDestination ?? (object)DBNull.Value);
                    }
                    command.Parameters.AddWithValue("@isActive", isActive ?? (object)DBNull.Value);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            // Get the connection string directly - it's either a template with Key Vault placeholders
                            // or a direct connection string
                            string connectionStringValue = reader.GetString(reader.GetOrdinal("ConnectionString"));

                            var connectionDto = new ConnectionDto
                            {
                                ConnectionId = reader.GetInt32(reader.GetOrdinal("ConnectionId")),
                                ConnectionName = reader.GetString(reader.GetOrdinal("ConnectionName")),
                                ConnectionString = connectionStringValue,
                                Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? string.Empty : reader.GetString(reader.GetOrdinal("Description")),
                                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
                            };

                            if (hasAccessLevelColumn)
                            {
                                string accessLevel = reader.GetString(reader.GetOrdinal("ConnectionAccessLevel"));
                                connectionDto.ConnectionAccessLevel = Enum.Parse<ConnectionAccessLevel>(accessLevel);

                                if (hasLastTestedOnColumn && !reader.IsDBNull(reader.GetOrdinal("LastTestedOn")))
                                {
                                    connectionDto.LastTestedOn = reader.GetDateTime(reader.GetOrdinal("LastTestedOn"));
                                }

                                // Read additional fields if they exist
                                if (!reader.IsDBNull(reader.GetOrdinal("CreatedOn")))
                                {
                                    connectionDto.CreatedOn = reader.GetDateTime(reader.GetOrdinal("CreatedOn"));
                                }

                                if (!reader.IsDBNull(reader.GetOrdinal("CreatedBy")))
                                {
                                    connectionDto.CreatedBy = reader.GetString(reader.GetOrdinal("CreatedBy"));
                                }

                                if (!reader.IsDBNull(reader.GetOrdinal("LastModifiedOn")))
                                {
                                    connectionDto.LastModifiedOn = reader.GetDateTime(reader.GetOrdinal("LastModifiedOn"));
                                }

                                if (!reader.IsDBNull(reader.GetOrdinal("LastModifiedBy")))
                                {
                                    connectionDto.LastModifiedBy = reader.GetString(reader.GetOrdinal("LastModifiedBy"));
                                }

                                // Read connection pool settings
                                connectionDto.MaxPoolSize = reader.GetInt32(reader.GetOrdinal("MaxPoolSize"));
                                connectionDto.MinPoolSize = reader.GetInt32(reader.GetOrdinal("MinPoolSize"));
                                connectionDto.Timeout = reader.GetInt32(reader.GetOrdinal("Timeout"));
                                connectionDto.Encrypt = reader.GetBoolean(reader.GetOrdinal("Encrypt"));
                                connectionDto.TrustServerCertificate = reader.GetBoolean(reader.GetOrdinal("TrustServerCertificate"));
                            }
                            else
                            {
                                // Set ConnectionAccessLevel based on IsSource and IsDestination
                                bool isSourceValue = reader.GetBoolean(reader.GetOrdinal("IsSource"));
                                bool isDestinationValue = reader.GetBoolean(reader.GetOrdinal("IsDestination"));

                                if (isSourceValue && isDestinationValue)
                                    connectionDto.ConnectionAccessLevel = ConnectionAccessLevel.ReadWrite;
                                else if (isSourceValue)
                                    connectionDto.ConnectionAccessLevel = ConnectionAccessLevel.ReadOnly;
                                else if (isDestinationValue)
                                    connectionDto.ConnectionAccessLevel = ConnectionAccessLevel.WriteOnly;
                            }

                            connections.Add(connectionDto);
                        }
                    }
                }
            }

            return connections;
        }

        public async Task<int> SaveConnectionAsync(string connectionString, ConnectionDto connectionDto, string username)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                // Check if a connection with the same name already exists
                if (connectionDto.ConnectionId <= 0)
                {
                    string checkSql = @"
                        SELECT ConnectionId FROM dbo.DataTransferConnections
                        WHERE ConnectionName = @name";

                    using (var command = new SqlCommand(checkSql, connection))
                    {
                        command.Parameters.AddWithValue("@name", connectionDto.ConnectionName);
                        var existingId = await command.ExecuteScalarAsync();

                        if (existingId != null && existingId != DBNull.Value)
                        {
                            // Connection with this name already exists, set the ID so we update it instead
                            connectionDto.ConnectionId = Convert.ToInt32(existingId);
                            _logger.LogInformation("Connection with name {ConnectionName} already exists with ID {ConnectionId}. Updating instead of creating new.",
                                connectionDto.ConnectionName, connectionDto.ConnectionId);
                        }
                    }
                }

                // Check if the ConnectionAccessLevel column exists
                bool hasAccessLevelColumn = false;
                bool hasLastTestedOnColumn = false;

                string checkColumnsSql = @"
                    SELECT
                        COUNT(*) AS ColumnCount
                    FROM
                        INFORMATION_SCHEMA.COLUMNS
                    WHERE
                        TABLE_NAME = 'DataTransferConnections'
                        AND COLUMN_NAME = 'ConnectionAccessLevel'";

                using (var checkCommand = new SqlCommand(checkColumnsSql, connection))
                {
                    var result = await checkCommand.ExecuteScalarAsync();
                    hasAccessLevelColumn = Convert.ToInt32(result) > 0;
                }

                checkColumnsSql = @"
                    SELECT
                        COUNT(*) AS ColumnCount
                    FROM
                        INFORMATION_SCHEMA.COLUMNS
                    WHERE
                        TABLE_NAME = 'DataTransferConnections'
                        AND COLUMN_NAME = 'LastTestedOn'";

                using (var checkCommand = new SqlCommand(checkColumnsSql, connection))
                {
                    var result = await checkCommand.ExecuteScalarAsync();
                    hasLastTestedOnColumn = Convert.ToInt32(result) > 0;
                }

                if (connectionDto.ConnectionId > 0)
                {
                    // Update existing connection
                    string sql;

                    if (hasAccessLevelColumn)
                    {
                        sql = @"
                            UPDATE dbo.DataTransferConnections
                            SET ConnectionName = @name,
                                ConnectionString = @connString,
                                Description = @description,
                                ConnectionAccessLevel = @accessLevel,
                                IsActive = @isActive,
                                MaxPoolSize = @maxPoolSize,
                                MinPoolSize = @minPoolSize,
                                Timeout = @timeout,
                                Encrypt = @encrypt,
                                TrustServerCertificate = @trustServerCert,";

                        if (hasLastTestedOnColumn && connectionDto.LastTestedOn.HasValue)
                        {
                            sql += @"
                                LastTestedOn = @lastTestedOn,";
                        }

                        sql += @"
                                LastModifiedOn = GETUTCDATE(),
                                LastModifiedBy = @username
                            WHERE ConnectionId = @id";
                    }
                    else
                    {
                        // Use old schema with IsSource and IsDestination
                        sql = @"
                            UPDATE dbo.DataTransferConnections
                            SET ConnectionName = @name,
                                ConnectionString = @connString,
                                Description = @description,
                                IsSource = @isSource,
                                IsDestination = @isDestination,
                                IsActive = @isActive,
                                LastModifiedOn = GETUTCDATE(),
                                LastModifiedBy = @username
                            WHERE ConnectionId = @id";
                    }

                    using (var command = new SqlCommand(sql, connection))
                    {
                        // The ConnectionString will be saved as-is (either a template with Key Vault placeholders 
                        // or a direct connection string)
                        string connectionStringToSave = connectionDto.ConnectionString;
                        _logger.LogInformation("Saving connection string for {ConnectionName}", connectionDto.ConnectionName);

                        command.Parameters.AddWithValue("@id", connectionDto.ConnectionId);
                        command.Parameters.AddWithValue("@name", connectionDto.ConnectionName);
                        command.Parameters.AddWithValue("@connString", connectionStringToSave);
                        command.Parameters.AddWithValue("@description", (object)connectionDto.Description ?? DBNull.Value);
                        command.Parameters.AddWithValue("@isActive", connectionDto.IsActive);
                        command.Parameters.AddWithValue("@username", username);
                        command.Parameters.AddWithValue("@maxPoolSize", connectionDto.MaxPoolSize);
                        command.Parameters.AddWithValue("@minPoolSize", connectionDto.MinPoolSize);
                        command.Parameters.AddWithValue("@timeout", connectionDto.Timeout);
                        command.Parameters.AddWithValue("@encrypt", connectionDto.Encrypt);
                        command.Parameters.AddWithValue("@trustServerCert", connectionDto.TrustServerCertificate);

                        if (hasAccessLevelColumn)
                        {
                            command.Parameters.AddWithValue("@accessLevel", connectionDto.ConnectionAccessLevel.ToString());

                            if (hasLastTestedOnColumn && connectionDto.LastTestedOn.HasValue)
                            {
                                command.Parameters.AddWithValue("@lastTestedOn", connectionDto.LastTestedOn.Value);
                            }
                        }
                        else
                        {
                            // Use legacy properties
                            command.Parameters.AddWithValue("@isSource", connectionDto.ConnectionAccessLevel == ConnectionAccessLevel.ReadOnly || connectionDto.ConnectionAccessLevel == ConnectionAccessLevel.ReadWrite);
                            command.Parameters.AddWithValue("@isDestination", connectionDto.ConnectionAccessLevel == ConnectionAccessLevel.WriteOnly || connectionDto.ConnectionAccessLevel == ConnectionAccessLevel.ReadWrite);
                        }

                        await command.ExecuteNonQueryAsync();
                        return connectionDto.ConnectionId;
                    }
                }
                else
                {
                    // Insert new connection
                    string sql;

                    if (hasAccessLevelColumn)
                    {
                        sql = @"
                            INSERT INTO dbo.DataTransferConnections
                                (ConnectionName, ConnectionString, Description, ConnectionAccessLevel, IsActive,
                                 MaxPoolSize, MinPoolSize, Timeout, Encrypt, TrustServerCertificate";

                        if (hasLastTestedOnColumn && connectionDto.LastTestedOn.HasValue)
                        {
                            sql += ", LastTestedOn";
                        }

                        sql += @", CreatedBy, CreatedOn)
                            VALUES
                                (@name, @connString, @description, @accessLevel, @isActive,
                                 @maxPoolSize, @minPoolSize, @timeout, @encrypt, @trustServerCert";

                        if (hasLastTestedOnColumn && connectionDto.LastTestedOn.HasValue)
                        {
                            sql += ", @lastTestedOn";
                        }

                        sql += @", @username, GETUTCDATE());
                            SELECT SCOPE_IDENTITY();";
                    }
                    else
                    {
                        // Use old schema with IsSource and IsDestination
                        sql = @"
                            INSERT INTO dbo.DataTransferConnections
                                (ConnectionName, ConnectionString, Description, IsSource, IsDestination, IsActive, CreatedBy, CreatedOn)
                            VALUES
                                (@name, @connString, @description, @isSource, @isDestination, @isActive, @username, GETUTCDATE());
                            SELECT SCOPE_IDENTITY();";
                    }

                    using (var command = new SqlCommand(sql, connection))
                    {
                        // The ConnectionString will be saved as-is (either a template with Key Vault placeholders 
                        // or a direct connection string)
                        string connectionStringToSave = connectionDto.ConnectionString;
                        _logger.LogInformation("Saving connection string for {ConnectionName}", connectionDto.ConnectionName);

                        command.Parameters.AddWithValue("@name", connectionDto.ConnectionName);
                        command.Parameters.AddWithValue("@connString", connectionStringToSave);
                        command.Parameters.AddWithValue("@description", (object)connectionDto.Description ?? DBNull.Value);
                        command.Parameters.AddWithValue("@isActive", connectionDto.IsActive);
                        command.Parameters.AddWithValue("@username", username);
                        command.Parameters.AddWithValue("@maxPoolSize", connectionDto.MaxPoolSize);
                        command.Parameters.AddWithValue("@minPoolSize", connectionDto.MinPoolSize);
                        command.Parameters.AddWithValue("@timeout", connectionDto.Timeout);
                        command.Parameters.AddWithValue("@encrypt", connectionDto.Encrypt);
                        command.Parameters.AddWithValue("@trustServerCert", connectionDto.TrustServerCertificate);

                        if (hasAccessLevelColumn)
                        {
                            command.Parameters.AddWithValue("@accessLevel", connectionDto.ConnectionAccessLevel.ToString());

                            if (hasLastTestedOnColumn && connectionDto.LastTestedOn.HasValue)
                            {
                                command.Parameters.AddWithValue("@lastTestedOn", connectionDto.LastTestedOn.Value);
                            }
                        }
                        else
                        {
                            // Use legacy properties
                            command.Parameters.AddWithValue("@isSource", connectionDto.ConnectionAccessLevel == ConnectionAccessLevel.ReadOnly || connectionDto.ConnectionAccessLevel == ConnectionAccessLevel.ReadWrite);
                            command.Parameters.AddWithValue("@isDestination", connectionDto.ConnectionAccessLevel == ConnectionAccessLevel.WriteOnly || connectionDto.ConnectionAccessLevel == ConnectionAccessLevel.ReadWrite);
                        }

                        return Convert.ToInt32(await command.ExecuteScalarAsync());
                    }
                }
            }
        }

        public async Task<int> SaveConfigurationAsync(string connectionString, DataTransferConfigurationDto configDto, string username)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        int configId;

                        if (configDto.ConfigurationId > 0)
                        {
                            // Update existing configuration
                            string sqlConfig = @"
                                UPDATE dbo.DataTransferConfigurations
                                SET ConfigurationName = @name,
                                    Description = @description,
                                    SourceConnectionId = @sourceId,
                                    DestinationConnectionId = @destId,
                                    BatchSize = @batchSize,
                                    ReportingFrequency = @reportFreq,
                                    IsActive = @isActive,
                                    LastModifiedOn = GETUTCDATE(),
                                    LastModifiedBy = @username
                                WHERE ConfigurationId = @id";

                            using (var command = new SqlCommand(sqlConfig, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@id", configDto.ConfigurationId);
                                command.Parameters.AddWithValue("@name", configDto.ConfigurationName);
                                command.Parameters.AddWithValue("@description", (object)configDto.Description ?? DBNull.Value);
                                command.Parameters.AddWithValue("@sourceId", configDto.SourceConnection.ConnectionId);
                                command.Parameters.AddWithValue("@destId", configDto.DestinationConnection.ConnectionId);
                                command.Parameters.AddWithValue("@batchSize", configDto.BatchSize);
                                command.Parameters.AddWithValue("@reportFreq", configDto.ReportingFrequency);
                                command.Parameters.AddWithValue("@isActive", configDto.IsActive);
                                command.Parameters.AddWithValue("@username", username);

                                await command.ExecuteNonQueryAsync();
                            }

                            configId = configDto.ConfigurationId;
                        }
                        else
                        {
                            // Insert new configuration
                            string sqlConfig = @"
                                INSERT INTO dbo.DataTransferConfigurations
                                    (ConfigurationName, Description, SourceConnectionId, DestinationConnectionId,
                                     BatchSize, ReportingFrequency, IsActive, CreatedBy, CreatedOn)
                                VALUES
                                    (@name, @description, @sourceId, @destId,
                                     @batchSize, @reportFreq, @isActive, @username, GETUTCDATE());
                                SELECT SCOPE_IDENTITY();";

                            using (var command = new SqlCommand(sqlConfig, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@name", configDto.ConfigurationName);
                                command.Parameters.AddWithValue("@description", (object)configDto.Description ?? DBNull.Value);
                                command.Parameters.AddWithValue("@sourceId", configDto.SourceConnection.ConnectionId);
                                command.Parameters.AddWithValue("@destId", configDto.DestinationConnection.ConnectionId);
                                command.Parameters.AddWithValue("@batchSize", configDto.BatchSize);
                                command.Parameters.AddWithValue("@reportFreq", configDto.ReportingFrequency);
                                command.Parameters.AddWithValue("@isActive", configDto.IsActive);
                                command.Parameters.AddWithValue("@username", username);

                                configId = Convert.ToInt32(await command.ExecuteScalarAsync());
                            }
                        }

                        // If updating, remove existing table mappings that are no longer in the configuration
                        if (configDto.ConfigurationId > 0)
                        {
                            string existingMappingsQuery = @"
                                SELECT MappingId FROM dbo.DataTransferTableMappings
                                WHERE ConfigurationId = @configId";

                            var existingMappingIds = new HashSet<int>();
                            using (var command = new SqlCommand(existingMappingsQuery, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@configId", configId);
                                using (var reader = await command.ExecuteReaderAsync())
                                {
                                    while (await reader.ReadAsync())
                                    {
                                        existingMappingIds.Add(reader.GetInt32(0));
                                    }
                                }
                            }

                            var updatedMappingIds = configDto.TableMappings
                                .Where(m => m.MappingId > 0)
                                .Select(m => m.MappingId)
                                .ToHashSet();

                            var mappingsToDelete = existingMappingIds.Except(updatedMappingIds);
                            if (mappingsToDelete.Any())
                            {
                                foreach (var mappingId in mappingsToDelete)
                                {
                                    string deleteMapping = "DELETE FROM dbo.DataTransferTableMappings WHERE MappingId = @id";
                                    using (var command = new SqlCommand(deleteMapping, connection, transaction))
                                    {
                                        command.Parameters.AddWithValue("@id", mappingId);
                                        await command.ExecuteNonQueryAsync();
                                    }
                                }
                            }
                        }

                        // Save table mappings
                        foreach (var mapping in configDto.TableMappings)
                        {
                            if (mapping.MappingId > 0)
                            {
                                // Update existing mapping
                                string sqlMapping = @"
                                    UPDATE dbo.DataTransferTableMappings
                                    SET SchemaName = @schema,
                                        TableName = @table,
                                        TimestampColumnName = @timestamp,
                                        OrderByColumn = @orderBy,
                                        CustomWhereClause = @where,
                                        IsActive = @isActive,
                                        Priority = @priority,
                                        LastModifiedOn = GETUTCDATE(),
                                        LastModifiedBy = @username
                                    WHERE MappingId = @id";

                                using (var command = new SqlCommand(sqlMapping, connection, transaction))
                                {
                                    command.Parameters.AddWithValue("@id", mapping.MappingId);
                                    command.Parameters.AddWithValue("@schema", mapping.SchemaName);
                                    command.Parameters.AddWithValue("@table", mapping.TableName);
                                    command.Parameters.AddWithValue("@timestamp", mapping.TimestampColumnName);
                                    command.Parameters.AddWithValue("@orderBy", (object)mapping.OrderByColumn ?? DBNull.Value);
                                    command.Parameters.AddWithValue("@where", (object)mapping.CustomWhereClause ?? DBNull.Value);
                                    command.Parameters.AddWithValue("@isActive", mapping.IsActive);
                                    command.Parameters.AddWithValue("@priority", mapping.Priority);
                                    command.Parameters.AddWithValue("@username", username);

                                    await command.ExecuteNonQueryAsync();
                                }
                            }
                            else
                            {
                                // Insert new mapping
                                string sqlMapping = @"
                                    INSERT INTO dbo.DataTransferTableMappings
                                        (ConfigurationId, SchemaName, TableName, TimestampColumnName,
                                         OrderByColumn, CustomWhereClause, IsActive, Priority, CreatedBy, CreatedOn)
                                    VALUES
                                        (@configId, @schema, @table, @timestamp,
                                         @orderBy, @where, @isActive, @priority, @username, GETUTCDATE());";

                                using (var command = new SqlCommand(sqlMapping, connection, transaction))
                                {
                                    command.Parameters.AddWithValue("@configId", configId);
                                    command.Parameters.AddWithValue("@schema", mapping.SchemaName);
                                    command.Parameters.AddWithValue("@table", mapping.TableName);
                                    command.Parameters.AddWithValue("@timestamp", mapping.TimestampColumnName);
                                    command.Parameters.AddWithValue("@orderBy", (object)mapping.OrderByColumn ?? DBNull.Value);
                                    command.Parameters.AddWithValue("@where", (object)mapping.CustomWhereClause ?? DBNull.Value);
                                    command.Parameters.AddWithValue("@isActive", mapping.IsActive);
                                    command.Parameters.AddWithValue("@priority", mapping.Priority);
                                    command.Parameters.AddWithValue("@username", username);

                                    await command.ExecuteNonQueryAsync();
                                }
                            }
                        }

                        // Similar handling for schedules - remove those no longer present
                        if (configDto.ConfigurationId > 0 && configDto.Schedules != null)
                        {
                            string existingSchedulesQuery = @"
                                SELECT ScheduleId FROM dbo.DataTransferSchedule
                                WHERE ConfigurationId = @configId";

                            var existingScheduleIds = new HashSet<int>();
                            using (var command = new SqlCommand(existingSchedulesQuery, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@configId", configId);
                                using (var reader = await command.ExecuteReaderAsync())
                                {
                                    while (await reader.ReadAsync())
                                    {
                                        existingScheduleIds.Add(reader.GetInt32(0));
                                    }
                                }
                            }

                            var updatedScheduleIds = configDto.Schedules
                                .Where(s => s.ScheduleId > 0)
                                .Select(s => s.ScheduleId)
                                .ToHashSet();

                            var schedulesToDelete = existingScheduleIds.Except(updatedScheduleIds);
                            if (schedulesToDelete.Any())
                            {
                                foreach (var scheduleId in schedulesToDelete)
                                {
                                    string deleteSchedule = "DELETE FROM dbo.DataTransferSchedule WHERE ScheduleId = @id";
                                    using (var command = new SqlCommand(deleteSchedule, connection, transaction))
                                    {
                                        command.Parameters.AddWithValue("@id", scheduleId);
                                        await command.ExecuteNonQueryAsync();
                                    }
                                }
                            }
                        }

                        // Save schedules
                        if (configDto.Schedules != null)
                        {
                            foreach (var schedule in configDto.Schedules)
                            {
                                if (schedule.ScheduleId > 0)
                                {
                                    // Update existing schedule
                                    string sqlSchedule = @"
                                        UPDATE dbo.DataTransferSchedule
                                        SET ScheduleType = @type,
                                            StartTime = @startTime,
                                            Frequency = @frequency,
                                            FrequencyUnit = @freqUnit,
                                            WeekDays = @weekDays,
                                            MonthDays = @monthDays,
                                            IsActive = @isActive,
                                            NextRunTime = @nextRun,
                                            LastModifiedOn = GETUTCDATE(),
                                            LastModifiedBy = @username
                                        WHERE ScheduleId = @id";

                                    using (var command = new SqlCommand(sqlSchedule, connection, transaction))
                                    {
                                        command.Parameters.AddWithValue("@id", schedule.ScheduleId);
                                        command.Parameters.AddWithValue("@type", schedule.ScheduleType);
                                        command.Parameters.AddWithValue("@startTime", schedule.StartTime.HasValue ? (object)schedule.StartTime.Value : DBNull.Value);
                                        command.Parameters.AddWithValue("@frequency", schedule.Frequency.HasValue ? (object)schedule.Frequency.Value : DBNull.Value);
                                        command.Parameters.AddWithValue("@freqUnit", (object)schedule.FrequencyUnit ?? DBNull.Value);
                                        command.Parameters.AddWithValue("@weekDays", (object)schedule.WeekDays ?? DBNull.Value);
                                        command.Parameters.AddWithValue("@monthDays", (object)schedule.MonthDays ?? DBNull.Value);
                                        command.Parameters.AddWithValue("@isActive", schedule.IsActive);
                                        command.Parameters.AddWithValue("@nextRun", schedule.NextRunTime.HasValue ? (object)schedule.NextRunTime.Value : DBNull.Value);
                                        command.Parameters.AddWithValue("@username", username);

                                        await command.ExecuteNonQueryAsync();
                                    }
                                }
                                else
                                {
                                    // Insert new schedule
                                    string sqlSchedule = @"
                                        INSERT INTO dbo.DataTransferSchedule
                                            (ConfigurationId, ScheduleType, StartTime, Frequency, FrequencyUnit,
                                             WeekDays, MonthDays, IsActive, NextRunTime, CreatedBy, CreatedOn)
                                        VALUES
                                            (@configId, @type, @startTime, @frequency, @freqUnit,
                                             @weekDays, @monthDays, @isActive, @nextRun, @username, GETUTCDATE());";

                                    using (var command = new SqlCommand(sqlSchedule, connection, transaction))
                                    {
                                        command.Parameters.AddWithValue("@configId", configId);
                                        command.Parameters.AddWithValue("@type", schedule.ScheduleType);
                                        command.Parameters.AddWithValue("@startTime", schedule.StartTime.HasValue ? (object)schedule.StartTime.Value : DBNull.Value);
                                        command.Parameters.AddWithValue("@frequency", schedule.Frequency.HasValue ? (object)schedule.Frequency.Value : DBNull.Value);
                                        command.Parameters.AddWithValue("@freqUnit", (object)schedule.FrequencyUnit ?? DBNull.Value);
                                        command.Parameters.AddWithValue("@weekDays", (object)schedule.WeekDays ?? DBNull.Value);
                                        command.Parameters.AddWithValue("@monthDays", (object)schedule.MonthDays ?? DBNull.Value);
                                        command.Parameters.AddWithValue("@isActive", schedule.IsActive);
                                        command.Parameters.AddWithValue("@nextRun", schedule.NextRunTime.HasValue ? (object)schedule.NextRunTime.Value : DBNull.Value);
                                        command.Parameters.AddWithValue("@username", username);

                                        await command.ExecuteNonQueryAsync();
                                    }
                                }
                            }
                        }

                        transaction.Commit();
                        return configId;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error saving data transfer configuration");
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public async Task<int> ExecuteDataTransferAsync(string connectionString, int configurationId, string username)
        {
            // Get the configuration details
            var config = await GetConfigurationAsync(connectionString, configurationId);
            if (config == null)
            {
                throw new ArgumentException($"Configuration with ID {configurationId} not found");
            }
            if (config.SourceConnection == null)
            {
                throw new InvalidOperationException($"Source connection for configuration ID {configurationId} is null.");
            }
            if (config.DestinationConnection == null)
            {
                throw new InvalidOperationException($"Destination connection for configuration ID {configurationId} is null.");
            }

            _logger.LogInformation("ExecuteDataTransferAsync: Resolving source connection string template for Config ID {ConfigId}. Template starts with: {CSStart}...", 
                configurationId, config.SourceConnection.ConnectionString.Length > 20 ? config.SourceConnection.ConnectionString.Substring(0,20) : config.SourceConnection.ConnectionString);
            string resolvedSourceConnectionString = await _connectionStringResolverService.ResolveConnectionStringAsync(config.SourceConnection.ConnectionString);
            _logger.LogInformation("ExecuteDataTransferAsync: Source CS resolved for Config ID {ConfigId}. Resolved CS starts with: {CSStart}...", 
                configurationId, resolvedSourceConnectionString.Length > 20 ? resolvedSourceConnectionString.Substring(0,20) : resolvedSourceConnectionString);

            _logger.LogInformation("ExecuteDataTransferAsync: Resolving destination connection string template for Config ID {ConfigId}. Template starts with: {CSStart}...", 
                configurationId, config.DestinationConnection.ConnectionString.Length > 20 ? config.DestinationConnection.ConnectionString.Substring(0,20) : config.DestinationConnection.ConnectionString);
            string resolvedDestinationConnectionString = await _connectionStringResolverService.ResolveConnectionStringAsync(config.DestinationConnection.ConnectionString);
            _logger.LogInformation("ExecuteDataTransferAsync: Destination CS resolved for Config ID {ConfigId}. Resolved CS starts with: {CSStart}...", 
                configurationId, resolvedDestinationConnectionString.Length > 20 ? resolvedDestinationConnectionString.Substring(0,20) : resolvedDestinationConnectionString);

            // Create a new run record
            int runId;
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string insertRunSql = @"
                    INSERT INTO dbo.DataTransferRuns
                        (ConfigurationId, StartTime, Status, TriggeredBy)
                    VALUES
                        (@configId, GETUTCDATE(), 'Running', @username);
                    SELECT SCOPE_IDENTITY();";

                using (var command = new SqlCommand(insertRunSql, connection))
                {
                    command.Parameters.AddWithValue("@configId", configurationId);
                    command.Parameters.AddWithValue("@username", username);

                    runId = Convert.ToInt32(await command.ExecuteScalarAsync());
                }
            }

            // Start the transfer in a background task
            _ = Task.Run(async () =>
            {
                try
                {
                    // Pass resolved connection strings to RunTransferAsync
                    await RunTransferAsync(connectionString, config, runId, resolvedSourceConnectionString, resolvedDestinationConnectionString);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error in data transfer run ID {runId}");

                    // Update run status to failed
                    using (var connection = new SqlConnection(connectionString))
                    {
                        await connection.OpenAsync();

                        string updateRunSql = @"
                            UPDATE dbo.DataTransferRuns
                            SET Status = 'Failed',
                                EndTime = GETUTCDATE()
                            WHERE RunId = @runId";

                        using (var command = new SqlCommand(updateRunSql, connection))
                        {
                            command.Parameters.AddWithValue("@runId", runId);
                            await command.ExecuteNonQueryAsync();
                        }
                    }
                }
            });

            return runId;
        }

        // Modified to accept resolved source and destination connection strings
        private async Task RunTransferAsync(
            string appDbConnectionString, // Connection string for the application's own database
            DataTransferConfigurationDto config, 
            int runId, 
            string resolvedSourceDbConnectionString, 
            string resolvedDestinationDbConnectionString)
        {
            _logger.LogInformation("RunTransferAsync for Run ID {RunId}: Source CS (resolved) starts with: {SourceCSStart}..., Dest CS (resolved) starts with: {DestCSStart}...",
                runId,
                resolvedSourceDbConnectionString.Length > 20 ? resolvedSourceDbConnectionString.Substring(0,20) : resolvedSourceDbConnectionString,
                resolvedDestinationDbConnectionString.Length > 20 ? resolvedDestinationDbConnectionString.Substring(0,20) : resolvedDestinationDbConnectionString);

            var transfer = new IncrementalDataTransfer(
                resolvedSourceDbConnectionString,     // Use resolved string
                resolvedDestinationDbConnectionString, // Use resolved string
                config.BatchSize,
                config.ReportingFrequency,
                _logger);

            int totalTablesProcessed = 0;
            int successfulTablesCount = 0;
            int failedTablesCount = 0;
            int totalRowsProcessed = 0;
            var startTime = DateTime.UtcNow;

            foreach (var mapping in config.TableMappings.Where(m => m.IsActive))
            {
                totalTablesProcessed++;

                try
                {
                    // Create a table metric record
                    int metricId;
                    using (var connection = new SqlConnection(appDbConnectionString)) // Use appDbConnectionString for metrics
                    {
                        await connection.OpenAsync();

                        string insertMetricSql = @"
                            INSERT INTO dbo.DataTransferTableMetrics
                                (RunId, MappingId, SchemaName, TableName, StartTime, Status)
                            VALUES
                                (@runId, @mappingId, @schema, @table, GETUTCDATE(), 'Running');
                            SELECT SCOPE_IDENTITY();";

                        using (var command = new SqlCommand(insertMetricSql, connection))
                        {
                            command.Parameters.AddWithValue("@runId", runId);
                            command.Parameters.AddWithValue("@mappingId", mapping.MappingId);
                            command.Parameters.AddWithValue("@schema", mapping.SchemaName);
                            command.Parameters.AddWithValue("@table", mapping.TableName);

                            metricId = Convert.ToInt32(await command.ExecuteScalarAsync());
                        }
                    }

                    // Run the transfer
                    var summary = await transfer.TransferTableAsync(
                        mapping.SchemaName,
                        mapping.TableName,
                        mapping.TimestampColumnName,
                        mapping.CustomWhereClause,
                        mapping.OrderByColumn);

                    if (summary.Success)
                    {
                        successfulTablesCount++;
                    }
                    else
                    {
                        failedTablesCount++;
                    }

                    totalRowsProcessed += summary.RowsProcessed;

                    // Update the metric record
                    using (var connection = new SqlConnection(appDbConnectionString)) // Use appDbConnectionString for metrics
                    {
                        await connection.OpenAsync();

                        string updateMetricSql = @"
                            UPDATE dbo.DataTransferTableMetrics
                            SET EndTime = @endTime,
                                Status = @status,
                                TotalRowsToProcess = @totalRows,
                                RowsProcessed = @rowsProcessed,
                                ElapsedMs = @elapsedMs,
                                RowsPerSecond = @rowsPerSec,
                                ErrorMessage = @error,
                                LastProcessedTimestamp = @lastTs
                            WHERE MetricId = @metricId";

                        using (var command = new SqlCommand(updateMetricSql, connection))
                        {
                            command.Parameters.AddWithValue("@metricId", metricId);
                            command.Parameters.AddWithValue("@endTime", summary.EndTime);
                            command.Parameters.AddWithValue("@status", summary.Success ? "Completed" : "Failed");
                            command.Parameters.AddWithValue("@totalRows", summary.TotalRowsToProcess);
                            command.Parameters.AddWithValue("@rowsProcessed", summary.RowsProcessed);
                            command.Parameters.AddWithValue("@elapsedMs", summary.ElapsedMs);
                            command.Parameters.AddWithValue("@rowsPerSec", summary.RowsPerSecond);
                            command.Parameters.AddWithValue("@error", summary.ErrorMessage ?? (object)DBNull.Value);

                            // We don't have direct access to the last processed timestamp
                            // Could update this to retrieve from watermarks table
                            command.Parameters.AddWithValue("@lastTs", (object)DBNull.Value);

                            await command.ExecuteNonQueryAsync();
                        }
                    }
                }
                catch (Exception ex)
                {
                    failedTablesCount++;
                    _logger.LogError(ex, $"Error processing table [{mapping.SchemaName}].[{mapping.TableName}] for Run ID {runId}");

                    // Log the error
                    using (var connection = new SqlConnection(appDbConnectionString)) // Use appDbConnectionString for logs
                    {
                        await connection.OpenAsync();

                        string insertLogSql = @"
                            INSERT INTO dbo.DataTransferLogs
                                (RunId, MappingId, LogTime, LogLevel, Message, Exception)
                            VALUES
                                (@runId, @mappingId, GETUTCDATE(), 'Error', @message, @exception)";

                        using (var command = new SqlCommand(insertLogSql, connection))
                        {
                            command.Parameters.AddWithValue("@runId", runId);
                            command.Parameters.AddWithValue("@mappingId", mapping.MappingId);
                            command.Parameters.AddWithValue("@message", $"Error processing table [{mapping.SchemaName}].[{mapping.TableName}]");
                            command.Parameters.AddWithValue("@exception", ex.ToString());

                            await command.ExecuteNonQueryAsync();
                        }
                    }
                }
            }

            var endTime = DateTime.UtcNow;
            var elapsedMs = (long)(endTime - startTime).TotalMilliseconds;
            var avgRowsPerSecond = elapsedMs > 0 ? totalRowsProcessed / (elapsedMs / 1000.0) : 0;

            // Update the run record
            using (var connection = new SqlConnection(appDbConnectionString)) // Use appDbConnectionString for run record
            {
                await connection.OpenAsync();

                string updateRunSql = @"
                    UPDATE dbo.DataTransferRuns
                    SET EndTime = @endTime,
                        Status = @status,
                        TotalTablesProcessed = @totalTables,
                        SuccessfulTablesCount = @successCount,
                        FailedTablesCount = @failedCount,
                        TotalRowsProcessed = @totalRows,
                        ElapsedMs = @elapsedMs,
                        AverageRowsPerSecond = @avgRowsPerSec
                    WHERE RunId = @runId";

                using (var command = new SqlCommand(updateRunSql, connection))
                {
                    command.Parameters.AddWithValue("@runId", runId);
                    command.Parameters.AddWithValue("@endTime", endTime);
                    command.Parameters.AddWithValue("@status", failedTablesCount == 0 ? "Completed" : "CompletedWithErrors");
                    command.Parameters.AddWithValue("@totalTables", totalTablesProcessed);
                    command.Parameters.AddWithValue("@successCount", successfulTablesCount);
                    command.Parameters.AddWithValue("@failedCount", failedTablesCount);
                    command.Parameters.AddWithValue("@totalRows", totalRowsProcessed);
                    command.Parameters.AddWithValue("@elapsedMs", elapsedMs);
                    command.Parameters.AddWithValue("@avgRowsPerSec", avgRowsPerSecond);

                    await command.ExecuteNonQueryAsync();
                }
            }

            // Update the schedule's LastRunTime if this was scheduled
            if (config.Schedules != null && config.Schedules.Any(s => s.IsActive))
            {
                using (var connection = new SqlConnection(appDbConnectionString)) // Use appDbConnectionString for schedule update
                {
                    await connection.OpenAsync();

                    string updateScheduleSql = @"
                        UPDATE dbo.DataTransferSchedule
                        SET LastRunTime = @lastRun
                        WHERE ConfigurationId = @configId AND IsActive = 1";

                    using (var command = new SqlCommand(updateScheduleSql, connection))
                    {
                        command.Parameters.AddWithValue("@configId", config.ConfigurationId);
                        command.Parameters.AddWithValue("@lastRun", endTime);

                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
        }

        public async Task<List<RunHistoryDto>> GetRunHistoryAsync(string connectionString, int configurationId = 0, int limit = 50)
        {
            var history = new List<RunHistoryDto>();

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string sql = @"
                    SELECT TOP (@limit) r.RunId, r.ConfigurationId, c.ConfigurationName,
                           r.StartTime, r.EndTime, r.Status,
                           r.TotalTablesProcessed, r.SuccessfulTablesCount, r.FailedTablesCount,
                           r.TotalRowsProcessed, r.ElapsedMs, r.AverageRowsPerSecond, r.TriggeredBy
                    FROM dbo.DataTransferRuns r
                    INNER JOIN dbo.DataTransferConfigurations c ON r.ConfigurationId = c.ConfigurationId
                    WHERE (@configId = 0 OR r.ConfigurationId = @configId)
                    ORDER BY r.StartTime DESC";

                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@limit", limit);
                    command.Parameters.AddWithValue("@configId", configurationId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            history.Add(new RunHistoryDto
                            {
                                RunId = reader.GetInt32(reader.GetOrdinal("RunId")),
                                ConfigurationId = reader.GetInt32(reader.GetOrdinal("ConfigurationId")),
                                ConfigurationName = reader.GetString(reader.GetOrdinal("ConfigurationName")),
                                StartTime = reader.GetDateTime(reader.GetOrdinal("StartTime")),
                                EndTime = reader.IsDBNull(reader.GetOrdinal("EndTime")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("EndTime")),
                                Status = reader.GetString(reader.GetOrdinal("Status")),
                                TotalTablesProcessed = reader.GetInt32(reader.GetOrdinal("TotalTablesProcessed")),
                                SuccessfulTablesCount = reader.GetInt32(reader.GetOrdinal("SuccessfulTablesCount")),
                                FailedTablesCount = reader.GetInt32(reader.GetOrdinal("FailedTablesCount")),
                                TotalRowsProcessed = reader.GetInt32(reader.GetOrdinal("TotalRowsProcessed")),
                                ElapsedMs = reader.IsDBNull(reader.GetOrdinal("ElapsedMs")) ? 0 : reader.GetInt64(reader.GetOrdinal("ElapsedMs")),
                                AverageRowsPerSecond = reader.IsDBNull(reader.GetOrdinal("AverageRowsPerSecond")) ? 0 : reader.GetDouble(reader.GetOrdinal("AverageRowsPerSecond")),
                                TriggeredBy = reader.IsDBNull(reader.GetOrdinal("TriggeredBy")) ? null : reader.GetString(reader.GetOrdinal("TriggeredBy"))
                            });
                        }
                    }
                }
            }

            return history;
        }

        public async Task<RunDetailsDto> GetRunDetailsAsync(string connectionString, int runId)
        {
            RunDetailsDto runDetails = null;

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                // Get run summary
                string runSql = @"
                    SELECT r.RunId, r.ConfigurationId, c.ConfigurationName,
                           r.StartTime, r.EndTime, r.Status,
                           r.TotalTablesProcessed, r.SuccessfulTablesCount, r.FailedTablesCount,
                           r.TotalRowsProcessed, r.ElapsedMs, r.AverageRowsPerSecond, r.TriggeredBy
                    FROM dbo.DataTransferRuns r
                    INNER JOIN dbo.DataTransferConfigurations c ON r.ConfigurationId = c.ConfigurationId
                    WHERE r.RunId = @runId";

                using (var command = new SqlCommand(runSql, connection))
                {
                    command.Parameters.AddWithValue("@runId", runId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            runDetails = new RunDetailsDto
                            {
                                RunId = reader.GetInt32(reader.GetOrdinal("RunId")),
                                ConfigurationId = reader.GetInt32(reader.GetOrdinal("ConfigurationId")),
                                ConfigurationName = reader.GetString(reader.GetOrdinal("ConfigurationName")),
                                StartTime = reader.GetDateTime(reader.GetOrdinal("StartTime")),
                                EndTime = reader.IsDBNull(reader.GetOrdinal("EndTime")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("EndTime")),
                                Status = reader.GetString(reader.GetOrdinal("Status")),
                                TotalTablesProcessed = reader.GetInt32(reader.GetOrdinal("TotalTablesProcessed")),
                                SuccessfulTablesCount = reader.GetInt32(reader.GetOrdinal("SuccessfulTablesCount")),
                                FailedTablesCount = reader.GetInt32(reader.GetOrdinal("FailedTablesCount")),
                                TotalRowsProcessed = reader.GetInt32(reader.GetOrdinal("TotalRowsProcessed")),
                                ElapsedMs = reader.IsDBNull(reader.GetOrdinal("ElapsedMs")) ? 0 : reader.GetInt64(reader.GetOrdinal("ElapsedMs")),
                                AverageRowsPerSecond = reader.IsDBNull(reader.GetOrdinal("AverageRowsPerSecond")) ? 0 : reader.GetDouble(reader.GetOrdinal("AverageRowsPerSecond")),
                                TriggeredBy = reader.IsDBNull(reader.GetOrdinal("TriggeredBy")) ? null : reader.GetString(reader.GetOrdinal("TriggeredBy")),
                                TableMetrics = new List<TableMetricDto>(),
                                Logs = new List<LogEntryDto>()
                            };
                        }
                    }
                }

                if (runDetails == null)
                {
                    return null;
                }

                // Get table metrics
                string metricsSql = @"
                    SELECT m.MetricId, m.MappingId, m.SchemaName, m.TableName,
                           m.StartTime, m.EndTime, m.Status,
                           m.TotalRowsToProcess, m.RowsProcessed,
                           m.ElapsedMs, m.RowsPerSecond, m.ErrorMessage
                    FROM dbo.DataTransferTableMetrics m
                    WHERE m.RunId = @runId
                    ORDER BY m.StartTime";

                using (var command = new SqlCommand(metricsSql, connection))
                {
                    command.Parameters.AddWithValue("@runId", runId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            runDetails.TableMetrics.Add(new TableMetricDto
                            {
                                MetricId = reader.GetInt32(reader.GetOrdinal("MetricId")),
                                MappingId = reader.GetInt32(reader.GetOrdinal("MappingId")),
                                SchemaName = reader.GetString(reader.GetOrdinal("SchemaName")),
                                TableName = reader.GetString(reader.GetOrdinal("TableName")),
                                StartTime = reader.GetDateTime(reader.GetOrdinal("StartTime")),
                                EndTime = reader.IsDBNull(reader.GetOrdinal("EndTime")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("EndTime")),
                                Status = reader.GetString(reader.GetOrdinal("Status")),
                                TotalRowsToProcess = reader.GetInt32(reader.GetOrdinal("TotalRowsToProcess")),
                                RowsProcessed = reader.GetInt32(reader.GetOrdinal("RowsProcessed")),
                                ElapsedMs = reader.IsDBNull(reader.GetOrdinal("ElapsedMs")) ? 0 : reader.GetInt64(reader.GetOrdinal("ElapsedMs")),
                                RowsPerSecond = reader.IsDBNull(reader.GetOrdinal("RowsPerSecond")) ? 0 : reader.GetDouble(reader.GetOrdinal("RowsPerSecond")),
                                ErrorMessage = reader.IsDBNull(reader.GetOrdinal("ErrorMessage")) ? null : reader.GetString(reader.GetOrdinal("ErrorMessage"))
                            });
                        }
                    }
                }

                // Get logs
                string logsSql = @"
                    SELECT l.LogId, l.MappingId, l.LogTime, l.LogLevel, l.Message, l.Exception
                    FROM dbo.DataTransferLogs l
                    WHERE l.RunId = @runId
                    ORDER BY l.LogTime";

                using (var command = new SqlCommand(logsSql, connection))
                {
                    command.Parameters.AddWithValue("@runId", runId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            runDetails.Logs.Add(new LogEntryDto
                            {
                                LogId = reader.GetInt32(reader.GetOrdinal("LogId")),
                                MappingId = reader.IsDBNull(reader.GetOrdinal("MappingId")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("MappingId")),
                                LogTime = reader.GetDateTime(reader.GetOrdinal("LogTime")),
                                LogLevel = reader.GetString(reader.GetOrdinal("LogLevel")),
                                Message = reader.GetString(reader.GetOrdinal("Message")),
                                Exception = reader.IsDBNull(reader.GetOrdinal("Exception")) ? null : reader.GetString(reader.GetOrdinal("Exception"))
                            });
                        }
                    }
                }
            }

            return runDetails;
        }
    }

    // DTOs
    public class ConnectionDto
    {
        public int ConnectionId { get; set; }
        public string ConnectionName { get; set; } = string.Empty;
        // This ConnectionString will hold the template (e.g., with {azurevault:...} placeholders) 
        // or a direct connection string if not using Key Vault.
        public string ConnectionString { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ConnectionAccessLevel ConnectionAccessLevel { get; set; } = ConnectionAccessLevel.ReadWrite;
        public bool IsActive { get; set; } = true;
        public DateTime? LastTestedOn { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; } = "System";
        public DateTime? LastModifiedOn { get; set; }
        public string LastModifiedBy { get; set; } = string.Empty;
        public int MaxPoolSize { get; set; } = 100;
        public int MinPoolSize { get; set; } = 5;
        public int Timeout { get; set; } = 30;
        public bool Encrypt { get; set; } = true;
        public bool TrustServerCertificate { get; set; } = true;

        // Removed ConnectionStringForDisplay, ConnectionDetails, ConnectionStringForUse
        // Removed IsSource and IsDestination (fully replaced by ConnectionAccessLevel)
    }

    public class TableMappingDto
    {
        public int MappingId { get; set; }
        public int ConfigurationId { get; set; }
        public string SchemaName { get; set; }
        public string TableName { get; set; }
        public string TimestampColumnName { get; set; }
        public string OrderByColumn { get; set; }
        public string CustomWhereClause { get; set; }
        public bool IsActive { get; set; } = true;
        public int Priority { get; set; } = 100;
    }

    public class ScheduleDto
    {
        public int ScheduleId { get; set; }
        public int ConfigurationId { get; set; }
        public string ScheduleType { get; set; }
        public TimeSpan? StartTime { get; set; }
        public int? Frequency { get; set; }
        public string FrequencyUnit { get; set; }
        public string WeekDays { get; set; }
        public string MonthDays { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? LastRunTime { get; set; }
        public DateTime? NextRunTime { get; set; }
    }

    public class DataTransferConfigurationDto
    {
        public int ConfigurationId { get; set; }
        public string ConfigurationName { get; set; }
        public string Description { get; set; }
        public ConnectionDto SourceConnection { get; set; }
        public ConnectionDto DestinationConnection { get; set; }
        public int BatchSize { get; set; } = 5000;
        public int ReportingFrequency { get; set; } = 10;
        public bool IsActive { get; set; } = true;
        public List<TableMappingDto> TableMappings { get; set; } = new List<TableMappingDto>();
        public List<ScheduleDto> Schedules { get; set; } = new List<ScheduleDto>();
    }

    public class RunHistoryDto
    {
        public int RunId { get; set; }
        public int ConfigurationId { get; set; }
        public string ConfigurationName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Status { get; set; }
        public int TotalTablesProcessed { get; set; }
        public int SuccessfulTablesCount { get; set; }
        public int FailedTablesCount { get; set; }
        public int TotalRowsProcessed { get; set; }
        public long ElapsedMs { get; set; }
        public double AverageRowsPerSecond { get; set; }
        public string TriggeredBy { get; set; }
    }

    public class TableMetricDto
    {
        public int MetricId { get; set; }
        public int MappingId { get; set; }
        public string SchemaName { get; set; }
        public string TableName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Status { get; set; }
        public int TotalRowsToProcess { get; set; }
        public int RowsProcessed { get; set; }
        public long ElapsedMs { get; set; }
        public double RowsPerSecond { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class LogEntryDto
    {
        public int LogId { get; set; }
        public int? MappingId { get; set; }
        public DateTime LogTime { get; set; }
        public string LogLevel { get; set; }
        public string Message { get; set; }
        public string Exception { get; set; }
    }

    public class RunDetailsDto : RunHistoryDto
    {
        public List<TableMetricDto> TableMetrics { get; set; } = new List<TableMetricDto>();
        public List<LogEntryDto> Logs { get; set; } = new List<LogEntryDto>();
    }
}