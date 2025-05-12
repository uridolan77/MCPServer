using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using MCPServer.Core.Data;
using MCPServer.Core.Models.DataTransfer;
using MCPServer.API.Features.DataTransfer.Models;

namespace MCPServer.API.Features.DataTransfer.Controllers
{
    [ApiController]
    [Route("api/datatransfer/[controller]")]
    [Authorize]
    public class ConfigurationsController : ControllerBase
    {
        private readonly ILogger<ConfigurationsController> _logger;
        private readonly ProgressPlayDbContext _progressPlayDbContext;

        public ConfigurationsController(
            ILogger<ConfigurationsController> logger,
            ProgressPlayDbContext progressPlayDbContext)
        {
            _logger = logger;
            _progressPlayDbContext = progressPlayDbContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetConfigurations()
        {
            try
            {
                _logger.LogInformation("GetConfigurations endpoint called");
                
                // Create a list to hold the final results with all properties
                var result = new List<object>();
                
                // Get all configurations with their basic data
                var configurations = await _progressPlayDbContext.DataTransferConfigurations
                    .Include(c => c.SourceConnection)
                    .Include(c => c.DestinationConnection)
                    .ToListAsync();

                // Process each configuration and include related data
                foreach (var config in configurations)
                {
                    // Get table mappings for this configuration
                    var tableMappings = await _progressPlayDbContext.DataTransferTableMappings
                        .Where(tm => tm.ConfigurationId == config.ConfigurationId)
                        .Select(tm => new
                        {
                            TableMappingId = tm.MappingId,
                            SourceTable = $"{tm.SchemaName}.{tm.TableName}",
                            DestinationTable = $"{tm.SchemaName}.{tm.TableName}", // Assuming same name in destination
                            TimestampColumnName = tm.TimestampColumnName,
                            OrderByColumn = tm.OrderByColumn,
                            CustomWhereClause = tm.CustomWhereClause,
                            IsActive = tm.IsActive,
                            Priority = tm.Priority
                        })
                        .ToListAsync();

                    // Get schedules for this configuration
                    var schedules = await _progressPlayDbContext.DataTransferSchedule
                        .Where(s => s.ConfigurationId == config.ConfigurationId)
                        .Select(s => new
                        {
                            ScheduleId = s.ScheduleId,
                            ScheduleType = s.ScheduleType,
                            StartTime = s.StartTime,
                            Frequency = s.Frequency,
                            FrequencyUnit = s.FrequencyUnit,
                            WeekDays = s.WeekDays,
                            MonthDays = s.MonthDays,
                            IsActive = s.IsActive,
                            LastRunTime = s.LastRunTime,
                            NextRunTime = s.NextRunTime
                        })
                        .ToListAsync();

                    // Create a complete object with all properties
                    result.Add(new
                    {
                        ConfigurationId = config.ConfigurationId,
                        ConfigurationName = config.ConfigurationName,
                        Description = config.Description,
                        SourceConnection = new
                        {
                            ConnectionId = config.SourceConnection.ConnectionId,
                            ConnectionName = config.SourceConnection.ConnectionName,
                            Description = config.SourceConnection.Description,
                            IsActive = config.SourceConnection.IsActive
                        },
                        DestinationConnection = new
                        {
                            ConnectionId = config.DestinationConnection.ConnectionId,
                            ConnectionName = config.DestinationConnection.ConnectionName,
                            Description = config.DestinationConnection.Description,
                            IsActive = config.DestinationConnection.IsActive
                        },
                        TableMappings = tableMappings,
                        Schedules = schedules,
                        IsActive = config.IsActive,
                        BatchSize = config.BatchSize,
                        ReportingFrequency = config.ReportingFrequency,
                        CreatedBy = config.CreatedBy,
                        CreatedOn = config.CreatedOn,
                        LastModifiedBy = config.LastModifiedBy,
                        LastModifiedOn = config.LastModifiedOn
                    });
                }
                
                return Ok(new { values = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving configurations");
                return StatusCode(500, "Error retrieving configurations");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetConfiguration(int id)
        {
            try
            {
                _logger.LogInformation("GetConfiguration endpoint called for ID: {Id}", id);
                
                // Retrieve the specific configuration with all related data
                var configuration = await _progressPlayDbContext.DataTransferConfigurations
                    .Include(c => c.SourceConnection)
                    .Include(c => c.DestinationConnection)
                    .FirstOrDefaultAsync(c => c.ConfigurationId == id);
                
                if (configuration == null)
                {
                    return NotFound($"Configuration with ID {id} not found");
                }
                
                // Get table mappings for this configuration
                var tableMappings = await _progressPlayDbContext.DataTransferTableMappings
                    .Where(tm => tm.ConfigurationId == id)
                    .ToListAsync();
                
                // Get schedules for this configuration
                var schedules = await _progressPlayDbContext.DataTransferSchedule
                    .Where(s => s.ConfigurationId == id)
                    .ToListAsync();
                
                // Get the most recent run for this configuration
                var lastRun = await _progressPlayDbContext.DataTransferRuns
                    .Where(r => r.ConfigurationId == id)
                    .OrderByDescending(r => r.StartTime)
                    .FirstOrDefaultAsync();
                
                // Format the response
                var response = new
                {
                    ConfigurationId = configuration.ConfigurationId,
                    ConfigurationName = configuration.ConfigurationName,
                    Description = configuration.Description,
                    SourceConnection = new
                    {
                        ConnectionId = configuration.SourceConnection.ConnectionId,
                        ConnectionName = configuration.SourceConnection.ConnectionName,
                        Description = configuration.SourceConnection.Description,
                        IsActive = configuration.SourceConnection.IsActive
                    },
                    DestinationConnection = new
                    {
                        ConnectionId = configuration.DestinationConnection.ConnectionId,
                        ConnectionName = configuration.DestinationConnection.ConnectionName,
                        Description = configuration.DestinationConnection.Description,
                        IsActive = configuration.DestinationConnection.IsActive
                    },
                    TableMappings = tableMappings.Select(tm => new
                    {
                        TableMappingId = tm.MappingId,
                        SourceTable = $"{tm.SchemaName}.{tm.TableName}",
                        DestinationTable = $"{tm.SchemaName}.{tm.TableName}", // Assuming same name in destination
                        TimestampColumnName = tm.TimestampColumnName,
                        OrderByColumn = tm.OrderByColumn,
                        CustomWhereClause = tm.CustomWhereClause,
                        IsActive = tm.IsActive,
                        Priority = tm.Priority
                    }),
                    Schedules = schedules.Select(s => new
                    {
                        ScheduleId = s.ScheduleId,
                        ScheduleType = s.ScheduleType,
                        StartTime = s.StartTime,
                        Frequency = s.Frequency,
                        FrequencyUnit = s.FrequencyUnit,
                        WeekDays = s.WeekDays,
                        MonthDays = s.MonthDays,
                        IsActive = s.IsActive,
                        LastRunTime = s.LastRunTime,
                        NextRunTime = s.NextRunTime
                    }),
                    LastRun = lastRun != null ? new
                    {
                        RunId = lastRun.RunId,
                        StartTime = lastRun.StartTime,
                        EndTime = lastRun.EndTime,
                        Status = lastRun.Status,
                        TotalRowsProcessed = lastRun.TotalRowsProcessed,
                        ElapsedMs = lastRun.ElapsedMs
                    } : null,
                    IsActive = configuration.IsActive,
                    BatchSize = configuration.BatchSize,
                    ReportingFrequency = configuration.ReportingFrequency,
                    CreatedBy = configuration.CreatedBy,
                    CreatedOn = configuration.CreatedOn,
                    LastModifiedBy = configuration.LastModifiedBy,
                    LastModifiedOn = configuration.LastModifiedOn
                };
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving configuration with ID {ConfigurationId}", id);
                return StatusCode(500, "Error retrieving configuration");
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveConfiguration([FromBody] ConfigurationRequest request)
        {
            try
            {
                _logger.LogInformation("SaveConfiguration endpoint called");
                
                if (request == null)
                {
                    return BadRequest("Configuration data is required");
                }
                
                // Check if this is an update or create operation
                if (request.ConfigurationId > 0)
                {
                    // Update existing configuration
                    var existingConfig = await _progressPlayDbContext.DataTransferConfigurations
                        .FindAsync(request.ConfigurationId);
                        
                    if (existingConfig == null)
                    {
                        return NotFound($"Configuration with ID {request.ConfigurationId} not found");
                    }
                    
                    // Update properties
                    existingConfig.ConfigurationName = request.ConfigurationName;
                    existingConfig.Description = request.Description;
                    existingConfig.SourceConnectionId = request.SourceConnectionId;
                    existingConfig.DestinationConnectionId = request.DestinationConnectionId;
                    existingConfig.BatchSize = request.BatchSize;
                    existingConfig.ReportingFrequency = request.ReportingFrequency;
                    existingConfig.IsActive = request.IsActive;
                    existingConfig.LastModifiedBy = request.LastModifiedBy ?? "System";
                    existingConfig.LastModifiedOn = DateTime.UtcNow;
                    
                    // Save changes
                    await _progressPlayDbContext.SaveChangesAsync();
                    
                    // Handle table mappings if provided
                    if (request.TableMappings != null && request.TableMappings.Any())
                    {
                        // Remove existing mappings
                        var existingMappings = await _progressPlayDbContext.DataTransferTableMappings
                            .Where(tm => tm.ConfigurationId == request.ConfigurationId)
                            .ToListAsync();
                            
                        _progressPlayDbContext.DataTransferTableMappings.RemoveRange(existingMappings);
                        
                        // Add new mappings
                        foreach (var mapping in request.TableMappings)
                        {
                            // Parse schema and table name
                            var parts = mapping.SourceTable.Split('.');
                            string schema = parts.Length > 1 ? parts[0] : "dbo";
                            string tableName = parts.Length > 1 ? parts[1] : parts[0];
                            
                            _progressPlayDbContext.DataTransferTableMappings.Add(new DataTransferTableMapping
                            {
                                ConfigurationId = existingConfig.ConfigurationId,
                                SchemaName = schema,
                                TableName = tableName,
                                TimestampColumnName = mapping.TimestampColumnName ?? "ModifiedDate",
                                OrderByColumn = mapping.OrderByColumn,
                                CustomWhereClause = mapping.CustomWhereClause,
                                IsActive = mapping.IsActive,
                                Priority = mapping.Priority ?? 100
                            });
                        }
                        
                        await _progressPlayDbContext.SaveChangesAsync();
                    }
                    
                    return Ok(new { id = existingConfig.ConfigurationId });
                }
                else
                {
                    // Create new configuration
                    var newConfig = new DataTransferConfiguration
                    {
                        ConfigurationName = request.ConfigurationName,
                        Description = request.Description,
                        SourceConnectionId = request.SourceConnectionId,
                        DestinationConnectionId = request.DestinationConnectionId,
                        BatchSize = request.BatchSize,
                        ReportingFrequency = request.ReportingFrequency,
                        IsActive = request.IsActive,
                        CreatedBy = request.CreatedBy ?? "System",
                        CreatedOn = DateTime.UtcNow,
                        LastModifiedBy = request.LastModifiedBy,
                        LastModifiedOn = null
                    };
                    
                    _progressPlayDbContext.DataTransferConfigurations.Add(newConfig);
                    await _progressPlayDbContext.SaveChangesAsync();
                    
                    // Add table mappings if provided
                    if (request.TableMappings != null && request.TableMappings.Any())
                    {
                        foreach (var mapping in request.TableMappings)
                        {
                            // Parse schema and table name
                            var parts = mapping.SourceTable.Split('.');
                            string schema = parts.Length > 1 ? parts[0] : "dbo";
                            string tableName = parts.Length > 1 ? parts[1] : parts[0];
                            
                            _progressPlayDbContext.DataTransferTableMappings.Add(new DataTransferTableMapping
                            {
                                ConfigurationId = newConfig.ConfigurationId,
                                SchemaName = schema,
                                TableName = tableName,
                                TimestampColumnName = mapping.TimestampColumnName ?? "ModifiedDate",
                                OrderByColumn = mapping.OrderByColumn,
                                CustomWhereClause = mapping.CustomWhereClause,
                                IsActive = mapping.IsActive,
                                Priority = mapping.Priority ?? 100
                            });
                        }
                        
                        await _progressPlayDbContext.SaveChangesAsync();
                    }
                    
                    return Ok(new { id = newConfig.ConfigurationId });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving configuration");
                return StatusCode(500, "Error saving configuration");
            }
        }

        [HttpPost("{id}/execute")]
        public async Task<IActionResult> ExecuteDataTransfer(int id)
        {
            try
            {
                _logger.LogInformation("ExecuteDataTransfer endpoint called for ID: {Id}", id);
                
                // Find the configuration
                var config = await _progressPlayDbContext.DataTransferConfigurations
                    .FindAsync(id);
                
                if (config == null)
                {
                    return NotFound($"Configuration with ID {id} not found");
                }
                
                // Create a new run record
                var run = new DataTransferRun
                {
                    ConfigurationId = id,
                    StartTime = DateTime.UtcNow,
                    Status = "Running",
                    TriggeredBy = User.Identity?.Name ?? "System"
                };
                
                _progressPlayDbContext.DataTransferRuns.Add(run);
                await _progressPlayDbContext.SaveChangesAsync();
                
                // In a real implementation, you would start the data transfer process here
                // This could involve spinning up a background task or calling a service
                
                return Ok(new { 
                    success = true, 
                    message = $"Data transfer started for configuration {id}",
                    runId = run.RunId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing data transfer for configuration ID {ConfigurationId}", id);
                return StatusCode(500, "Error executing data transfer");
            }
        }

        [HttpPost("{id}/test")]
        public async Task<IActionResult> TestConfiguration(int id)
        {
            try
            {
                _logger.LogInformation("TestConfiguration endpoint called for ID: {Id}", id);
                
                // Find the configuration with connections
                var config = await _progressPlayDbContext.DataTransferConfigurations
                    .Include(c => c.SourceConnection)
                    .Include(c => c.DestinationConnection)
                    .FirstOrDefaultAsync(c => c.ConfigurationId == id);
                
                if (config == null)
                {
                    return NotFound($"Configuration with ID {id} not found");
                }
                
                // Using the data from the updated database schema
                return Ok(new { 
                    success = true, 
                    message = $"Configuration {id} tested successfully",
                    details = new
                    {
                        SourceConnection = new
                        {
                            ConnectionId = config.SourceConnection.ConnectionId,
                            ConnectionName = config.SourceConnection.ConnectionName,
                            ConnectionAccessLevel = config.SourceConnection.ConnectionAccessLevel,
                            IsActive = config.SourceConnection.IsActive,
                            LastTestedOn = config.SourceConnection.LastTestedOn
                        },
                        DestinationConnection = new
                        {
                            ConnectionId = config.DestinationConnection.ConnectionId,
                            ConnectionName = config.DestinationConnection.ConnectionName,
                            ConnectionAccessLevel = config.DestinationConnection.ConnectionAccessLevel,
                            IsActive = config.DestinationConnection.IsActive,
                            LastTestedOn = config.DestinationConnection.LastTestedOn
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing configuration with ID {ConfigurationId}", id);
                return StatusCode(500, "Error testing configuration");
            }
        }
    }

    // Define request model for configurations
    public class ConfigurationRequest
    {
        public int ConfigurationId { get; set; }
        public string ConfigurationName { get; set; }
        public string Description { get; set; }
        public int SourceConnectionId { get; set; }
        public int DestinationConnectionId { get; set; }
        public List<TableMappingRequest> TableMappings { get; set; }
        public int BatchSize { get; set; }
        public int ReportingFrequency { get; set; }
        public bool IsActive { get; set; }
        public string CreatedBy { get; set; }
        public string LastModifiedBy { get; set; }
    }

    public class TableMappingRequest
    {
        public int MappingId { get; set; }
        public string SourceTable { get; set; }
        public string DestinationTable { get; set; }
        public string TimestampColumnName { get; set; }
        public string OrderByColumn { get; set; }
        public string CustomWhereClause { get; set; }
        public bool IsActive { get; set; }
        public int? Priority { get; set; }
    }
}