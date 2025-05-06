using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MCPServer.API.Features.DataTransfer.Models;

namespace MCPServer.API.Features.DataTransfer.Controllers
{
    [ApiController]
    [Route("api/datatransfer/[controller]")]
    [Authorize]
    public class ConnectionsController : ControllerBase
    {
        private readonly ILogger<ConnectionsController> _logger;
        private readonly IConfiguration _configuration;

        public ConnectionsController(
            ILogger<ConnectionsController> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        [HttpGet]
        public ActionResult<IEnumerable<ConnectionResponse>> GetConnections()
        {
            try
            {
                // In a real implementation, you would retrieve this from a database
                // For now, we'll return a sample list of connections
                var connections = new List<ConnectionResponse>
                {
                    new ConnectionResponse
                    {
                        Id = "1",
                        Name = "Source Database",
                        Description = "Primary source database for data migrations",
                        IsSource = true,
                        ConnectionStringMasked = "Server=source-server;Database=****;User=****;Password=****;"
                    },
                    new ConnectionResponse
                    {
                        Id = "2",
                        Name = "Target Database",
                        Description = "Primary target database for data migrations",
                        IsSource = false,
                        ConnectionStringMasked = "Server=target-server;Database=****;User=****;Password=****;"
                    }
                };

                return Ok(connections);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving connections");
                return StatusCode(500, "Error retrieving connections");
            }
        }

        [HttpPost]
        public ActionResult<ConnectionResponse> CreateConnection([FromBody] ConnectionRequest request)
        {
            try
            {
                // In a real implementation, you would save this to a database
                // For now, we'll just return a mock response
                var response = new ConnectionResponse
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = request.Name,
                    Description = request.Description,
                    IsSource = request.IsSource,
                    ConnectionStringMasked = MaskConnectionString(request.ConnectionString)
                };

                return CreatedAtAction(nameof(GetConnections), response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating connection");
                return StatusCode(500, "Error creating connection");
            }
        }

        [HttpPost("test")]
        public async Task<ActionResult<ConnectionTestResponse>> TestConnection([FromBody] ConnectionTestRequest request)
        {
            var response = new ConnectionTestResponse();

            try
            {
                using (var connection = new SqlConnection(request.ConnectionString))
                {
                    await connection.OpenAsync();
                    
                    // Get server information
                    using (var command = new SqlCommand("SELECT @@VERSION", connection))
                    {
                        var version = await command.ExecuteScalarAsync() as string;
                        response.Version = version;
                    }
                    
                    response.Success = true;
                    response.Message = "Connection successful";
                    response.ServerInfo = connection.DataSource;
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing connection");
                
                response.Success = false;
                response.Message = $"Connection failed: {ex.Message}";
                
                return StatusCode(500, response);
            }
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
    }
}