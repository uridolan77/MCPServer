using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MCPServer.DatabaseSchema;
using MCPServer.Core.Services.DataTransfer;
using MCPServer.Core.Models.DataTransfer;

namespace MCPServer.API.Controllers
{
    // Note: DataTransferService has been removed in the cleanup
    // This controller now needs to implement its functionality directly or
    // use a replacement service

    [ApiController]
    [Route("api/data-transfer")]
    public class DataTransferController : ControllerBase
    {
        private readonly ILogger<DataTransferController> _logger;
        private string _connectionString; // Not readonly so we can update it at runtime
        private readonly ConnectionStringHasher _connectionStringHasher;
        private readonly IConnectionStringResolverService _connectionStringResolverService;
        private readonly DataTransferMetricsService _metricsService; // Injected service for metrics

        public DataTransferController(
            IConfiguration configuration,
            ILogger<DataTransferController> logger,
            IConnectionStringResolverService connectionStringResolverService,
            DataTransferMetricsService metricsService) // Added metrics service
        {
            _logger = logger;
            _connectionStringHasher = new ConnectionStringHasher(logger);
            _connectionStringResolverService = connectionStringResolverService;
            _metricsService = metricsService; // Initialized metrics service

            // For development, use direct connection string with credentials
            _connectionString = "Server=tcp:progressplay-server.database.windows.net,1433;Initial Catalog=ProgressPlayDB;User ID=pp-sa;Password=RDlS8C6zVewS-wJOr4_oY5Y;Encrypt=True;TrustServerCertificate=True;Connection Timeout=30;";

            _logger.LogInformation("Connection string configured for ProgressPlayDB with development credentials");
        }

        // TODO: Implement methods that were previously using DataTransferService
        // Keeping stub methods to maintain the API surface

        [HttpGet("configurations")]
        public IActionResult GetConfigurations()
        {
            _logger.LogInformation("GetConfigurations endpoint called");
            
            // Return configurations in the format expected by the frontend
            var mockConfigurations = new List<object>
            {
                new
                {
                    ConfigurationId = 1,
                    ConfigurationName = "Daily Player Data Sync",
                    Description = "Synchronize player data from source to destination database",
                    SourceConnection = new
                    {
                        ConnectionId = 1,
                        ConnectionName = "ProgressPlayDB",
                        ConnectionString = "Server=tcp:progressplay-server.database.windows.net,1433;Initial Catalog=ProgressPlayDB;",
                        Description = "ProgressPlay Production Database",
                        IsActive = true
                    },
                    DestinationConnection = new
                    {
                        ConnectionId = 2,
                        ConnectionName = "MCPAnalyticsDB",
                        ConnectionString = "Server=tcp:mcp-analytics.database.windows.net,1433;Initial Catalog=MCPAnalyticsDB;",
                        Description = "MCP Analytics Database",
                        IsActive = true
                    },
                    TableMappings = new[]
                    {
                        new { 
                            TableMappingId = 1,
                            SourceTable = "Players", 
                            DestinationTable = "Players",
                            IsActive = true
                        },
                        new { 
                            TableMappingId = 2,
                            SourceTable = "PlayerActions", 
                            DestinationTable = "PlayerActions",
                            IsActive = true
                        },
                        new { 
                            TableMappingId = 3,
                            SourceTable = "GameSessions", 
                            DestinationTable = "GameSessions",
                            IsActive = true
                        }
                    },
                    IsActive = true,
                    BatchSize = 1000,
                    ReportingFrequency = 100
                },
                new
                {
                    ConfigurationId = 2,
                    ConfigurationName = "Weekly Transaction Summary",
                    Description = "Transfer weekly transaction summary to reporting database",
                    SourceConnection = new
                    {
                        ConnectionId = 1,
                        ConnectionName = "TransactionsDB",
                        ConnectionString = "Server=tcp:transactions-server.database.windows.net,1433;Initial Catalog=TransactionsDB;",
                        Description = "Transactions Database",
                        IsActive = true
                    },
                    DestinationConnection = new
                    {
                        ConnectionId = 3,
                        ConnectionName = "ReportingDB", 
                        ConnectionString = "Server=tcp:reporting.database.windows.net,1433;Initial Catalog=ReportingDB;",
                        Description = "Reporting Database",
                        IsActive = true
                    },
                    TableMappings = new[]
                    {
                        new { 
                            TableMappingId = 4,
                            SourceTable = "Transactions", 
                            DestinationTable = "Transactions",
                            IsActive = true
                        },
                        new { 
                            TableMappingId = 5,
                            SourceTable = "TransactionSummary", 
                            DestinationTable = "TransactionSummary",
                            IsActive = true
                        }
                    },
                    IsActive = true,
                    BatchSize = 500,
                    ReportingFrequency = 50
                },
                new
                {
                    ConfigurationId = 3,
                    ConfigurationName = "User Profile Transfer",
                    Description = "Transfer user profiles between systems",
                    SourceConnection = new
                    {
                        ConnectionId = 4,
                        ConnectionName = "UserManagementDB",
                        ConnectionString = "Server=tcp:user-mgmt.database.windows.net,1433;Initial Catalog=UserManagementDB;",
                        Description = "User Management Database",
                        IsActive = true
                    },
                    DestinationConnection = new
                    {
                        ConnectionId = 2,
                        ConnectionName = "CustomerDB",
                        ConnectionString = "Server=tcp:customer.database.windows.net,1433;Initial Catalog=CustomerDB;",
                        Description = "Customer Database",
                        IsActive = true
                    },
                    TableMappings = new[]
                    {
                        new { 
                            TableMappingId = 6,
                            SourceTable = "UserProfiles", 
                            DestinationTable = "UserProfiles",
                            IsActive = true
                        },
                        new { 
                            TableMappingId = 7,
                            SourceTable = "UserPreferences", 
                            DestinationTable = "UserPreferences",
                            IsActive = true
                        }
                    },
                    IsActive = false,
                    BatchSize = 200,
                    ReportingFrequency = 20
                }
            };
            
            // Return data in the expected format with a "$values" property
            // This matches what the frontend expects with extractFromNestedValues function
            return Ok(new { values = mockConfigurations });
        }

