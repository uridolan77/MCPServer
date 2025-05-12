using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using MCPServer.API.Features.DataTransfer.Models;
using MCPServer.Core.Data;
using MCPServer.Core.Models.DataTransfer;
using MCPServer.Core.Services.Interfaces;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using MCPServer.API.Services;

namespace MCPServer.API.Features.DataTransfer.Controllers
{
    [ApiController]
    [Route("api/datatransfer/[controller]")]
    [Authorize]
    public class ConnectionsController : ControllerBase
    {
        private readonly ILogger<ConnectionsController> _logger;
        private readonly IConfiguration _configuration;
        private readonly ProgressPlayDbContext _progressPlayDbContext;
        private readonly ICredentialService _credentialService;
        private readonly IConnectionStringResolverService _connectionStringResolver;

        public ConnectionsController(
            ILogger<ConnectionsController> logger,
            IConfiguration configuration,
            ProgressPlayDbContext progressPlayDbContext,
            ICredentialService credentialService,
            IConnectionStringResolverService connectionStringResolver)
        {
            _logger = logger;
            _configuration = configuration;
            _progressPlayDbContext = progressPlayDbContext;
            _credentialService = credentialService;
            _connectionStringResolver = connectionStringResolver;
        }

        [HttpGet]
        public ActionResult<IEnumerable<ConnectionResponse>> GetConnections()
        {
            try
            {
                // Only retrieve connections from the ProgressPlayDB
                var connections = _progressPlayDbContext.DataTransferConnections
                    .AsNoTracking() // Improves performance for read-only operations
                    .Select(c => new ConnectionResponse
                    {
                        ConnectionId = c.ConnectionId,
                        ConnectionName = c.ConnectionName ?? string.Empty,
                        Description = c.Description ?? string.Empty,
                        ConnectionAccessLevel = c.ConnectionAccessLevel ?? "ReadWrite",
                        ConnectionStringMasked = "Connection details masked for security",
                        Server = c.Server ?? string.Empty,
                        Port = c.Port,
                        Database = c.Database ?? string.Empty,
                        Username = c.Username ?? string.Empty,
                        // Password is masked for security in list view
                        AdditionalParameters = c.AdditionalParameters ?? string.Empty,
                        IsActive = c.IsActive,
                        IsConnectionValid = c.IsConnectionValid, // <--- ADDED
                        MinPoolSize = c.MinPoolSize,
                        MaxPoolSize = c.MaxPoolSize,
                        Timeout = c.Timeout,
                        TrustServerCertificate = c.TrustServerCertificate,
                        Encrypt = c.Encrypt,
                        CreatedBy = c.CreatedBy ?? string.Empty,
                        CreatedOn = c.CreatedOn,
                        LastModifiedBy = c.LastModifiedBy ?? string.Empty,
                        LastModifiedOn = c.LastModifiedOn,
                        LastTestedOn = c.LastTestedOn,
                        Source = "ProgressPlayDB"
                    })
                    .ToList();

                return Ok(connections);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving connections");
                return StatusCode(500, "Error retrieving connections");
            }
        }

