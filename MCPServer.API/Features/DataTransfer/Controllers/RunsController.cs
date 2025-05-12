using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MCPServer.API.Features.DataTransfer.Controllers
{
    [ApiController]
    [Route("api/datatransfer/[controller]")]
    [Authorize]
    public class RunsController : ControllerBase
    {
        private readonly ILogger<RunsController> _logger;

        public RunsController(ILogger<RunsController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IActionResult GetRuns([FromQuery] int? configurationId = null)
        {
            _logger.LogInformation("GetRuns endpoint called. ConfigurationId filter: {ConfigurationId}", configurationId);
            
            // Sample list of data transfer runs
            var runs = new List<object>
            {
                new
                {
                    RunId = 1,
                    ConfigurationId = 1,
                    ConfigurationName = "Daily Player Data Sync",
                    StartTime = DateTime.UtcNow.AddDays(-1),
                    EndTime = DateTime.UtcNow.AddDays(-1).AddHours(2),
                    Status = "Completed",
                    RecordsProcessed = 15000,
                    Success = true,
                    Message = "Transfer completed successfully"
                },
                new
                {
                    RunId = 2,
                    ConfigurationId = 2,
                    ConfigurationName = "Weekly Transaction Summary",
                    StartTime = DateTime.UtcNow.AddDays(-2),
                    EndTime = DateTime.UtcNow.AddDays(-2).AddHours(1),
                    Status = "Completed",
                    RecordsProcessed = 5000,
                    Success = true,
                    Message = "Transfer completed successfully"
                },
                new
                {
                    RunId = 3,
                    ConfigurationId = 1,
                    ConfigurationName = "Daily Player Data Sync",
                    StartTime = DateTime.UtcNow.AddDays(-2),
                    EndTime = DateTime.UtcNow.AddDays(-2).AddHours(2),
                    Status = "Completed",
                    RecordsProcessed = 14500,
                    Success = true,
                    Message = "Transfer completed successfully"
                },
                new
                {
                    RunId = 4,
                    ConfigurationId = 3,
                    ConfigurationName = "User Profile Transfer",
                    StartTime = DateTime.UtcNow.AddDays(-5),
                    EndTime = DateTime.UtcNow.AddDays(-5).AddMinutes(45),
                    Status = "Failed",
                    RecordsProcessed = 1500,
                    Success = false,
                    Message = "Error connecting to destination database"
                }
            };
            
            // Filter by configuration ID if provided
            if (configurationId.HasValue)
            {
                var filteredRuns = new List<object>();
                foreach (var run in runs)
                {
                    dynamic dynRun = run;
                    if (dynRun.ConfigurationId == configurationId.Value)
                    {
                        filteredRuns.Add(run);
                    }
                }
                runs = filteredRuns;
            }
            
            // Return data in the expected format with a "values" property
            return Ok(new { values = runs });
        }

        [HttpGet("{id}")]
        public IActionResult GetRun(int id)
        {
            _logger.LogInformation("GetRun endpoint called for ID: {Id}", id);
            
            // Return detailed information about a specific run
            if (id >= 1 && id <= 4) // Check if valid run ID
            {
                var run = new
                {
                    RunId = id,
                    ConfigurationId = id == 2 ? 2 : (id == 4 ? 3 : 1),
                    ConfigurationName = id == 2 ? "Weekly Transaction Summary" : (id == 4 ? "User Profile Transfer" : "Daily Player Data Sync"),
                    StartTime = DateTime.UtcNow.AddDays(-1),
                    EndTime = DateTime.UtcNow.AddDays(-1).AddHours(2),
                    Status = id == 4 ? "Failed" : "Completed",
                    RecordsProcessed = id == 2 ? 5000 : (id == 4 ? 1500 : 15000),
                    Success = id != 4,
                    Message = id == 4 ? "Error connecting to destination database" : "Transfer completed successfully",
                    TableResults = new List<object>
                    {
                        new
                        {
                            TableName = "Players",
                            SourceRowCount = 10000,
                            DestinationRowCount = 10000,
                            RowsTransferred = 10000,
                            Status = "Completed",
                            Success = true,
                            Message = "All rows transferred successfully",
                            StartTime = DateTime.UtcNow.AddDays(-1),
                            EndTime = DateTime.UtcNow.AddDays(-1).AddHours(1)
                        },
                        new
                        {
                            TableName = "PlayerActions",
                            SourceRowCount = 5000,
                            DestinationRowCount = 5000,
                            RowsTransferred = 5000,
                            Status = "Completed",
                            Success = true,
                            Message = "All rows transferred successfully",
                            StartTime = DateTime.UtcNow.AddDays(-1).AddHours(1),
                            EndTime = DateTime.UtcNow.AddDays(-1).AddHours(2)
                        }
                    }
                };
                
                return Ok(run);
            }
            
            return NotFound($"Run with ID {id} not found");
        }

        [HttpPost("cancel/{id}")]
        public IActionResult CancelRun(int id)
        {
            _logger.LogInformation("CancelRun endpoint called for ID: {Id}", id);
            return Ok(new { success = true, message = $"Run {id} has been cancelled" });
        }

        [HttpPost("restart/{id}")]
        public IActionResult RestartRun(int id)
        {
            _logger.LogInformation("RestartRun endpoint called for ID: {Id}", id);
            return Ok(new { success = true, message = $"Run {id} has been restarted", newRunId = new Random().Next(5, 100) });
        }
    }
}