        [HttpGet("configurations/{id}")]
        public IActionResult GetConfiguration(int id)
        {
            _logger.LogInformation("GetConfiguration endpoint called for ID: {Id}", id);
            
            // Return mock data based on the requested ID
            if (id == 1)
            {
                return Ok(new 
                {
                    ConfigurationId = 1,
                    ConfigurationName = "Daily Player Data Sync",
                    Description = "Synchronize player data from source to destination database",
                    SourceConnection = new
                    {
                        ConnectionId = 1,
                        ConnectionName = "ProgressPlayDB",
                        ConnectionString = "Server=tcp:progressplay-server.database.windows.net,1433;Initial Catalog=ProgressPlayDB;",
                        Description = "ProgressPlay Production Database",
                        IsActive = true
                    },
                    DestinationConnection = new
                    {
                        ConnectionId = 2,
                        ConnectionName = "MCPAnalyticsDB",
                        ConnectionString = "Server=tcp:mcp-analytics.database.windows.net,1433;Initial Catalog=MCPAnalyticsDB;",
                        Description = "MCP Analytics Database",
                        IsActive = true
                    },
                    TableMappings = new[]
                    {
                        new { 
                            TableMappingId = 1,
                            SourceTable = "Players", 
                            DestinationTable = "Players",
                            IsActive = true,
                            ColumnMappings = new[] 
                            {
                                new { SourceColumn = "PlayerId", DestinationColumn = "PlayerId", IsPrimaryKey = true },
                                new { SourceColumn = "PlayerName", DestinationColumn = "PlayerName", IsPrimaryKey = false },
                                new { SourceColumn = "Email", DestinationColumn = "Email", IsPrimaryKey = false },
                                new { SourceColumn = "RegistrationDate", DestinationColumn = "RegistrationDate", IsPrimaryKey = false }
                            }
                        },
                        new { 
                            TableMappingId = 2,
                            SourceTable = "PlayerActions", 
                            DestinationTable = "PlayerActions",
                            IsActive = true,
                            ColumnMappings = new[] 
                            {
                                new { SourceColumn = "ActionId", DestinationColumn = "ActionId", IsPrimaryKey = true },
                                new { SourceColumn = "PlayerId", DestinationColumn = "PlayerId", IsPrimaryKey = false },
                                new { SourceColumn = "ActionType", DestinationColumn = "ActionType", IsPrimaryKey = false },
                                new { SourceColumn = "ActionDate", DestinationColumn = "ActionDate", IsPrimaryKey = false }
                            }
                        },
                        new { 
                            TableMappingId = 3,
                            SourceTable = "GameSessions", 
                            DestinationTable = "GameSessions",
                            IsActive = true,
                            ColumnMappings = new[] 
                            {
                                new { SourceColumn = "SessionId", DestinationColumn = "SessionId", IsPrimaryKey = true },
                                new { SourceColumn = "PlayerId", DestinationColumn = "PlayerId", IsPrimaryKey = false },
                                new { SourceColumn = "StartTime", DestinationColumn = "StartTime", IsPrimaryKey = false },
                                new { SourceColumn = "EndTime", DestinationColumn = "EndTime", IsPrimaryKey = false }
                            }
                        }
                    },
                    Schedule = new
                    {
                        IsScheduled = true,
                        Frequency = "Daily",
                        StartTime = "02:00:00",
                        DaysOfWeek = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" }
                    },
                    IsActive = true,
                    LastRun = DateTime.UtcNow.AddDays(-1),
                    NextRun = DateTime.UtcNow.AddHours(2),
                    CreatedAt = DateTime.UtcNow.AddMonths(-3),
                    UpdatedAt = DateTime.UtcNow.AddDays(-5),
                    BatchSize = 1000,
                    ReportingFrequency = 100
                });
            }
            else if (id == 2)
            {
                return Ok(new 
                {
                    ConfigurationId = 2,
                    ConfigurationName = "Weekly Transaction Summary",
                    Description = "Transfer weekly transaction summary to reporting database",
                    SourceConnection = new
                    {
                        ConnectionId = 1,
                        ConnectionName = "TransactionsDB",
                        ConnectionString = "Server=tcp:transactions-server.database.windows.net,1433;Initial Catalog=TransactionsDB;",
                        Description = "Transactions Database",
                        IsActive = true
                    },
                    DestinationConnection = new
                    {
                        ConnectionId = 3,
                        ConnectionName = "ReportingDB",
                        ConnectionString = "Server=tcp:reporting.database.windows.net,1433;Initial Catalog=ReportingDB;",
                        Description = "Reporting Database",
                        IsActive = true
                    },
                    TableMappings = new[]
                    {
                        new { 
                            TableMappingId = 4,
                            SourceTable = "Transactions", 
                            DestinationTable = "Transactions",
                            IsActive = true,
                            ColumnMappings = new[] 
                            {
                                new { SourceColumn = "TransactionId", DestinationColumn = "TransactionId", IsPrimaryKey = true },
                                new { SourceColumn = "PlayerId", DestinationColumn = "PlayerId", IsPrimaryKey = false },
                                new { SourceColumn = "Amount", DestinationColumn = "Amount", IsPrimaryKey = false },
                                new { SourceColumn = "TransactionDate", DestinationColumn = "TransactionDate", IsPrimaryKey = false }
                            }
                        },
                        new { 
                            TableMappingId = 5,
                            SourceTable = "TransactionSummary", 
                            DestinationTable = "TransactionSummary",
                            IsActive = true,
                            ColumnMappings = new[] 
                            {
                                new { SourceColumn = "SummaryId", DestinationColumn = "SummaryId", IsPrimaryKey = true },
                                new { SourceColumn = "WeekStartDate", DestinationColumn = "WeekStartDate", IsPrimaryKey = false },
                                new { SourceColumn = "WeekEndDate", DestinationColumn = "WeekEndDate", IsPrimaryKey = false },
                                new { SourceColumn = "TotalTransactions", DestinationColumn = "TotalTransactions", IsPrimaryKey = false }
                            }
                        }
                    },
                    Schedule = new
                    {
                        IsScheduled = true,
                        Frequency = "Weekly",
                        StartTime = "03:00:00",
                        DaysOfWeek = new[] { "Monday" }
                    },
                    IsActive = true,
                    LastRun = DateTime.UtcNow.AddDays(-7),
                    NextRun = DateTime.UtcNow.AddDays(7).Date.Add(new TimeSpan(3, 0, 0)),
                    CreatedAt = DateTime.UtcNow.AddMonths(-2),
                    UpdatedAt = DateTime.UtcNow.AddDays(-7),
                    BatchSize = 500,
                    ReportingFrequency = 50
                });
            }
            else if (id == 3)
            {
                return Ok(new 
                {
                    ConfigurationId = 3,
                    ConfigurationName = "User Profile Transfer",
                    Description = "Transfer user profiles between systems",
                    SourceConnection = new
                    {
                        ConnectionId = 4,
                        ConnectionName = "UserManagementDB",
                        ConnectionString = "Server=tcp:user-mgmt.database.windows.net,1433;Initial Catalog=UserManagementDB;",
                        Description = "User Management Database",
                        IsActive = true
                    },
                    DestinationConnection = new
                    {
                        ConnectionId = 2,
                        ConnectionName = "CustomerDB",
                        ConnectionString = "Server=tcp:customer.database.windows.net,1433;Initial Catalog=CustomerDB;",
                        Description = "Customer Database",
                        IsActive = true
                    },
                    TableMappings = new[]
                    {
                        new { 
                            TableMappingId = 6,
                            SourceTable = "UserProfiles", 
                            DestinationTable = "UserProfiles",
                            IsActive = true,
                            ColumnMappings = new[] 
                            {
                                new { SourceColumn = "UserProfileId", DestinationColumn = "UserProfileId", IsPrimaryKey = true },
                                new { SourceColumn = "UserId", DestinationColumn = "UserId", IsPrimaryKey = false },
                                new { SourceColumn = "FirstName", DestinationColumn = "FirstName", IsPrimaryKey = false },
                                new { SourceColumn = "LastName", DestinationColumn = "LastName", IsPrimaryKey = false }
                            }
                        },
                        new { 
                            TableMappingId = 7,
                            SourceTable = "UserPreferences", 
                            DestinationTable = "UserPreferences",
                            IsActive = true,
                            ColumnMappings = new[] 
                            {
                                new { SourceColumn = "PreferenceId", DestinationColumn = "PreferenceId", IsPrimaryKey = true },
                                new { SourceColumn = "UserId", DestinationColumn = "UserId", IsPrimaryKey = false },
                                new { SourceColumn = "PreferenceKey", DestinationColumn = "PreferenceKey", IsPrimaryKey = false },
                                new { SourceColumn = "PreferenceValue", DestinationColumn = "PreferenceValue", IsPrimaryKey = false }
                            }
                        }
                    },
                    Schedule = new
                    {
                        IsScheduled = false,
                        Frequency = string.Empty,
                        StartTime = string.Empty,
                        DaysOfWeek = new string[] { }
                    },
                    IsActive = false,
                    LastRun = DateTime.MinValue,
                    NextRun = DateTime.MinValue,
                    CreatedAt = DateTime.UtcNow.AddMonths(-1),
                    UpdatedAt = DateTime.UtcNow.AddDays(-15),
                    BatchSize = 200,
                    ReportingFrequency = 20
                });
            }
            
            return NotFound($"Configuration with ID {id} not found");
        }

