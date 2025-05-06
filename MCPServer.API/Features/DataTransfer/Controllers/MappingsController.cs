using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MCPServer.Core.Features.DataTransfer.Models;

namespace MCPServer.API.Features.DataTransfer.Controllers
{
    [ApiController]
    [Route("api/datatransfer/[controller]")]
    [Authorize]
    public class MappingsController : ControllerBase
    {
        private readonly ILogger<MappingsController> _logger;
        private readonly IConfiguration _configuration;

        public MappingsController(
            ILogger<MappingsController> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        [HttpGet]
        public ActionResult<IEnumerable<TableMapping>> GetMappings()
        {
            try
            {
                // In a production environment, this would likely be stored in a database
                // For now, we'll retrieve it from configuration
                var mappings = _configuration.GetSection("DataTransfer:TableMappings").Get<List<TableMapping>>();
                
                if (mappings == null)
                {
                    return NotFound("No table mappings found in configuration");
                }
                
                return Ok(mappings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving table mappings");
                return StatusCode(500, "Error retrieving table mappings");
            }
        }

        [HttpGet("{tableName}")]
        public ActionResult<TableMapping> GetMapping(string tableName)
        {
            try
            {
                var mappings = _configuration.GetSection("DataTransfer:TableMappings").Get<List<TableMapping>>();
                
                if (mappings == null)
                {
                    return NotFound("No table mappings found in configuration");
                }
                
                var mapping = mappings.Find(m => m.SourceTable.Equals(tableName, StringComparison.OrdinalIgnoreCase));
                
                if (mapping == null)
                {
                    return NotFound($"No mapping found for table {tableName}");
                }
                
                return Ok(mapping);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving table mapping for {TableName}", tableName);
                return StatusCode(500, $"Error retrieving table mapping for {tableName}");
            }
        }

        [HttpPost]
        public ActionResult<TableMapping> CreateMapping([FromBody] TableMapping mapping)
        {
            try
            {
                // In a production environment, you would store this in a database
                // For now, we'll pretend it was created successfully
                
                // Validate the mapping
                if (string.IsNullOrEmpty(mapping.SourceTable))
                {
                    return BadRequest("Source table name is required");
                }
                
                if (string.IsNullOrEmpty(mapping.TargetTable))
                {
                    return BadRequest("Target table name is required");
                }
                
                if (mapping.ColumnMappings == null || mapping.ColumnMappings.Count == 0)
                {
                    return BadRequest("At least one column mapping is required");
                }
                
                // For incremental loading, validate that incremental column is specified
                if (mapping.IncrementalType != "None" && string.IsNullOrEmpty(mapping.IncrementalColumn))
                {
                    return BadRequest("Incremental column must be specified when incremental type is not 'None'");
                }

                // Return a successful response
                return CreatedAtAction(nameof(GetMapping), new { tableName = mapping.SourceTable }, mapping);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating table mapping");
                return StatusCode(500, "Error creating table mapping");
            }
        }

        [HttpPut("{tableName}")]
        public ActionResult<TableMapping> UpdateMapping(string tableName, [FromBody] TableMapping mapping)
        {
            try
            {
                // In a production environment, you would update this in a database
                // For now, we'll just validate and return success
                
                // Validate that the mapping matches the URL
                if (!mapping.SourceTable.Equals(tableName, StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest("Source table name in URL must match the mapping");
                }
                
                // Same validations as create
                if (string.IsNullOrEmpty(mapping.TargetTable))
                {
                    return BadRequest("Target table name is required");
                }
                
                if (mapping.ColumnMappings == null || mapping.ColumnMappings.Count == 0)
                {
                    return BadRequest("At least one column mapping is required");
                }
                
                if (mapping.IncrementalType != "None" && string.IsNullOrEmpty(mapping.IncrementalColumn))
                {
                    return BadRequest("Incremental column must be specified when incremental type is not 'None'");
                }

                // Return a successful response
                return Ok(mapping);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating table mapping for {TableName}", tableName);
                return StatusCode(500, $"Error updating table mapping for {tableName}");
            }
        }

        [HttpDelete("{tableName}")]
        public ActionResult DeleteMapping(string tableName)
        {
            try
            {
                // In a production environment, you would delete this from a database
                // For now, we'll just return success
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting table mapping for {TableName}", tableName);
                return StatusCode(500, $"Error deleting table mapping for {tableName}");
            }
        }
    }
}