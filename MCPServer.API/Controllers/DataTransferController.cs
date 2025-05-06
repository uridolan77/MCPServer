using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using MCPServer.Core.Services.DataTransfer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MCPServer.API.Controllers
{
    [ApiController]
    [Route("api/data-transfer")]
    public class DataTransferController : ControllerBase
    {
        private readonly DataTransferService _dataTransferService;
        private readonly ILogger<DataTransferController> _logger;
        private string _connectionString; // Not readonly so we can update it at runtime

        public DataTransferController(
            DataTransferService dataTransferService,
            IConfiguration configuration,
            ILogger<DataTransferController> logger)
        {
            _dataTransferService = dataTransferService;
            _logger = logger;

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
                return Ok(configurations);
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
        public async Task<IActionResult> GetConnections([FromQuery] bool? isSource, [FromQuery] bool? isDestination, [FromQuery] bool? isActive = true)
        {
            try
            {
                // Log the connection string (without sensitive info) for debugging
                _logger.LogInformation("Attempting to connect to database server: {Server}", GetServerFromConnectionString(_connectionString));

                // Use the actual database connection
                _logger.LogInformation("Getting database connections for {Server}", GetServerFromConnectionString(_connectionString));

                var connections = await _dataTransferService.GetConnectionsAsync(_connectionString, isSource, isDestination, isActive);
                return Ok(connections);
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
                string username = (User.Identity?.IsAuthenticated == true && !string.IsNullOrEmpty(User.Identity.Name)) ? User.Identity.Name : "System";
                int runId = await _dataTransferService.ExecuteDataTransferAsync(_connectionString, id, username);

                return Ok(new { runId });
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
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
                return Ok(history);
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
    }
}