        [HttpGet("connections")]
        public async Task<IActionResult> GetConnections([FromQuery] bool? isSource, [FromQuery] bool? isDestination, [FromQuery] bool? isActive = null)
        {
            _logger.LogInformation("GetConnections endpoint called with filters: isSource={IsSource}, isDestination={IsDestination}, isActive={IsActive}", 
                isSource, isDestination, isActive);
            
            try
            {
                // Query the database for all connections
                var connections = new List<DataTransferConnection>();
                
                // Here we use the actual connection string to connect to the database
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    string sql = @"
                        SELECT 
                            ConnectionId,
                            ConnectionName,
                            ConnectionString,
                            ConnectionAccessLevel,
                            Description,
                            Server,
                            Port,
                            Database,
                            Username,
                            Password,
                            AdditionalParameters,
                            IsActive,
                            IsConnectionValid,
                            MinPoolSize,
                            MaxPoolSize,
                            Timeout,
                            TrustServerCertificate,
                            Encrypt,
                            CreatedBy,
                            CreatedOn,
                            LastModifiedBy,
                            LastModifiedOn,
                            LastTestedOn
                        FROM DataTransferConnections 
                        WHERE 1=1";
                    
                    // Apply filters based on ConnectionAccessLevel instead of IsSource/IsDestination
                    // which are computed properties in our model
                    if (isSource.HasValue && isDestination.HasValue)
                    {
                        if (isSource.Value && isDestination.Value)
                        {
                            // Both source and destination = ReadWrite
                            sql += " AND ConnectionAccessLevel = 'ReadWrite'";
                        }
                        else if (isSource.Value)
                        {
                            // Source only = ReadOnly
                            sql += " AND ConnectionAccessLevel = 'ReadOnly'";
                        }
                        else if (isDestination.Value)
                        {
                            // Destination only = WriteOnly
                            sql += " AND ConnectionAccessLevel = 'WriteOnly'";
                        }
                        else
                        {
                            // Neither source nor destination - shouldn't return any results
                            sql += " AND 1=0";
                        }
                    }
                    else if (isSource.HasValue)
                    {
                        if (isSource.Value)
                        {
                            // Source - either ReadOnly or ReadWrite
                            sql += " AND (ConnectionAccessLevel = 'ReadOnly' OR ConnectionAccessLevel = 'ReadWrite')";
                        }
                        else
                        {
                            // Not source - must be WriteOnly
                            sql += " AND ConnectionAccessLevel = 'WriteOnly'";
                        }
                    }
                    else if (isDestination.HasValue)
                    {
                        if (isDestination.Value)
                        {
                            // Destination - either WriteOnly or ReadWrite
                            sql += " AND (ConnectionAccessLevel = 'WriteOnly' OR ConnectionAccessLevel = 'ReadWrite')";
                        }
                        else
                        {
                            // Not destination - must be ReadOnly
                            sql += " AND ConnectionAccessLevel = 'ReadOnly'";
                        }
                    }
                    
                    if (isActive.HasValue)
                    {
                        sql += " AND IsActive = @IsActive";
                    }
                    
                    _logger.LogInformation("Executing SQL: {Sql}", sql);
                    
                    using (var command = new SqlCommand(sql, connection))
                    {
                        if (isActive.HasValue)
                        {
                            command.Parameters.AddWithValue("@IsActive", isActive.Value ? 1 : 0);
                        }
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var conn = new DataTransferConnection
                                {
                                    ConnectionId = reader.GetInt32(reader.GetOrdinal("ConnectionId")),
                                    ConnectionName = !reader.IsDBNull(reader.GetOrdinal("ConnectionName")) ? 
                                        reader.GetString(reader.GetOrdinal("ConnectionName")) : null,
                                    ConnectionString = !reader.IsDBNull(reader.GetOrdinal("ConnectionString")) ? 
                                        reader.GetString(reader.GetOrdinal("ConnectionString")) : null,
                                    ConnectionAccessLevel = !reader.IsDBNull(reader.GetOrdinal("ConnectionAccessLevel")) ? 
                                        reader.GetString(reader.GetOrdinal("ConnectionAccessLevel")) : null,
                                    Description = !reader.IsDBNull(reader.GetOrdinal("Description")) ? 
                                        reader.GetString(reader.GetOrdinal("Description")) : null,
                                    Server = !reader.IsDBNull(reader.GetOrdinal("Server")) ? 
                                        reader.GetString(reader.GetOrdinal("Server")) : null,
                                    Port = !reader.IsDBNull(reader.GetOrdinal("Port")) ? 
                                        reader.GetInt32(reader.GetOrdinal("Port")) : null,
                                    Database = !reader.IsDBNull(reader.GetOrdinal("Database")) ? 
                                        reader.GetString(reader.GetOrdinal("Database")) : null,
                                    Username = !reader.IsDBNull(reader.GetOrdinal("Username")) ? 
                                        reader.GetString(reader.GetOrdinal("Username")) : null,
                                    Password = !reader.IsDBNull(reader.GetOrdinal("Password")) ? 
                                        reader.GetString(reader.GetOrdinal("Password")) : null,
                                    AdditionalParameters = !reader.IsDBNull(reader.GetOrdinal("AdditionalParameters")) ? 
                                        reader.GetString(reader.GetOrdinal("AdditionalParameters")) : null,
                                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                                    IsConnectionValid = !reader.IsDBNull(reader.GetOrdinal("IsConnectionValid")) ? 
                                        reader.GetBoolean(reader.GetOrdinal("IsConnectionValid")) : null,
                                    MinPoolSize = !reader.IsDBNull(reader.GetOrdinal("MinPoolSize")) ? 
                                        reader.GetInt32(reader.GetOrdinal("MinPoolSize")) : null,
                                    MaxPoolSize = !reader.IsDBNull(reader.GetOrdinal("MaxPoolSize")) ? 
                                        reader.GetInt32(reader.GetOrdinal("MaxPoolSize")) : null,
                                    Timeout = !reader.IsDBNull(reader.GetOrdinal("Timeout")) ? 
                                        reader.GetInt32(reader.GetOrdinal("Timeout")) : null,
                                    TrustServerCertificate = !reader.IsDBNull(reader.GetOrdinal("TrustServerCertificate")) ? 
                                        reader.GetBoolean(reader.GetOrdinal("TrustServerCertificate")) : null,
                                    Encrypt = !reader.IsDBNull(reader.GetOrdinal("Encrypt")) ? 
                                        reader.GetBoolean(reader.GetOrdinal("Encrypt")) : null,
                                    CreatedBy = !reader.IsDBNull(reader.GetOrdinal("CreatedBy")) ? 
                                        reader.GetString(reader.GetOrdinal("CreatedBy")) : null,
                                    CreatedOn = !reader.IsDBNull(reader.GetOrdinal("CreatedOn")) ? 
                                        reader.GetDateTime(reader.GetOrdinal("CreatedOn")) : DateTime.UtcNow,
                                    LastModifiedBy = !reader.IsDBNull(reader.GetOrdinal("LastModifiedBy")) ? 
                                        reader.GetString(reader.GetOrdinal("LastModifiedBy")) : null,
                                    LastModifiedOn = !reader.IsDBNull(reader.GetOrdinal("LastModifiedOn")) ? 
                                        reader.GetDateTime(reader.GetOrdinal("LastModifiedOn")) : null,
                                    LastTestedOn = !reader.IsDBNull(reader.GetOrdinal("LastTestedOn")) ? 
                                        reader.GetDateTime(reader.GetOrdinal("LastTestedOn")) : null
                                };
                                
                                connections.Add(conn);
                            }
                        }
                    }
                }
                
