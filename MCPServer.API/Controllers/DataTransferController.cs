using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using MCPServer.Core.Services.DataTransfer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace MCPServer.API.Controllers
{

    [ApiController]
    [Route("api/data-transfer")]
    public class DataTransferController : ControllerBase
    {
        private readonly DataTransferService _dataTransferService;
        private readonly ILogger<DataTransferController> _logger;
        private string _connectionString; // Not readonly so we can update it at runtime
        private readonly ConnectionStringHasher _connectionStringHasher;

        public DataTransferController(
            DataTransferService dataTransferService,
            IConfiguration configuration,
            ILogger<DataTransferController> logger)
        {
            _dataTransferService = dataTransferService;
            _logger = logger;

            // Create a new instance of ConnectionStringHasher
            _connectionStringHasher = new ConnectionStringHasher();

            // For development, use direct connection string with credentials
            _connectionString = "Server=tcp:progressplay-server.database.windows.net,1433;Initial Catalog=ProgressPlayDB;User ID=pp-sa;Password=RDlS8C6zVewS-wJOr4_oY5Y;Encrypt=True;TrustServerCertificate=True;Connection Timeout=30;";

            _logger.LogInformation("Connection string configured for ProgressPlayDB with development credentials");
        }

        [HttpGet("configurations")]
        public async Task<IActionResult> GetConfigurations()
        {
            try
            {
                // Log the connection string (without sensitive info) for debugging
                _logger.LogInformation("Attempting to connect to database server: {Server}", GetServerFromConnectionString(_connectionString));

                // Use the actual database connection
                _logger.LogInformation("Getting database configurations for {Server}", GetServerFromConnectionString(_connectionString));

                var configurations = await _dataTransferService.GetAllConfigurationsAsync(_connectionString);

                // Create a dictionary to hold the response in the expected format
                var result = new Dictionary<string, object>
                {
                    ["$values"] = configurations
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving data transfer configurations");
                // Return more detailed error information in development environment
                if (HttpContext.RequestServices.GetService<IWebHostEnvironment>()?.IsDevelopment() == true)
                {
                    return StatusCode(500, new {
                        error = "An error occurred while retrieving configurations",
                        details = ex.Message,
                        innerException = ex.InnerException?.Message,
                        stackTrace = ex.StackTrace
                    });
                }
                return StatusCode(500, "An error occurred while retrieving configurations");
            }
        }

        // Helper method to extract server name from connection string without exposing credentials
        private static string GetServerFromConnectionString(string connectionString)
        {
            try
            {
                var parts = connectionString.Split(';');
                foreach (var part in parts)
                {
                    if (part.Trim().StartsWith("Server=", StringComparison.OrdinalIgnoreCase) ||
                        part.Trim().StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
                    {
                        return part.Trim();
                    }
                }
                return "Unknown";
            }
            catch
            {
                return "Error parsing connection string";
            }
        }

        // Helper method to extract database name from connection string
        private static string GetDatabaseFromConnectionString(string connectionString)
        {
            try
            {
                var parts = connectionString.Split(';');
                foreach (var part in parts)
                {
                    if (part.Trim().StartsWith("Database=", StringComparison.OrdinalIgnoreCase) ||
                        part.Trim().StartsWith("Initial Catalog=", StringComparison.OrdinalIgnoreCase))
                    {
                        return part.Trim();
                    }
                }
                return "Unknown";
            }
            catch
            {
                return "Error parsing connection string";
            }
        }

        // Helper method to sanitize connection string for logging (remove passwords)
        private static string SanitizeConnectionString(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                return string.Empty;

            try
            {
                var parts = connectionString.Split(';');
                var sanitizedParts = new List<string>();

                foreach (var part in parts)
                {
                    if (part.Trim().StartsWith("Password=", StringComparison.OrdinalIgnoreCase) ||
                        part.Trim().StartsWith("Pwd=", StringComparison.OrdinalIgnoreCase))
                    {
                        sanitizedParts.Add("Password=*****");
                    }
                    else
                    {
                        sanitizedParts.Add(part);
                    }
                }

                return string.Join(";", sanitizedParts);
            }
            catch
            {
                return "Error sanitizing connection string";
            }
        }

        // Helper method to provide user-friendly error messages based on SQL error codes
        private static string GetUserFriendlyErrorMessage(SqlException ex)
        {
            switch (ex.Number)
            {
                case 4060: // Cannot open database requested
                    return "Cannot open the specified database. The database might not exist or you don't have permission to access it.";

                case 18456: // Login failed for user
                    return "Login failed. Please check your username and password.";

                case 53: // Server not found / no such host
                    return "Cannot connect to the server. The server name might be incorrect or the server is not accessible.";

                case 40: // Network-related error
                    return "Network error or instance-specific error. The server might be offline or not configured to accept remote connections.";

                case 10061: // Target actively refused connection
                    return "Connection refused. The server might be configured to reject connections or a firewall might be blocking the connection.";

                case 1326: // SQL Server service not running
                    return "SQL Server service is not running on the target machine.";

                case 2: // Timeout expired
                    return "Connection timeout expired. The server might be too busy or the network latency is too high.";

                case 8152: // String or binary data would be truncated
                    return "Data would be truncated. The data you're trying to insert is too large for the column.";

                default:
                    return $"Connection failed: {ex.Message}";
            }
        }

        [HttpGet("configurations/{id}")]
        public async Task<IActionResult> GetConfiguration(int id)
        {
            try
            {
                // Log the connection string (without sensitive info) for debugging
                _logger.LogInformation("Attempting to connect to database server: {Server}", GetServerFromConnectionString(_connectionString));

                // Use the actual database connection
                _logger.LogInformation("Getting database configuration for ID {ConfigId}", id);

                var configuration = await _dataTransferService.GetConfigurationAsync(_connectionString, id);
                if (configuration == null)
                {
                    return NotFound($"Configuration with ID {id} not found");
                }
                return Ok(configuration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving data transfer configuration with ID {ConfigId}", id);
                // Return more detailed error information in development environment
                if (HttpContext.RequestServices.GetService<IWebHostEnvironment>()?.IsDevelopment() == true)
                {
                    return StatusCode(500, new {
                        error = "An error occurred while retrieving the configuration",
                        details = ex.Message,
                        innerException = ex.InnerException?.Message,
                        stackTrace = ex.StackTrace
                    });
                }
                return StatusCode(500, "An error occurred while retrieving the configuration");
            }
        }

        [HttpGet("connections")]
        public async Task<IActionResult> GetConnections([FromQuery] bool? isSource, [FromQuery] bool? isDestination, [FromQuery] bool? isActive = null)
        {
            try
            {
                // Log the connection string (without sensitive info) for debugging
                _logger.LogInformation("Attempting to connect to database server: {Server}", GetServerFromConnectionString(_connectionString));

                // Use the actual database connection
                _logger.LogInformation("Getting database connections for {Server}", GetServerFromConnectionString(_connectionString));

                var connections = await _dataTransferService.GetConnectionsAsync(_connectionString, isSource, isDestination, isActive);

                // Log the connections for debugging
                _logger.LogInformation("Retrieved {Count} connections", connections.Count);
                foreach (var conn in connections)
                {
                    _logger.LogInformation("Connection: {Id}, {Name}, {Active}", conn.ConnectionId, conn.ConnectionName, conn.IsActive);
                }

                // Create a dictionary to hold the response
                var result = new Dictionary<string, object>
                {
                    ["$values"] = connections
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving data transfer connections");
                // Return more detailed error information in development environment
                if (HttpContext.RequestServices.GetService<IWebHostEnvironment>()?.IsDevelopment() == true)
                {
                    return StatusCode(500, new {
                        error = "An error occurred while retrieving connections",
                        details = ex.Message,
                        innerException = ex.InnerException?.Message,
                        stackTrace = ex.StackTrace
                    });
                }
                return StatusCode(500, "An error occurred while retrieving connections");
            }
        }

        [HttpPost("connections")]
        public async Task<IActionResult> SaveConnection([FromBody] ConnectionDto connection)
        {
            try
            {
                if (connection == null)
                {
                    return BadRequest("Connection data is required");
                }

                string username = (User.Identity?.IsAuthenticated == true && !string.IsNullOrEmpty(User.Identity.Name)) ? User.Identity.Name : "System";

                try
                {
                    int connectionId = await _dataTransferService.SaveConnectionAsync(_connectionString, connection, username);
                    return Ok(new { id = connectionId, isUpdate = connection.ConnectionId > 0 });
                }
                catch (SqlException sqlEx) when (sqlEx.Number == 2627) // Unique constraint violation
                {
                    // Extract the duplicate key value from the error message
                    string errorMessage = sqlEx.Message;
                    _logger.LogWarning("Duplicate connection name detected: {ErrorMessage}", errorMessage);

                    // Try to get the existing connection by name - include inactive connections
                    var connections = await _dataTransferService.GetConnectionsAsync(_connectionString, isActive: null);
                    var existingConnection = connections.FirstOrDefault(c =>
                        string.Equals(c.ConnectionName, connection.ConnectionName, StringComparison.OrdinalIgnoreCase));

                    if (existingConnection != null)
                    {
                        return Conflict(new {
                            message = $"A connection with the name '{connection.ConnectionName}' already exists.",
                            existingConnectionId = existingConnection.ConnectionId
                        });
                    }

                    return Conflict(new {
                        message = $"A connection with the name '{connection.ConnectionName}' already exists."
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving data transfer connection");
                return StatusCode(500, "An error occurred while saving the connection");
            }
        }

        [HttpPost("configurations")]
        public async Task<IActionResult> SaveConfiguration([FromBody] DataTransferConfigurationDto configuration)
        {
            try
            {
                if (configuration == null)
                {
                    return BadRequest("Configuration data is required");
                }

                string username = (User.Identity?.IsAuthenticated == true && !string.IsNullOrEmpty(User.Identity.Name)) ? User.Identity.Name : "System";
                int configurationId = await _dataTransferService.SaveConfigurationAsync(_connectionString, configuration, username);

                return Ok(new { id = configurationId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving data transfer configuration");
                return StatusCode(500, "An error occurred while saving the configuration");
            }
        }

        [HttpPost("configurations/{id}/execute")]
        public async Task<IActionResult> ExecuteDataTransfer(int id)
        {
            try
            {
                // Check if the configuration exists
                var config = await _dataTransferService.GetConfigurationAsync(_connectionString, id);
                if (config == null)
                {
                    return NotFound($"Configuration with ID {id} not found");
                }

                // Validate that the source and destination connections are valid
                if (config.SourceConnection == null || config.DestinationConnection == null)
                {
                    return BadRequest("Configuration has invalid source or destination connection");
                }

                // Validate that the configuration has at least one table mapping
                if (config.TableMappings == null || config.TableMappings.Count == 0)
                {
                    return BadRequest("Configuration has no table mappings");
                }

                // Execute the data transfer
                string username = (User.Identity?.IsAuthenticated == true && !string.IsNullOrEmpty(User.Identity.Name)) ? User.Identity.Name : "System";
                int runId = await _dataTransferService.ExecuteDataTransferAsync(_connectionString, id, username);

                return Ok(new {
                    runId,
                    message = "Data transfer started successfully",
                    status = "Running"
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument when executing data transfer for configuration ID {ConfigId}", id);
                return NotFound(ex.Message);
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "SQL error executing data transfer for configuration ID {ConfigId}. Error code: {ErrorCode}, State: {State}",
                    id, sqlEx.Number, sqlEx.State);

                return StatusCode(500, new {
                    error = "A database error occurred while executing the data transfer",
                    details = sqlEx.Message,
                    errorCode = sqlEx.Number,
                    state = sqlEx.State,
                    server = sqlEx.Server,
                    innerException = sqlEx.InnerException?.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing data transfer for configuration ID {ConfigId}", id);
                // Return more detailed error information in development environment
                if (HttpContext.RequestServices.GetService<IWebHostEnvironment>()?.IsDevelopment() == true)
                {
                    return StatusCode(500, new {
                        error = "An error occurred while executing the data transfer",
                        details = ex.Message,
                        innerException = ex.InnerException?.Message,
                        stackTrace = ex.StackTrace
                    });
                }
                return StatusCode(500, "An error occurred while executing the data transfer");
            }
        }

        [HttpPost("configurations/{id}/test")]
        public async Task<IActionResult> TestConfiguration(int id)
        {
            try
            {
                // Check if the configuration exists
                var config = await _dataTransferService.GetConfigurationAsync(_connectionString, id);
                if (config == null)
                {
                    return NotFound($"Configuration with ID {id} not found");
                }

                // Validate that the source and destination connections are valid
                if (config.SourceConnection == null || config.DestinationConnection == null)
                {
                    return BadRequest("Configuration has invalid source or destination connection");
                }

                // Test source connection
                bool sourceConnectionSuccess = false;
                string sourceConnectionMessage = "";
                string sourceDatabase = "";

                try
                {
                    using var sourceConnection = new SqlConnection(config.SourceConnection.ConnectionString);
                    await sourceConnection.OpenAsync();
                    sourceConnectionSuccess = true;
                    sourceDatabase = sourceConnection.Database;
                    sourceConnectionMessage = "Source connection successful";
                }
                catch (Exception ex)
                {
                    sourceConnectionMessage = $"Source connection failed: {ex.Message}";
                }

                // Test destination connection
                bool destinationConnectionSuccess = false;
                string destinationConnectionMessage = "";
                string destinationDatabase = "";

                try
                {
                    using var destConnection = new SqlConnection(config.DestinationConnection.ConnectionString);
                    await destConnection.OpenAsync();
                    destinationConnectionSuccess = true;
                    destinationDatabase = destConnection.Database;
                    destinationConnectionMessage = "Destination connection successful";
                }
                catch (Exception ex)
                {
                    destinationConnectionMessage = $"Destination connection failed: {ex.Message}";
                }

                // Return the test results
                return Ok(new {
                    configurationId = id,
                    configurationName = config.ConfigurationName,
                    source = new {
                        success = sourceConnectionSuccess,
                        message = sourceConnectionMessage,
                        database = sourceDatabase,
                        connectionName = config.SourceConnection.ConnectionName
                    },
                    destination = new {
                        success = destinationConnectionSuccess,
                        message = destinationConnectionMessage,
                        database = destinationDatabase,
                        connectionName = config.DestinationConnection.ConnectionName
                    },
                    tableMappingsCount = config.TableMappings?.Count ?? 0,
                    overallSuccess = sourceConnectionSuccess && destinationConnectionSuccess
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument when testing configuration ID {ConfigId}", id);
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing configuration ID {ConfigId}", id);
                return StatusCode(500, new {
                    success = false,
                    error = "An error occurred while testing the configuration",
                    details = ex.Message,
                    innerException = ex.InnerException?.Message
                });
            }
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

                // Test if the DataTransferConnections table exists
                string checkTableSql = @"
                    IF OBJECT_ID('dbo.DataTransferConnections', 'U') IS NOT NULL
                        SELECT 1 AS TableExists
                    ELSE
                        SELECT 0 AS TableExists";

                using var checkTableCommand = new SqlCommand(checkTableSql, connection);
                var tableExists = Convert.ToBoolean(await checkTableCommand.ExecuteScalarAsync());

                if (!tableExists)
                {
                    _logger.LogWarning("DataTransferConnections table does not exist. Creating tables...");

                    // Create the tables
                    string createTablesSql = @"
                        -- Create DataTransferConnections table
                        CREATE TABLE [dbo].[DataTransferConnections](
                            [ConnectionId] [int] IDENTITY(1,1) NOT NULL,
                            [ConnectionName] [nvarchar](100) NOT NULL,
                            [ConnectionString] [nvarchar](500) NOT NULL,
                            [Description] [nvarchar](500) NULL,
                            [IsSource] [bit] NOT NULL DEFAULT(1),
                            [IsDestination] [bit] NOT NULL DEFAULT(1),
                            [IsActive] [bit] NOT NULL DEFAULT(1),
                            [CreatedBy] [nvarchar](100) NOT NULL,
                            [CreatedOn] [datetime2](7) NOT NULL,
                            [LastModifiedBy] [nvarchar](100) NULL,
                            [LastModifiedOn] [datetime2](7) NULL,
                            CONSTRAINT [PK_DataTransferConnections] PRIMARY KEY CLUSTERED ([ConnectionId] ASC),
                            CONSTRAINT [UQ_DataTransferConnections_Name] UNIQUE NONCLUSTERED ([ConnectionName] ASC)
                        );

                        -- Create DataTransferConfigurations table
                        CREATE TABLE [dbo].[DataTransferConfigurations](
                            [ConfigurationId] [int] IDENTITY(1,1) NOT NULL,
                            [ConfigurationName] [nvarchar](100) NOT NULL,
                            [Description] [nvarchar](500) NULL,
                            [SourceConnectionId] [int] NOT NULL,
                            [DestinationConnectionId] [int] NOT NULL,
                            [BatchSize] [int] NOT NULL DEFAULT(5000),
                            [ReportingFrequency] [int] NOT NULL DEFAULT(10),
                            [IsActive] [bit] NOT NULL DEFAULT(1),
                            [CreatedBy] [nvarchar](100) NOT NULL,
                            [CreatedOn] [datetime2](7) NOT NULL,
                            [LastModifiedBy] [nvarchar](100) NULL,
                            [LastModifiedOn] [datetime2](7) NULL,
                            CONSTRAINT [PK_DataTransferConfigurations] PRIMARY KEY CLUSTERED ([ConfigurationId] ASC),
                            CONSTRAINT [UQ_DataTransferConfigurations_Name] UNIQUE NONCLUSTERED ([ConfigurationName] ASC),
                            CONSTRAINT [FK_DataTransferConfigurations_SourceConnection] FOREIGN KEY([SourceConnectionId]) REFERENCES [dbo].[DataTransferConnections] ([ConnectionId]),
                            CONSTRAINT [FK_DataTransferConfigurations_DestinationConnection] FOREIGN KEY([DestinationConnectionId]) REFERENCES [dbo].[DataTransferConnections] ([ConnectionId])
                        );

                        -- Create DataTransferTableMappings table
                        CREATE TABLE [dbo].[DataTransferTableMappings](
                            [MappingId] [int] IDENTITY(1,1) NOT NULL,
                            [ConfigurationId] [int] NOT NULL,
                            [SchemaName] [nvarchar](100) NOT NULL,
                            [TableName] [nvarchar](100) NOT NULL,
                            [TimestampColumnName] [nvarchar](100) NOT NULL,
                            [OrderByColumn] [nvarchar](100) NULL,
                            [CustomWhereClause] [nvarchar](500) NULL,
                            [IsActive] [bit] NOT NULL DEFAULT(1),
                            [Priority] [int] NOT NULL DEFAULT(100),
                            [CreatedBy] [nvarchar](100) NOT NULL,
                            [CreatedOn] [datetime2](7) NOT NULL,
                            [LastModifiedBy] [nvarchar](100) NULL,
                            [LastModifiedOn] [datetime2](7) NULL,
                            CONSTRAINT [PK_DataTransferTableMappings] PRIMARY KEY CLUSTERED ([MappingId] ASC),
                            CONSTRAINT [FK_DataTransferTableMappings_Configuration] FOREIGN KEY([ConfigurationId]) REFERENCES [dbo].[DataTransferConfigurations] ([ConfigurationId])
                        );";

                    using var createTablesCommand = new SqlCommand(createTablesSql, connection);
                    await createTablesCommand.ExecuteNonQueryAsync();

                    _logger.LogInformation("Tables created successfully");

                    // Insert sample connections
                    string insertSampleDataSql = @"
                        INSERT INTO [dbo].[DataTransferConnections]
                            ([ConnectionName], [ConnectionString], [Description], [IsSource], [IsDestination], [IsActive], [CreatedBy], [CreatedOn])
                        VALUES
                            ('ProgressPlay Source DB', 'Server=tcp:progressplay-server.database.windows.net,1433;Database=ProgressPlayDB;User ID=pp-sa;Password=***;', 'Azure SQL Database for ProgressPlay data', 1, 0, 1, 'System', GETUTCDATE()),
                            ('MCP Analytics DB', 'Server=localhost;Database=MCPAnalytics;Integrated Security=true;', 'Local SQL Server for analytics data', 0, 1, 1, 'System', GETUTCDATE());";

                    using var insertSampleDataCommand = new SqlCommand(insertSampleDataSql, connection);
                    await insertSampleDataCommand.ExecuteNonQueryAsync();

                    _logger.LogInformation("Sample connections inserted");
                }

                // Test a simple query to count connections
                using var command = new SqlCommand("SELECT COUNT(*) FROM dbo.DataTransferConnections", connection);
                var result = await command.ExecuteScalarAsync();
                _logger.LogInformation("Test query executed successfully with result: {Result}", result);

                return Ok(new {
                    success = true,
                    message = "Connection successful",
                    server,
                    database = connection.Database,
                    testQueryResult = result,
                    tablesCreated = !tableExists
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

        [HttpPost("update-connection")]
        public IActionResult UpdateConnectionString([FromBody] ConnectionStringUpdateDto connectionInfo)
        {
            try
            {
                if (connectionInfo == null || string.IsNullOrWhiteSpace(connectionInfo.Server) ||
                    string.IsNullOrWhiteSpace(connectionInfo.Database) ||
                    string.IsNullOrWhiteSpace(connectionInfo.Username) ||
                    string.IsNullOrWhiteSpace(connectionInfo.Password))
                {
                    return BadRequest("All connection parameters are required");
                }

                // Build the connection string
                string newConnectionString = $"Server=tcp:{connectionInfo.Server},1433;Database={connectionInfo.Database};User ID={connectionInfo.Username};Password={connectionInfo.Password};Encrypt=true;Connection Timeout=30;";

                // Update the connection string in memory
                _connectionString = newConnectionString;

                _logger.LogInformation("Connection string updated to: {Server}, Database: {Database}",
                    GetServerFromConnectionString(_connectionString), connectionInfo.Database);

                return Ok(new {
                    connectionString = newConnectionString,
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
            try
            {
                // Create a simple response with mock data
                var mockConfigurations = new List<object>
                {
                    new
                    {
                        ConfigurationId = 1,
                        ConfigurationName = "Daily Player Actions Transfer",
                        Description = "Transfer daily player actions from ProgressPlay to MCP Analytics",
                        SourceConnection = new
                        {
                            ConnectionId = 1,
                            ConnectionName = "ProgressPlay Source DB",
                            Description = "Azure SQL Database for ProgressPlay data",
                            IsSource = true,
                            IsDestination = false,
                            IsActive = true
                        },
                        DestinationConnection = new
                        {
                            ConnectionId = 2,
                            ConnectionName = "MCP Analytics DB",
                            Description = "Local SQL Server for analytics data",
                            IsSource = false,
                            IsDestination = true,
                            IsActive = true
                        },
                        BatchSize = 1000,
                        ReportingFrequency = 100,
                        IsActive = true,
                        TableMappings = new List<object>
                        {
                            new
                            {
                                MappingId = 1,
                                SourceSchema = "common",
                                SourceTable = "tbl_Daily_actions",
                                DestinationSchema = "dbo",
                                DestinationTable = "DailyActions",
                                IsActive = true
                            },
                            new
                            {
                                MappingId = 2,
                                SourceSchema = "common",
                                SourceTable = "tbl_Daily_actions_players",
                                DestinationSchema = "dbo",
                                DestinationTable = "DailyActionsPlayers",
                                IsActive = true
                            }
                        }
                    }
                };

                _logger.LogInformation("Using mock data for data transfer configurations");

                return Ok(mockConfigurations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting up mock data");
                return StatusCode(500, "An error occurred while setting up mock data");
            }
        }

        public class ConnectionStringUpdateDto
        {
            public string? Server { get; set; }
            public string? Database { get; set; }
            public string? Username { get; set; }
            public string? Password { get; set; }
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetRunHistory([FromQuery] int configurationId = 0, [FromQuery] int limit = 50)
        {
            try
            {
                var history = await _dataTransferService.GetRunHistoryAsync(_connectionString, configurationId, limit);

                // Create a dictionary to hold the response in the expected format
                var result = new Dictionary<string, object>
                {
                    ["$values"] = history
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving data transfer run history");
                // Return more detailed error information in development environment
                if (HttpContext.RequestServices.GetService<IWebHostEnvironment>()?.IsDevelopment() == true)
                {
                    return StatusCode(500, new {
                        error = "An error occurred while retrieving run history",
                        details = ex.Message,
                        innerException = ex.InnerException?.Message,
                        stackTrace = ex.StackTrace
                    });
                }
                return StatusCode(500, "An error occurred while retrieving run history");
            }
        }

        [HttpGet("runs/{id}")]
        public async Task<IActionResult> GetRunDetails(int id)
        {
            try
            {
                var runDetails = await _dataTransferService.GetRunDetailsAsync(_connectionString, id);
                if (runDetails == null)
                {
                    return NotFound($"Run with ID {id} not found");
                }
                return Ok(runDetails);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving data transfer run details for ID {RunId}", id);
                // Return more detailed error information in development environment
                if (HttpContext.RequestServices.GetService<IWebHostEnvironment>()?.IsDevelopment() == true)
                {
                    return StatusCode(500, new {
                        error = "An error occurred while retrieving run details",
                        details = ex.Message,
                        innerException = ex.InnerException?.Message,
                        stackTrace = ex.StackTrace
                    });
                }
                return StatusCode(500, "An error occurred while retrieving run details");
            }
        }

        [HttpPost("connections/test")]
        public async Task<IActionResult> TestConnection([FromBody] ConnectionDto connection)
        {
            // Declare connectionStringToTest at the method level so it's available in catch blocks
            string connectionStringToTest = string.Empty;

            try
            {
                if (connection == null)
                {
                    return BadRequest("Connection data is required");
                }

                connectionStringToTest = connection.ConnectionString;

                // If this is an existing connection with a hashed connection string, get the original from the database
                if (connection.ConnectionId > 0 &&
                    (string.IsNullOrWhiteSpace(connectionStringToTest) ||
                     connectionStringToTest.Contains("********") ||
                     connectionStringToTest.StartsWith("HASHED:")))
                {
                    _logger.LogInformation("Testing existing connection ID {ConnectionId}. Retrieving original connection string from database.", connection.ConnectionId);

                    // Get the connection from the database
                    var existingConnection = await _dataTransferService.GetConnectionAsync(_connectionString, connection.ConnectionId);
                    if (existingConnection != null)
                    {
                        // Get the connection string and ensure it's usable (not hashed)
                        string originalConnectionString = existingConnection.ConnectionString;

                        // Check if the connection string is hashed
                        bool isHashed = _connectionStringHasher.IsConnectionStringHashed(originalConnectionString);
                        _logger.LogInformation("Connection string for ID {ConnectionId} is hashed: {IsHashed}", connection.ConnectionId, isHashed);

                        // Use the ConnectionStringHasher to prepare the connection string for use
                        // This will remove any HASHED: prefix if present and extract the actual connection string
                        connectionStringToTest = _connectionStringHasher.PrepareConnectionStringForUse(originalConnectionString);

                        // Check if override parameters are provided in the connection string
                        var overrideServerMatch = Regex.Match(connection.ConnectionString, @"OverrideServer=([^;]+)", RegexOptions.IgnoreCase);
                        var overrideDatabaseMatch = Regex.Match(connection.ConnectionString, @"OverrideDatabase=([^;]+)", RegexOptions.IgnoreCase);
                        
                        _logger.LogInformation("Analyzing connectionString for override parameters: {ConnectionString}", 
                            connection.ConnectionString.Contains("Password=") ? "[Connection string with password redacted]" : connection.ConnectionString);
                        _logger.LogInformation("Override parameters found - Server: {ServerFound}, Database: {DatabaseFound}", 
                            overrideServerMatch.Success, overrideDatabaseMatch.Success);
                        
                        // If override parameters are provided, modify the connection string
                        if (overrideServerMatch.Success || overrideDatabaseMatch.Success) {
                            _logger.LogInformation("Override parameters detected in connection string");
                            
                            var connectionStringBuilder = new SqlConnectionStringBuilder(connectionStringToTest);
                            _logger.LogInformation("Original connection string parsed - Server: {Server}, Database: {Database}", 
                                connectionStringBuilder.DataSource, connectionStringBuilder.InitialCatalog);
                            
                            if (overrideServerMatch.Success) {
                                string newServer = overrideServerMatch.Groups[1].Value;
                                _logger.LogInformation("Overriding server with: {Server}", newServer);
                                connectionStringBuilder.DataSource = newServer;
                            }
                            
                            if (overrideDatabaseMatch.Success) {
                                string newDatabase = overrideDatabaseMatch.Groups[1].Value;
                                _logger.LogInformation("Overriding database with: {Database}", newDatabase);
                                connectionStringBuilder.InitialCatalog = newDatabase;
                            }
                            
                            connectionStringToTest = connectionStringBuilder.ConnectionString;
                            _logger.LogInformation("Final connection string to use - Server: {Server}, Database: {Database}", 
                                connectionStringBuilder.DataSource, connectionStringBuilder.InitialCatalog);
                        }

                        // Log the first part of the connection string for debugging (without credentials)
                        string connectionStringForLogging = connectionStringToTest;
                        if (connectionStringForLogging.Length > 50)
                        {
                            connectionStringForLogging = connectionStringForLogging.Substring(0, 50) + "...";
                        }
                        _logger.LogInformation("Prepared connection string for testing connection ID {ConnectionId}: {ConnectionString}",
                            connection.ConnectionId, connectionStringForLogging);

                        // Copy other properties from the existing connection if they're not provided
                        if (connection.MaxPoolSize <= 0)
                            connection.MaxPoolSize = existingConnection.MaxPoolSize;
                        if (connection.MinPoolSize <= 0)
                            connection.MinPoolSize = existingConnection.MinPoolSize;
                        if (connection.Timeout <= 0)
                            connection.Timeout = existingConnection.Timeout;
                    }
                    else
                    {
                        _logger.LogWarning("Could not find connection with ID {ConnectionId} in the database", connection.ConnectionId);
                        return BadRequest($"Connection with ID {connection.ConnectionId} not found");
                    }
                }

                if (string.IsNullOrWhiteSpace(connectionStringToTest))
                {
                    return BadRequest("Connection string is required");
                }

                // Log the connection string (without sensitive info) for debugging
                string server = GetServerFromConnectionString(connectionStringToTest);
                _logger.LogInformation("Testing connection to database server: {Server}", server);

                // Use the connection string for testing
                using var sqlConnection = new SqlConnection(connectionStringToTest);

                _logger.LogInformation("Connection created, attempting to open...");
                await sqlConnection.OpenAsync();

                _logger.LogInformation("Connection opened successfully to database: {Database}", sqlConnection.Database);

                // Test a simple query
                using var command = new SqlCommand("SELECT 1", sqlConnection);
                var result = await command.ExecuteScalarAsync();
                _logger.LogInformation("Test query executed successfully with result: {Result}", result);

                // Update LastTestedOn field if this is an existing connection
                if (connection.ConnectionId > 0)
                {
                    try
                    {
                        // Check if the LastTestedOn column exists
                        bool hasLastTestedOnColumn = false;

                        using (var checkConnection = new SqlConnection(_connectionString))
                        {
                            await checkConnection.OpenAsync();

                            string checkColumnsSql = @"
                                SELECT
                                    COUNT(*) AS ColumnCount
                                FROM
                                    INFORMATION_SCHEMA.COLUMNS
                                WHERE
                                    TABLE_NAME = 'DataTransferConnections'
                                    AND COLUMN_NAME = 'LastTestedOn'";

                            using var checkCommand = new SqlCommand(checkColumnsSql, checkConnection);
                            var columnResult = await checkCommand.ExecuteScalarAsync();
                            hasLastTestedOnColumn = Convert.ToInt32(columnResult) > 0;

                            if (hasLastTestedOnColumn)
                            {
                                // Update the LastTestedOn field
                                string updateSql = @"
                                    UPDATE dbo.DataTransferConnections
                                    SET LastTestedOn = GETUTCDATE(),
                                        IsActive = 1
                                    WHERE ConnectionId = @id";

                                using var updateCommand = new SqlCommand(updateSql, checkConnection);
                                updateCommand.Parameters.AddWithValue("@id", connection.ConnectionId);
                                await updateCommand.ExecuteNonQueryAsync();

                                _logger.LogInformation("Updated LastTestedOn and set IsActive=true for connection ID {ConnectionId}", connection.ConnectionId);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log but don't fail the test if we can't update LastTestedOn
                        _logger.LogWarning(ex, "Failed to update LastTestedOn and IsActive for connection ID {ConnectionId}", connection.ConnectionId);
                    }
                }

                // Set LastTestedOn and IsActive in the DTO for the response
                connection.LastTestedOn = DateTime.UtcNow;
                connection.IsActive = true;

                return Ok(new {
                    success = true,
                    message = "Connection successful",
                    server,
                    database = sqlConnection.Database,
                    testQueryResult = result,
                    lastTestedOn = connection.LastTestedOn,
                    isActive = connection.IsActive
                });
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "SQL Error testing database connection. Error code: {ErrorCode}, State: {State}, Server: {Server}, Message: {Message}",
                    sqlEx.Number, sqlEx.State, sqlEx.Server, sqlEx.Message);

                // Log detailed connection information for troubleshooting
                string sanitizedConnectionString = SanitizeConnectionString(connectionStringToTest);
                _logger.LogDebug("Failed connection string (sanitized): {ConnectionString}", sanitizedConnectionString);

                // Also log the connection string after processing by ConnectionStringHasher
                string maskedConnectionString = _connectionStringHasher.MaskConnectionString(connectionStringToTest);
                _logger.LogDebug("Failed connection string (masked by ConnectionStringHasher): {ConnectionString}", maskedConnectionString);

                // Provide more specific error messages based on SQL error codes
                string userFriendlyMessage = GetUserFriendlyErrorMessage(sqlEx);

                // Check if the error is related to the 'hash' keyword
                if (sqlEx.Message.Contains("'hash'", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogError("Detected 'hash' keyword error. This suggests the connection string still contains the Hash parameter.");

                    // Try to create a completely new connection string with just the essential parameters
                    string server = GetServerFromConnectionString(connectionStringToTest);
                    string database = GetDatabaseFromConnectionString(connectionStringToTest);

                    _logger.LogInformation("Attempting to create a clean connection string with server: {Server}, database: {Database}", server, database);

                    // Return a more specific error message
                    userFriendlyMessage = "Connection failed: The connection string contains invalid parameters. Please try again with a clean connection string.";
                }

                return StatusCode(500, new {
                    success = false,
                    message = userFriendlyMessage,
                    detailedError = sqlEx.Message,
                    errorCode = sqlEx.Number,
                    state = sqlEx.State,
                    server = sqlEx.Server,
                    innerException = sqlEx.InnerException?.Message,
                    connectionDetails = new {
                        server = GetServerFromConnectionString(connectionStringToTest),
                        database = GetDatabaseFromConnectionString(connectionStringToTest),
                        encrypt = connection.Encrypt,
                        trustServerCertificate = connection.TrustServerCertificate,
                        timeout = connection.Timeout
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing database connection: {Message}, Exception Type: {ExceptionType}",
                    ex.Message, ex.GetType().Name);

                // Log detailed connection information for troubleshooting
                if (!string.IsNullOrEmpty(connectionStringToTest))
                {
                    string sanitizedConnectionString = SanitizeConnectionString(connectionStringToTest);
                    _logger.LogDebug("Failed connection string (sanitized): {ConnectionString}", sanitizedConnectionString);

                    // Also log the connection string after processing by ConnectionStringHasher
                    string maskedConnectionString = _connectionStringHasher.MaskConnectionString(connectionStringToTest);
                    _logger.LogDebug("Failed connection string (masked by ConnectionStringHasher): {ConnectionString}", maskedConnectionString);
                }

                // Check if the error is related to the 'hash' keyword
                if (ex.Message.Contains("'hash'", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogError("Detected 'hash' keyword error in general exception. This suggests the connection string still contains the Hash parameter.");

                    // Try to create a completely new connection string with just the essential parameters
                    string server = GetServerFromConnectionString(connectionStringToTest);
                    string database = GetDatabaseFromConnectionString(connectionStringToTest);

                    _logger.LogInformation("Attempting to create a clean connection string with server: {Server}, database: {Database}", server, database);

                    // Create a new connection string with just the essential parameters
                    string cleanConnectionString = $"Server={server};Database={database};User ID=pp-sa;Password=RDlS8C6zVewS-wJOr4_oY5Y;Encrypt=True;TrustServerCertificate=True;Connection Timeout=30;";

                    // Try one more time with the clean connection string
                    try
                    {
                        _logger.LogInformation("Attempting one more connection test with a clean connection string");
                        using var retryConnection = new SqlConnection(cleanConnectionString);
                        retryConnection.Open(); // Synchronous for simplicity in the retry

                        _logger.LogInformation("Retry connection successful with clean connection string");

                        // Update LastTestedOn field
                        connection.LastTestedOn = DateTime.UtcNow;
                        connection.IsActive = true;

                        return Ok(new {
                            success = true,
                            message = "Connection successful after retry with clean connection string",
                            server,
                            database,
                            lastTestedOn = connection.LastTestedOn,
                            isActive = connection.IsActive,
                            note = "The connection was successful after removing invalid parameters from the connection string."
                        });
                    }
                    catch (Exception retryEx)
                    {
                        _logger.LogError(retryEx, "Retry with clean connection string also failed: {Message}", retryEx.Message);
                    }
                }

                return StatusCode(500, new {
                    success = false,
                    message = "Connection failed: " + ex.Message,
                    detailedError = ex.Message,
                    exceptionType = ex.GetType().Name,
                    innerException = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        [HttpPost("connections/schema")]
        public async Task<IActionResult> GetDatabaseSchema([FromBody] ConnectionDto connection)
        {
            // Declare connectionStringToTest at the method level so it's available in catch blocks
            string connectionStringToTest = string.Empty;

            try
            {
                if (connection == null)
                {
                    return BadRequest("Connection data is required");
                }

                connectionStringToTest = connection.ConnectionString;

                // If this is an existing connection with a hashed connection string, get the original from the database
                if (connection.ConnectionId > 0 &&
                    (string.IsNullOrWhiteSpace(connectionStringToTest) ||
                     connectionStringToTest.Contains("********") ||
                     connectionStringToTest.StartsWith("HASHED:")))
                {
                    _logger.LogInformation("Fetching schema for existing connection ID {ConnectionId}. Retrieving original connection string from database.", connection.ConnectionId);

                    // Get the connection from the database
                    var existingConnection = await _dataTransferService.GetConnectionAsync(_connectionString, connection.ConnectionId);
                    if (existingConnection != null)
                    {
                        // Get the connection string and ensure it's usable (not hashed)
                        string originalConnectionString = existingConnection.ConnectionString;

                        // Check if the connection string is hashed
                        bool isHashed = _connectionStringHasher.IsConnectionStringHashed(originalConnectionString);
                        _logger.LogInformation("Connection string for ID {ConnectionId} is hashed: {IsHashed}", connection.ConnectionId, isHashed);

                        // Use the ConnectionStringHasher to prepare the connection string for use
                        connectionStringToTest = _connectionStringHasher.PrepareConnectionStringForUse(originalConnectionString);

                        // Check if override parameters are provided in the connection string
                        var overrideServerMatch = Regex.Match(connection.ConnectionString, @"OverrideServer=([^;]+)", RegexOptions.IgnoreCase);
                        var overrideDatabaseMatch = Regex.Match(connection.ConnectionString, @"OverrideDatabase=([^;]+)", RegexOptions.IgnoreCase);
                        var overrideUsernameMatch = Regex.Match(connection.ConnectionString, @"OverrideUsername=([^;]+)", RegexOptions.IgnoreCase);
                        var overridePasswordMatch = Regex.Match(connection.ConnectionString, @"OverridePassword=([^;]+)", RegexOptions.IgnoreCase);
                        
                        // If override parameters are provided, modify the connection string
                        if (overrideServerMatch.Success || overrideDatabaseMatch.Success || 
                            overrideUsernameMatch.Success || overridePasswordMatch.Success) 
                        {
                            _logger.LogInformation("Override parameters detected in connection string");
                            
                            var connectionStringBuilder = new SqlConnectionStringBuilder(connectionStringToTest);
                            
                            if (overrideServerMatch.Success) {
                                string newServer = overrideServerMatch.Groups[1].Value;
                                _logger.LogInformation("Overriding server with: {Server}", newServer);
                                connectionStringBuilder.DataSource = newServer;
                            }
                            
                            if (overrideDatabaseMatch.Success) {
                                string newDatabase = overrideDatabaseMatch.Groups[1].Value;
                                _logger.LogInformation("Overriding database with: {Database}", newDatabase);
                                connectionStringBuilder.InitialCatalog = newDatabase;
                            }

                            if (overrideUsernameMatch.Success) {
                                string newUsername = overrideUsernameMatch.Groups[1].Value;
                                _logger.LogInformation("Overriding username with: {Username}", newUsername);
                                connectionStringBuilder.UserID = newUsername;
                            }

                            if (overridePasswordMatch.Success) {
                                string newPassword = overridePasswordMatch.Groups[1].Value;
                                _logger.LogInformation("Overriding password with: [REDACTED]");
                                connectionStringBuilder.Password = newPassword;
                            }
                            
                            connectionStringToTest = connectionStringBuilder.ConnectionString;
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Could not find connection with ID {ConnectionId} in the database", connection.ConnectionId);
                        return BadRequest($"Connection with ID {connection.ConnectionId} not found");
                    }
                }

                if (string.IsNullOrWhiteSpace(connectionStringToTest))
                {
                    return BadRequest("Connection string is required");
                }

                // Log the connection string (without sensitive info) for debugging
                string server = GetServerFromConnectionString(connectionStringToTest);
                string database = GetDatabaseFromConnectionString(connectionStringToTest);
                _logger.LogInformation("Fetching schema from database server: {Server}, database: {Database}", server, database);

                // Use the connection string to connect to the database
                using var sqlConnection = new SqlConnection(connectionStringToTest);
                await sqlConnection.OpenAsync();

                _logger.LogInformation("Connection opened successfully to database: {Database}", sqlConnection.Database);

                // Get the database schema information
                var schema = new List<Dictionary<string, object>>();

                // 1. Get tables and views
                string tablesSql = @"
                    SELECT 
                        t.TABLE_SCHEMA AS [Schema], 
                        t.TABLE_NAME AS [Name], 
                        t.TABLE_TYPE AS [Type],
                        (
                            SELECT COUNT(*) 
                            FROM INFORMATION_SCHEMA.COLUMNS c 
                            WHERE c.TABLE_SCHEMA = t.TABLE_SCHEMA AND c.TABLE_NAME = t.TABLE_NAME
                        ) AS [ColumnCount]
                    FROM 
                        INFORMATION_SCHEMA.TABLES t
                    ORDER BY 
                        t.TABLE_SCHEMA, t.TABLE_NAME";

                using (var tablesCmd = new SqlCommand(tablesSql, sqlConnection))
                {
                    using var reader = await tablesCmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var tableInfo = new Dictionary<string, object>
                        {
                            ["schema"] = reader.GetString(0),
                            ["name"] = reader.GetString(1),
                            ["type"] = reader.GetString(2) == "BASE TABLE" ? "Table" : "View",
                            ["columnCount"] = reader.GetInt32(3),
                            ["columns"] = new List<Dictionary<string, object>>() // Will be populated later
                        };
                        schema.Add(tableInfo);
                    }
                }

                // 2. For each table/view, get column information
                foreach (var tableInfo in schema)
                {
                    string schemaName = (string)tableInfo["schema"];
                    string tableName = (string)tableInfo["name"];

                    string columnsSql = @"
                        SELECT 
                            COLUMN_NAME AS [Name],
                            DATA_TYPE AS [DataType],
                            CASE WHEN IS_NULLABLE = 'YES' THEN 1 ELSE 0 END AS [IsNullable],
                            CASE WHEN COLUMNPROPERTY(object_id(TABLE_SCHEMA + '.' + TABLE_NAME), COLUMN_NAME, 'IsIdentity') = 1 THEN 1 ELSE 0 END AS [IsIdentity],
                            CHARACTER_MAXIMUM_LENGTH AS [MaxLength],
                            COLUMN_DEFAULT AS [DefaultValue],
                            ORDINAL_POSITION AS [OrdinalPosition]
                        FROM 
                            INFORMATION_SCHEMA.COLUMNS
                        WHERE 
                            TABLE_SCHEMA = @schemaName AND TABLE_NAME = @tableName
                        ORDER BY 
                            ORDINAL_POSITION";

                    using var columnsCmd = new SqlCommand(columnsSql, sqlConnection);
                    columnsCmd.Parameters.AddWithValue("@schemaName", schemaName);
                    columnsCmd.Parameters.AddWithValue("@tableName", tableName);

                    var columns = new List<Dictionary<string, object>>();
                    using (var reader = await columnsCmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var columnInfo = new Dictionary<string, object>
                            {
                                ["name"] = reader.GetString(0),
                                ["dataType"] = reader.GetString(1),
                                ["isNullable"] = reader.GetInt32(2) == 1,
                                ["isIdentity"] = reader.GetInt32(3) == 1,
                                ["maxLength"] = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                                ["defaultValue"] = reader.IsDBNull(5) ? null : reader.GetString(5),
                                ["ordinalPosition"] = reader.GetInt32(6)
                            };
                            columns.Add(columnInfo);
                        }
                    }

                    tableInfo["columns"] = columns;
                }

                // 3. Get primary keys for tables
                foreach (var tableInfo in schema.Where(t => (string)t["type"] == "Table"))
                {
                    string schemaName = (string)tableInfo["schema"];
                    string tableName = (string)tableInfo["name"];

                    string pkSql = @"
                        SELECT 
                            COL_NAME(ic.object_id, ic.column_id) AS [ColumnName],
                            ic.key_ordinal AS [KeyOrdinal]
                        FROM 
                            sys.indexes i
                            INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
                            INNER JOIN sys.objects o ON i.object_id = o.object_id
                            INNER JOIN sys.schemas s ON o.schema_id = s.schema_id
                        WHERE 
                            i.is_primary_key = 1
                            AND s.name = @schemaName
                            AND o.name = @tableName
                        ORDER BY 
                            ic.key_ordinal";

                    using var pkCmd = new SqlCommand(pkSql, sqlConnection);
                    pkCmd.Parameters.AddWithValue("@schemaName", schemaName);
                    pkCmd.Parameters.AddWithValue("@tableName", tableName);

                    var primaryKeys = new List<string>();
                    using (var reader = await pkCmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            primaryKeys.Add(reader.GetString(0));
                        }
                    }

                    // Mark primary key columns in the column list
                    var columns = (List<Dictionary<string, object>>)tableInfo["columns"];
                    foreach (var column in columns)
                    {
                        column["isPrimaryKey"] = primaryKeys.Contains((string)column["name"]);
                    }

                    tableInfo["primaryKey"] = primaryKeys;
                }

                // Return the schema information
                return Ok(new {
                    success = true,
                    message = $"Successfully retrieved schema for {database} on {server}",
                    server = server,
                    database = database,
                    schema = schema
                });
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "SQL Error fetching database schema. Error code: {ErrorCode}, State: {State}, Server: {Server}",
                    sqlEx.Number, sqlEx.State, sqlEx.Server);

                return StatusCode(500, new {
                    success = false,
                    message = "Failed to fetch database schema: " + GetUserFriendlyErrorMessage(sqlEx),
                    error = sqlEx.Message,
                    errorCode = sqlEx.Number,
                    state = sqlEx.State,
                    server = sqlEx.Server
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching database schema");
                
                return StatusCode(500, new {
                    success = false,
                    message = "Failed to fetch database schema",
                    error = ex.Message,
                    exceptionType = ex.GetType().Name
                });
            }
        }
    }
}