        [HttpGet("{id}")]
        public ActionResult<ConnectionResponse> GetConnection(int id, [FromQuery] bool edit = false)
        {
            try
            {
                // First try to find the connection in the ProgressPlayDB
                var connection = _progressPlayDbContext.DataTransferConnections
                    .AsNoTracking() // Improves performance for read-only operations
                    .FirstOrDefault(c => c.ConnectionId == id);
                
                if (connection == null)
                {
                    return NotFound($"Connection with ID {id} not found in ProgressPlayDB");
                }

                var response = new ConnectionResponse
                {
                    ConnectionId = connection.ConnectionId,
                    ConnectionName = connection.ConnectionName ?? string.Empty,
                    Description = connection.Description ?? string.Empty,
                    ConnectionAccessLevel = connection.ConnectionAccessLevel ?? "ReadWrite",
                    ConnectionStringMasked = MaskConnectionString(connection.ConnectionString ?? string.Empty),
                    LastTestedOn = connection.LastTestedOn,
                    IsConnectionValid = connection.IsConnectionValid, // <--- ADDED
                    Source = "ProgressPlayDB"
                };
                
                // If this is an edit request, include the full connection data
                if (edit)
                {
                    response.ConnectionString = connection.ConnectionString ?? string.Empty;
                    response.Server = connection.Server ?? string.Empty;
                    response.Port = connection.Port;
                    response.Database = connection.Database ?? string.Empty;
                    response.Username = connection.Username ?? string.Empty;
                    response.Password = connection.Password ?? string.Empty;
                    response.AdditionalParameters = connection.AdditionalParameters ?? string.Empty;
                    response.IsActive = connection.IsActive;
                    // IsConnectionValid is already set above
                    response.MinPoolSize = connection.MinPoolSize;
                    response.MaxPoolSize = connection.MaxPoolSize;
                    response.Timeout = connection.Timeout;
                    response.TrustServerCertificate = connection.TrustServerCertificate;
                    response.Encrypt = connection.Encrypt;
                    response.CreatedBy = connection.CreatedBy ?? string.Empty;
                    response.CreatedOn = connection.CreatedOn;
                    response.LastModifiedBy = connection.LastModifiedBy ?? string.Empty;
                    response.LastModifiedOn = connection.LastModifiedOn;
                    
                    _logger.LogInformation("Returning connection with ID {ConnectionId} for editing", id);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving connection with ID {ConnectionId}", id);
                return StatusCode(500, "Error retrieving connection");
            }
        }

        [HttpPost]
        public ActionResult<ConnectionResponse> CreateConnection([FromBody] ConnectionRequest request)
        {
            try
            {
                // Validate connection string before saving
                if (!IsValidConnectionString(request.ConnectionString))
                {
                    return BadRequest("Invalid connection string format");
                }
                
                // Determine the connection access level based on source/destination flags
                string connectionAccessLevel = DetermineConnectionAccessLevel(request.IsSource, request.IsDestination);
                
                // Create a new DataTransferConnection entity with updated schema
                var connection = new DataTransferConnection
                {
                    ConnectionName = request.Name,
                    ConnectionString = request.ConnectionString,
                    Description = request.Description,
                    ConnectionAccessLevel = connectionAccessLevel,
                    IsActive = true,
                    IsConnectionValid = null, // <--- ADDED: New connections are untested
                    CreatedBy = User.Identity?.Name ?? "System",
                    CreatedOn = DateTime.UtcNow
                };
                
                // Save to database
                _progressPlayDbContext.DataTransferConnections.Add(connection);
                _progressPlayDbContext.SaveChanges();
                
                // Return the newly created connection
                var response = new ConnectionResponse
                {
                    ConnectionId = connection.ConnectionId,
                    ConnectionName = connection.ConnectionName,
                    Description = connection.Description,
                    ConnectionAccessLevel = connection.ConnectionAccessLevel,
                    ConnectionStringMasked = MaskConnectionString(request.ConnectionString),
                    LastTestedOn = connection.LastTestedOn,
                    IsConnectionValid = connection.IsConnectionValid, // <--- ADDED
                    Source = "ProgressPlayDB"
                };

                return CreatedAtAction(nameof(GetConnections), response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating connection");
                return StatusCode(500, "Error creating connection");
            }
        }

        [HttpPut("{id}")]
        public ActionResult<ConnectionResponse> UpdateConnection(int id, [FromBody] ConnectionUpdateRequest request)
        {
            try
            {
                var connection = _progressPlayDbContext.DataTransferConnections.FirstOrDefault(c => c.ConnectionId == id);
                
                if (connection == null)
                {
                    return NotFound($"Connection with ID {id} not found");
                }

                // Update basic properties
                connection.ConnectionName = request.Name ?? connection.ConnectionName;
                connection.Description = request.Description ?? connection.Description;
                
                // Check if any connection-defining properties have changed
                bool connectionPropertiesChanged = 
                    (request.ConnectionString != null && request.ConnectionString != connection.ConnectionString) ||
                    (request.Server != null && request.Server != connection.Server) ||
                    (request.Port.HasValue && request.Port != connection.Port) ||
                    (request.Database != null && request.Database != connection.Database) ||
                    (request.Username != null && request.Username != connection.Username) ||
                    (request.Password != null && request.Password != connection.Password) || // Note: Password comparison might need secure handling if not already
                    (request.AdditionalParameters != null && request.AdditionalParameters != connection.AdditionalParameters) ||
                    (request.MinPoolSize.HasValue && request.MinPoolSize != connection.MinPoolSize) ||
                    (request.MaxPoolSize.HasValue && request.MaxPoolSize != connection.MaxPoolSize) ||
                    (request.Timeout.HasValue && request.Timeout != connection.Timeout) ||
                    (request.TrustServerCertificate.HasValue && request.TrustServerCertificate != connection.TrustServerCertificate) ||
                    (request.Encrypt.HasValue && request.Encrypt != connection.Encrypt);

                if (connectionPropertiesChanged)
                {
                    connection.IsConnectionValid = null; // Reset validation status
                    // Update properties if they are provided in the request
                    if(request.ConnectionString != null) connection.ConnectionString = request.ConnectionString;
                    if(request.Server != null) connection.Server = request.Server;
                    if(request.Port.HasValue) connection.Port = request.Port.Value;
                    if(request.Database != null) connection.Database = request.Database;
                    if(request.Username != null) connection.Username = request.Username;
                    if(request.Password != null) connection.Password = request.Password; // Consider security implications
                    if(request.AdditionalParameters != null) connection.AdditionalParameters = request.AdditionalParameters;
                    if(request.MinPoolSize.HasValue) connection.MinPoolSize = request.MinPoolSize.Value;
                    if(request.MaxPoolSize.HasValue) connection.MaxPoolSize = request.MaxPoolSize.Value;
                    if(request.Timeout.HasValue) connection.Timeout = request.Timeout.Value;
                    if(request.TrustServerCertificate.HasValue) connection.TrustServerCertificate = request.TrustServerCertificate.Value;
                    if(request.Encrypt.HasValue) connection.Encrypt = request.Encrypt.Value;
                }

                // Update the ConnectionAccessLevel based on IsSource and IsDestination flags if provided
                bool isSourceChanged = request.IsSource.HasValue;
                bool isDestinationChanged = request.IsDestination.HasValue;
                
                if (isSourceChanged || isDestinationChanged)
                {
                    // Get current values
                    bool isSource = request.IsSource ?? (connection.ConnectionAccessLevel == "ReadOnly" || connection.ConnectionAccessLevel == "ReadWrite");
                    bool isDestination = request.IsDestination ?? (connection.ConnectionAccessLevel == "WriteOnly" || connection.ConnectionAccessLevel == "ReadWrite");
                    
                    // Set the new ConnectionAccessLevel
                    connection.ConnectionAccessLevel = DetermineConnectionAccessLevel(isSource, isDestination);
                }
                
                if (request.IsActive.HasValue)
                {
                    connection.IsActive = request.IsActive.Value;
                }
                
                // Update audit fields
                connection.LastModifiedBy = User.Identity?.Name ?? "System";
                connection.LastModifiedOn = DateTime.UtcNow;

                _progressPlayDbContext.SaveChanges();

                var response = new ConnectionResponse
                {
                    ConnectionId = connection.ConnectionId,
                    ConnectionName = connection.ConnectionName,
                    Description = connection.Description,
                    ConnectionAccessLevel = connection.ConnectionAccessLevel,
                    ConnectionStringMasked = "Connection details masked for security",
                    LastTestedOn = connection.LastTestedOn,
                    IsConnectionValid = connection.IsConnectionValid, // <--- ADDED
                    Source = "ProgressPlayDB"
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating connection with ID {ConnectionId}", id);
                return StatusCode(500, "Error updating connection");
            }
        }

        [HttpDelete("{id}")]
        public ActionResult DeleteConnection(int id)
        {
            try
            {
                var connection = _progressPlayDbContext.DataTransferConnections.FirstOrDefault(c => c.ConnectionId == id);
                
                if (connection == null)
                {
                    return NotFound($"Connection with ID {id} not found");
                }

                _progressPlayDbContext.DataTransferConnections.Remove(connection);
                _progressPlayDbContext.SaveChanges();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting connection with ID {ConnectionId}", id);
                return StatusCode(500, "Error deleting connection");
            }
        }

        [HttpPost("test")]
        public async Task<ActionResult<ConnectionTestResponse>> TestConnection([FromBody] ConnectionTestRequest request)
        {
            var response = new ConnectionTestResponse();
            DataTransferConnection? dbConnection = null;

            try
            {
                // If request contains an ID, try to load the connection from DB
                // This part is conceptual, assuming ConnectionTestRequest might be extended or a different model used for testing existing connections by ID.
                // For now, we assume request.Id is not standard on ConnectionTestRequest.
                // If an ID were present (e.g., request.ConnectionId), you'd load `dbConnection` here.

                // Validate the connection string
                if (!IsValidConnectionString(request.ConnectionString))
                {
                    response.IsSuccess = false;
                    response.Message = "Invalid connection string format";
                    return BadRequest(response);
                }

                // Try to open a connection
                using (var connection = new SqlConnection(request.ConnectionString))
                {
                    try
                    {
                        await connection.OpenAsync();
                        response.IsSuccess = true;
                        response.Message = "Connection successful";
                        response.IsConnectionValid = true; // <--- ADDED
                        if (dbConnection != null)
                        {
                            dbConnection.LastTestedOn = DateTime.UtcNow;
                            dbConnection.IsConnectionValid = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        response.IsSuccess = false;
                        response.Message = $"Connection failed: {ex.Message}";
                        response.IsConnectionValid = false; // <--- ADDED
                        if (dbConnection != null)
                        {
                            dbConnection.LastTestedOn = DateTime.UtcNow;
                            dbConnection.IsConnectionValid = false;
                        }
                    }
                }
                
                if (dbConnection != null)
                {
                    await _progressPlayDbContext.SaveChangesAsync(); // Save changes if dbConnection was loaded and updated
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing connection");
                response.IsSuccess = false;
                response.Message = $"Error testing connection: {ex.Message}";
                return StatusCode(500, response);
            }
        }

        [HttpPost("test/{id}")]
        public async Task<ActionResult<ConnectionTestResponse>> TestConnectionById(int id)
        {
            try
            {
                // First try to find the connection in the ProgressPlayDB
                var connection = await _progressPlayDbContext.DataTransferConnections.FirstOrDefaultAsync(c => c.ConnectionId == id);
                
                if (connection == null)
                {
                    return NotFound($"Connection with ID {id} not found in ProgressPlayDB");
                }

                // Get the connection string from configuration based on the connection ID
                // For this example, we'll use a simple mapping to retrieve connection strings
                string connectionStringTemplate = GetConnectionStringForId(id);
                
                if (string.IsNullOrEmpty(connectionStringTemplate))
                {
                    return BadRequest("Connection string not found for this connection");
                }
                
                // Resolve any Azure Key Vault placeholders in the connection string
                string connectionString = await _connectionStringResolver.ResolveConnectionStringAsync(connectionStringTemplate);

                // Create a response object
                var response = new ConnectionTestResponse();
                
                // Test the connection
                using (var sqlConnection = new SqlConnection(connectionString))
                {
                    try
                    {
                        await sqlConnection.OpenAsync();
                        response.IsSuccess = true;
                        response.Message = $"Connection successful";
                        response.ServerInfo = sqlConnection.ServerVersion;
                        response.IsConnectionValid = true; // <--- ADDED
                        
                        // Update the connection in the database
                        connection.LastTestedOn = DateTime.UtcNow;
                        connection.IsConnectionValid = true;
                    }
                    catch (Exception ex)
                    {
                        response.IsSuccess = false;
                        response.Message = $"Connection failed: {ex.Message}";
                        response.IsConnectionValid = false; // <--- ADDED
                        
                        // Update the connection in the database
                        connection.LastTestedOn = DateTime.UtcNow;
                        connection.IsConnectionValid = false;
                    }
                }
                
                await _progressPlayDbContext.SaveChangesAsync(); // Save changes to LastTestedOn and IsConnectionValid

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing connection with ID {ConnectionId}", id);
                return StatusCode(500, "Error testing connection");
            }
        }

        [HttpGet("{id}/edit")]
        public ActionResult<ConnectionResponse> GetConnectionForEdit(int id)
        {
            try
            {
                // Find the connection in the ProgressPlayDB
                var connection = _progressPlayDbContext.DataTransferConnections
                    .FirstOrDefault(c => c.ConnectionId == id);
                
                if (connection == null)
                {
                    return NotFound($"Connection with ID {id} not found in ProgressPlayDB");
                }

                var response = new ConnectionResponse
                {
                    ConnectionId = connection.ConnectionId,
                    ConnectionName = connection.ConnectionName ?? string.Empty,
                    Description = connection.Description ?? string.Empty,
                    ConnectionAccessLevel = connection.ConnectionAccessLevel ?? "ReadWrite",
                    ConnectionString = connection.ConnectionString ?? string.Empty,
                    ConnectionStringMasked = MaskConnectionString(connection.ConnectionString ?? string.Empty),
                    Server = connection.Server ?? string.Empty,
                    Port = connection.Port,
                    Database = connection.Database ?? string.Empty,
                    Username = connection.Username ?? string.Empty,
                    Password = connection.Password ?? string.Empty,
                    AdditionalParameters = connection.AdditionalParameters ?? string.Empty,
                    IsActive = connection.IsActive,
                    IsConnectionValid = connection.IsConnectionValid, // <--- ADDED
                    MinPoolSize = connection.MinPoolSize,
                    MaxPoolSize = connection.MaxPoolSize,
                    Timeout = connection.Timeout,
                    TrustServerCertificate = connection.TrustServerCertificate,
                    Encrypt = connection.Encrypt,
                    CreatedBy = connection.CreatedBy ?? string.Empty,
                    CreatedOn = connection.CreatedOn,
                    LastModifiedBy = connection.LastModifiedBy ?? string.Empty,
                    LastModifiedOn = connection.LastModifiedOn,
                    LastTestedOn = connection.LastTestedOn,
                    Source = "ProgressPlayDB"
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving connection with ID {ConnectionId} for editing", id);
                return StatusCode(500, "Error retrieving connection for editing");
            }
        }

        // Helper method to get connection string for a specific ID from configuration
        private string GetConnectionStringForId(int id)
        {
            // This is a simplified example - in a real application, you would retrieve
            // the appropriate connection string from a secure store based on the ID
            // For now, we'll use the ProgressPlayDB connection string as an example
            return _configuration.GetConnectionString("ProgressPlayDB") ?? "";
        }

        private string MaskConnectionString(string connectionString)
        {
            // Simple method to mask sensitive parts of a connection string
            try
            {
                var builder = new SqlConnectionStringBuilder(connectionString);
                
                if (!string.IsNullOrEmpty(builder.Password))
                    builder.Password = "****";
                
                if (!string.IsNullOrEmpty(builder.UserID))
                    builder.UserID = "****";
                
                return builder.ConnectionString;
            }
            catch
            {
                // If there's any error parsing, just do a simple mask
                return connectionString.Replace("Password=", "Password=****")
                    .Replace("User Id=", "User Id=****")
                    .Replace("Uid=", "Uid=****")
                    .Replace("Pwd=", "Pwd=****");
            }
        }
        
        private string ComputeSha256Hash(string rawData)
        {
            // Create a SHA256 hash from string
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                // Convert byte array to a string
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        private bool IsValidConnectionString(string connectionString)
        {
            try
            {
                // Attempt to parse the connection string
                var builder = new SqlConnectionStringBuilder(connectionString);
                
                // Check that required properties are present
                if (string.IsNullOrEmpty(builder.DataSource))
                {
                    return false;
                }
                
                if (string.IsNullOrEmpty(builder.InitialCatalog))
                {
                    return false;
                }
                
                // Check that either integrated security is true or user ID and password are provided
                if (!builder.IntegratedSecurity && 
                    (string.IsNullOrEmpty(builder.UserID) || string.IsNullOrEmpty(builder.Password)))
                {
                    return false;
                }
                
                return true;
            }
            catch
            {
                // If there's an exception parsing the connection string, it's invalid
                return false;
            }
        }

        // Helper method to determine ConnectionAccessLevel based on IsSource and IsDestination flags
        private string DetermineConnectionAccessLevel(bool isSource, bool isDestination)
        {
            if (isSource && isDestination)
                return "ReadWrite";
            else if (isSource)
                return "ReadOnly";
            else if (isDestination)
                return "WriteOnly";
            else
                return "ReadWrite"; // Default to read-write if neither is specified
        }
    }
}