                _logger.LogInformation("Retrieved {Count} connections from the database", connections.Count);
                
                // Return with $values property to match frontend expectations
                return Ok(new { values = connections });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving connections from database");
                return StatusCode(500, new { message = "Error retrieving connections", error = ex.Message });
            }
        }
        
        // Helper method to extract server name from a connection string
        private string ExtractServerFromConnectionString(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                return null;
                
            try
            {
                // Try to extract Server or Data Source
                var serverMatch = Regex.Match(connectionString, @"Server\s*=\s*tcp:([^,;]+)", RegexOptions.IgnoreCase);
                if (serverMatch.Success && serverMatch.Groups.Count > 1)
                {
                    return serverMatch.Groups[1].Value.Trim();
                }
                
                // Try Data Source format
                var dataSourceMatch = Regex.Match(connectionString, @"Data Source\s*=\s*([^;]+)", RegexOptions.IgnoreCase);
                if (dataSourceMatch.Success && dataSourceMatch.Groups.Count > 1)
                {
                    string dataSource = dataSourceMatch.Groups[1].Value.Trim();
                    // If it includes port, extract just the server
                    var parts = dataSource.Split(',');
                    return parts[0].Trim();
                }
                
                // Try simple server format
                var simpleServerMatch = Regex.Match(connectionString, @"Server\s*=\s*([^;,]+)", RegexOptions.IgnoreCase);
                if (simpleServerMatch.Success && simpleServerMatch.Groups.Count > 1)
                {
                    return simpleServerMatch.Groups[1].Value.Trim();
                }
            }
            catch
            {
                // If regex fails, return null
            }
            
            return null;
        }
        
        // Helper method to extract database name from a connection string
        private string ExtractDatabaseFromConnectionString(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                return null;
                
            try
            {
                // Try Initial Catalog format
                var initialCatalogMatch = Regex.Match(connectionString, @"Initial Catalog\s*=\s*([^;]+)", RegexOptions.IgnoreCase);
                if (initialCatalogMatch.Success && initialCatalogMatch.Groups.Count > 1)
                {
                    return initialCatalogMatch.Groups[1].Value.Trim();
                }
                
                // Try Database format
                var databaseMatch = Regex.Match(connectionString, @"Database\s*=\s*([^;]+)", RegexOptions.IgnoreCase);
                if (databaseMatch.Success && databaseMatch.Groups.Count > 1)
                {
                    return databaseMatch.Groups[1].Value.Trim();
                }
            }
            catch
            {
                // If regex fails, return null
            }
            
            return null;
        }

        [HttpPost("connections")]
        public IActionResult SaveConnection([FromBody] object connection)
        {
            _logger.LogInformation("SaveConnection endpoint called");
            return Ok(new { id = 0, isUpdate = false });
        }

        [HttpPost("configurations")]
        public IActionResult SaveConfiguration([FromBody] object configuration)
        {
            _logger.LogInformation("SaveConfiguration endpoint called");
            return Ok(new { id = 0 });
        }

        [HttpPost("configurations/{id}/execute")]
        public IActionResult ExecuteDataTransfer(int id)
        {
            _logger.LogInformation("ExecuteDataTransfer endpoint called for ID: {Id}", id);
            return NotFound($"Configuration with ID {id} not found");
        }

        [HttpPost("configurations/{id}/test")]
        public IActionResult TestConfiguration(int id)
        {
            _logger.LogInformation("TestConfiguration endpoint called for ID: {Id}", id);
            return NotFound($"Configuration with ID {id} not found");
        }

        [HttpGet("test-connection")]
        public async Task<IActionResult> TestConnection()
        {
            try
            {
                // Log the connection string (without sensitive info) for debugging
                string server = GetServerFromConnectionString(_connectionString);
                _logger.LogInformation("Testing connection to database server: {Server}", server);

                // Use the actual connection
                using var connection = new SqlConnection(_connectionString);

                _logger.LogInformation("Connection created, attempting to open...");
                await connection.OpenAsync();

                _logger.LogInformation("Connection opened successfully to database: {Database}", connection.Database);

                return Ok(new {
                    success = true,
                    message = "Connection successful",
                    server,
                    database = connection.Database
                });
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "SQL Error testing database connection. Error code: {ErrorCode}, State: {State}, Server: {Server}",
                    sqlEx.Number, sqlEx.State, sqlEx.Server);

                return StatusCode(500, new {
                    success = false,
                    message = "SQL Connection failed",
                    error = sqlEx.Message,
                    errorCode = sqlEx.Number,
                    state = sqlEx.State,
                    server = sqlEx.Server,
                    innerException = sqlEx.InnerException?.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing database connection");
                return StatusCode(500, new {
                    success = false,
                    message = "Connection failed",
                    error = ex.Message,
                    exceptionType = ex.GetType().Name,
                    innerException = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        // Helper methods moved here to eliminate dependency on DataTransferService

        // Helper class to store connection string update parameters
        public class ConnectionStringUpdateDto
        {
            public string? Server { get; set; }
            public string? Database { get; set; }
            public string? Username { get; set; }
            public string? Password { get; set; }
            public string? ConnectionString { get; set; } // Add direct connection string support
        }

        [HttpPost("update-connection")]
        public IActionResult UpdateConnectionString([FromBody] ConnectionStringUpdateDto connectionInfo)
        {
            try
            {
                // If a full connection string is provided, use it directly
                if (!string.IsNullOrWhiteSpace(connectionInfo.ConnectionString))
                {
                    _connectionString = connectionInfo.ConnectionString;
                    
                    _logger.LogInformation("Connection string updated directly");
                    
                    return Ok(new {
                        message = "Connection string updated successfully and will be used for future requests."
                    });
                }
                
                // Otherwise, validate individual components
                if (connectionInfo == null || string.IsNullOrWhiteSpace(connectionInfo.Server) ||
                    string.IsNullOrWhiteSpace(connectionInfo.Database) ||
                    string.IsNullOrWhiteSpace(connectionInfo.Username) ||
                    string.IsNullOrWhiteSpace(connectionInfo.Password))
                {
                    return BadRequest("All connection parameters are required (Server, Database, Username, Password) or provide a full ConnectionString");
                }

                // Build the connection string
                string newConnectionString = $"Server=tcp:{connectionInfo.Server},1433;Database={connectionInfo.Database};User ID={connectionInfo.Username};Password={connectionInfo.Password};Encrypt=true;Connection Timeout=30;";

                // Update the connection string in memory
                _connectionString = newConnectionString;

                _logger.LogInformation("Connection string updated to: {Server}, Database: {Database}",
                    GetServerFromConnectionString(_connectionString), connectionInfo.Database);

                return Ok(new {
                    message = "Connection string updated successfully and will be used for future requests."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating connection string");
                return StatusCode(500, "An error occurred while updating the connection string");
            }
        }

        [HttpPost("use-mock-data")]
        public IActionResult UseMockData()
        {
            _logger.LogInformation("UseMockData endpoint called");
            // Create a simple response with mock data
            var mockConfigurations = new List<object>
            {
                new
                {
                    ConfigurationId = 1,
                    ConfigurationName = "Daily Player Actions Transfer",
                    Description = "Transfer daily player actions from ProgressPlay to MCP Analytics",
                    // Other mock data
                }
            };

            return Ok(mockConfigurations);
        }

        // Endpoints for data transfer metrics

        /// <summary>
        /// Gets the run history for a specific data transfer configuration
        /// </summary>
        /// <param name="configurationId">The ID of the data transfer configuration</param>
        /// <param name="limit">Optional limit on number of runs to return</param>
        /// <returns>List of run history entries</returns>
        [HttpGet("configurations/{configurationId}/runs")]
        public async Task<IActionResult> GetConfigurationRunHistory(int configurationId, [FromQuery] int? limit = null)
        {
            try
            {
                _logger.LogInformation("GetConfigurationRunHistory endpoint called for configuration ID: {ConfigurationId}", configurationId);
                // Convert nullable int to int for the service call
                var runs = await _metricsService.GetRunHistoryAsync(configurationId, limit ?? 100);
                return Ok(new { values = runs });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving run history for configuration ID: {ConfigurationId}", configurationId);
                return StatusCode(500, new { message = "Error retrieving run history", error = ex.Message });
            }
        }

        /// <summary>
        /// Gets detailed information about a specific data transfer run
        /// </summary>
        /// <param name="runId">The ID of the data transfer run</param>
        /// <returns>Detailed run information</returns>
        [HttpGet("runs/{runId}")]
        public async Task<IActionResult> GetRunById(int runId)
        {
            try
            {
                _logger.LogInformation("GetRunById endpoint called for run ID: {RunId}", runId);
                var run = await _metricsService.GetRunByIdAsync(runId);
                
                if (run == null)
                {
                    return NotFound($"Run with ID {runId} not found");
                }
                
                return Ok(run);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving run details for run ID: {RunId}", runId);
                return StatusCode(500, new { message = "Error retrieving run details", error = ex.Message });
            }
        }

        /// <summary>
        /// Gets metrics for tables processed in a specific data transfer run
        /// </summary>
        /// <param name="runId">The ID of the data transfer run</param>
        /// <returns>Metrics for each table in the run</returns>
        [HttpGet("runs/{runId}/metrics")]
        public async Task<IActionResult> GetRunMetrics(int runId)
        {
            try
            {
                _logger.LogInformation("GetRunMetrics endpoint called for run ID: {RunId}", runId);
                var metrics = await _metricsService.GetRunMetricsAsync(runId);
                
                if (metrics == null || !metrics.Any())
                {
                    return NotFound($"No metrics found for run ID {runId}");
                }
                
                return Ok(new { values = metrics });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving metrics for run ID: {RunId}", runId);
                return StatusCode(500, new { message = "Error retrieving run metrics", error = ex.Message });
            }
        }

        [HttpGet("runs")]
        public async Task<IActionResult> GetAllRunHistory([FromQuery] int configurationId = 0, [FromQuery] int limit = 50)
        {
            _logger.LogInformation("GetAllRunHistory endpoint called. ConfigurationId: {ConfigId}, Limit: {Limit}", 
                configurationId, limit);
            
            try
            {
                var runHistory = await _metricsService.GetRunHistoryAsync(configurationId, limit);
                return Ok(new { values = runHistory });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving run history");
                return StatusCode(500, "An error occurred while retrieving run history");
            }
        }

        // Remove this duplicate method since it's replaced by GetRunById above
        // [HttpGet("runs/{id}")]
        // public async Task<IActionResult> GetRun(int id)
        // {
        //     _logger.LogInformation("GetRun endpoint called for RunId: {RunId}", id);
        //     
        //     try
        //     {
        //         var run = await _metricsService.GetRunByIdAsync(id);
        //         
        //         if (run == null)
        //         {
        //             return NotFound($"Run with ID {id} not found");
        //         }
        //         
        //         return Ok(run);
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogError(ex, "Error retrieving run details");
        //         return StatusCode(500, "An error occurred while retrieving run details");
        //     }
        // }

        // Support class needed by DataTransferController
        public class ConnectionStringHasher
        {
            private readonly ILogger _logger;

            public ConnectionStringHasher(ILogger logger)
            {
                _logger = logger;
            }

            // Add any methods needed for connection string hashing
        }

        // Simple interface to resolve connection strings
        public interface IConnectionStringResolverService
        {
            Task<string> ResolveConnectionStringAsync(string connectionStringTemplate);
        }

        // Helper method to extract the server name from a connection string for logging
        private string GetServerFromConnectionString(string connectionString)
        {
            try
            {
                // Simple regex to extract server name from connection string
                var match = Regex.Match(connectionString, @"Server\s*=\s*tcp:([^,]+)", RegexOptions.IgnoreCase);
                if (match.Success && match.Groups.Count > 1)
                {
                    return match.Groups[1].Value;
                }
                
                // Fallback for other formats
                match = Regex.Match(connectionString, @"Data Source\s*=\s*([^;]+)", RegexOptions.IgnoreCase);
                if (match.Success && match.Groups.Count > 1)
                {
                    return match.Groups[1].Value;
                }
                
                return "unknown-server";
            }
            catch
            {
                return "error-parsing-server";
            }
        }
    }
}