using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using MCPServer.API.Features.DataTransfer.Models;
using MCPServer.Core.Features.DataTransfer.Services;
using MCPServer.Core.Features.DataTransfer.Models;
using MCPServer.Core.Data;
using MCPServer.Core.Models.DataTransfer;

namespace MCPServer.API.Features.DataTransfer.Controllers
{
    [ApiController]
    [Route("api/datatransfer/[controller]")]
    [Authorize]
    public class DataTransferController : ControllerBase
    {
        private readonly ILogger<DataTransferController> _logger;
        private readonly DataMigrationService _migrationService;
        private readonly DataValidationService _validationService;
        private readonly ProgressPlayDbContext _progressPlayDbContext;

        public DataTransferController(
            ILogger<DataTransferController> logger,
            DataMigrationService migrationService,
            DataValidationService validationService,
            ProgressPlayDbContext progressPlayDbContext)
        {
            _logger = logger;
            _migrationService = migrationService;
            _validationService = validationService;
            _progressPlayDbContext = progressPlayDbContext;
        }

        [HttpGet("tables")]
        public ActionResult<IEnumerable<string>> GetAvailableTables()
        {
            try
            {
                var tables = _migrationService.GetProcessedTables();
                return Ok(tables);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving available tables");
                return StatusCode(500, "Error retrieving available tables");
            }
        }

        [HttpPost("migrate")]
        public async Task<ActionResult<MigrationResponse>> MigrateData([FromBody] MigrationRequest request)
        {
            var response = new MigrationResponse();
            
            try
            {
                _logger.LogInformation("Starting data migration for {TableCount} tables", 
                    request.Tables?.Count ?? 0);
                
                // Create a run record to track this migration
                var run = new DataTransferRun
                {
                    ConfigurationId = request.ConfigurationId,
                    StartTime = DateTime.UtcNow,
                    Status = "Running",
                    TriggeredBy = User.Identity?.Name ?? "System",
                    TotalTablesProcessed = request.Tables?.Count ?? 0
                };
                
                _progressPlayDbContext.DataTransferRuns.Add(run);
                await _progressPlayDbContext.SaveChangesAsync();
                
                // Log the start of migration
                var startLog = new DataTransferLog
                {
                    RunId = run.RunId,
                    LogTime = DateTime.UtcNow,
                    LogLevel = "Information",
                    Message = $"Starting data migration for configuration ID {request.ConfigurationId}"
                };
                
                _progressPlayDbContext.DataTransferLogs.Add(startLog);
                await _progressPlayDbContext.SaveChangesAsync();
                
                // Configure the migration service
                if (request.Tables?.Count > 0)
                {
                    _migrationService.SetTableFilter(request.Tables);
                }
                
                if (request.DryRun)
                {
                    _migrationService.EnableDryRun();
                    _logger.LogInformation("Dry run mode enabled - no data will be written");
                    
                    // Log dry run mode
                    var dryRunLog = new DataTransferLog
                    {
                        RunId = run.RunId,
                        LogTime = DateTime.UtcNow,
                        LogLevel = "Information",
                        Message = "Dry run mode enabled - no data will be written"
                    };
                    
                    _progressPlayDbContext.DataTransferLogs.Add(dryRunLog);
                    await _progressPlayDbContext.SaveChangesAsync();
                }

                var startTime = DateTime.UtcNow;
                
                try
                {
                    // Execute the migration
                    await _migrationService.RunMigrationAsync();
                    
                    // Update run record with success status
                    run.Status = "Completed";
                    run.SuccessfulTablesCount = request.Tables?.Count ?? 0;
                    run.FailedTablesCount = 0;
                    run.EndTime = DateTime.UtcNow;
                    run.ElapsedMs = (long)(run.EndTime.Value - run.StartTime).TotalMilliseconds;
                    
                    // Log success
                    var successLog = new DataTransferLog
                    {
                        RunId = run.RunId,
                        LogTime = DateTime.UtcNow,
                        LogLevel = "Information",
                        Message = "Data migration completed successfully"
                    };
                    
                    _progressPlayDbContext.DataTransferLogs.Add(successLog);
                }
                catch (Exception ex)
                {
                    // Update run record with failure status
                    run.Status = "Failed";
                    run.FailedTablesCount = request.Tables?.Count ?? 0;
                    run.SuccessfulTablesCount = 0;
                    run.EndTime = DateTime.UtcNow;
                    run.ElapsedMs = (long)(run.EndTime.Value - run.StartTime).TotalMilliseconds;
                    
                    // Log the error
                    var errorLog = new DataTransferLog
                    {
                        RunId = run.RunId,
                        LogTime = DateTime.UtcNow,
                        LogLevel = "Error",
                        Message = $"Error during data migration: {ex.Message}",
                        Exception = ex.ToString()
                    };
                    
                    _progressPlayDbContext.DataTransferLogs.Add(errorLog);
                    
                    // Rethrow to be caught by the outer try/catch
                    throw;
                }
                finally
                {
                    // Save the updated run record
                    await _progressPlayDbContext.SaveChangesAsync();
                }
                
                // For now, we don't have direct access to results, so we're creating a dummy success response
                // In a more complete implementation, you would capture results from the migration process
                response.Success = true;
                response.Message = request.DryRun 
                    ? "Dry run completed successfully" 
                    : "Data migration completed successfully";
                response.RunId = run.RunId;
                
                // Add dummy results for each table
                foreach (var table in request.Tables ?? _migrationService.GetProcessedTables())
                {
                    response.Results.Add(new TableMigrationResult
                    {
                        TableName = table,
                        Success = true,
                        RowsProcessed = 0, // You would get actual counts in a complete implementation
                        ElapsedTime = "00:00:00" // You would get actual timing in a complete implementation
                    });
                }
                
                // Run validation if requested
                if (request.Validate && !request.DryRun)
                {
                    _logger.LogInformation("Running data validation");
                    
                    // Log validation start
                    var validationLog = new DataTransferLog
                    {
                        RunId = run.RunId,
                        LogTime = DateTime.UtcNow,
                        LogLevel = "Information",
                        Message = "Starting data validation"
                    };
                    
                    _progressPlayDbContext.DataTransferLogs.Add(validationLog);
                    await _progressPlayDbContext.SaveChangesAsync();
                    
                    var validationResults = await _validationService.ValidateAsync(
                        request.Tables ?? _migrationService.GetProcessedTables());
                    
                    foreach (var result in validationResults)
                    {
                        response.ValidationMessages.Add(new ValidationMessage
                        {
                            TableName = result.TableName,
                            ValidationType = result.ValidationType,
                            Success = result.Success,
                            Details = result.Details,
                            ErrorMessage = result.ErrorMessage
                        });
                        
                        // Log each validation result
                        var resultLog = new DataTransferLog
                        {
                            RunId = run.RunId,
                            LogTime = DateTime.UtcNow,
                            LogLevel = result.Success ? "Information" : "Warning",
                            Message = $"Validation {result.ValidationType} for table {result.TableName}: " +
                                     (result.Success ? "Passed" : "Failed") +
                                     (string.IsNullOrEmpty(result.Details) ? "" : $" - {result.Details}")
                        };
                        
                        _progressPlayDbContext.DataTransferLogs.Add(resultLog);
                    }
                    
                    await _progressPlayDbContext.SaveChangesAsync();
                    
                    // Overall success is only true if all validations passed
                    if (response.ValidationMessages.Any(v => !v.Success))
                    {
                        response.Message += ", but some validations failed";
                        
                        // Log validation failures
                        var failureLog = new DataTransferLog
                        {
                            RunId = run.RunId,
                            LogTime = DateTime.UtcNow,
                            LogLevel = "Warning",
                            Message = "Some validations failed"
                        };
                        
                        _progressPlayDbContext.DataTransferLogs.Add(failureLog);
                        await _progressPlayDbContext.SaveChangesAsync();
                    }
                }
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during data migration");
                
                response.Success = false;
                response.Message = $"Error during data migration: {ex.Message}";
                
                return StatusCode(500, response);
            }
        }

        [HttpPost("validate")]
        public async Task<ActionResult<MigrationResponse>> ValidateData([FromBody] MigrationRequest request)
        {
            var response = new MigrationResponse();
            
            try
            {
                _logger.LogInformation("Starting data validation for {TableCount} tables", 
                    request.Tables?.Count ?? 0);
                
                // Create a validation run record
                var run = new DataTransferRun
                {
                    ConfigurationId = request.ConfigurationId,
                    StartTime = DateTime.UtcNow,
                    Status = "Validating",
                    TriggeredBy = User.Identity?.Name ?? "System",
                    TotalTablesProcessed = request.Tables?.Count ?? 0
                };
                
                _progressPlayDbContext.DataTransferRuns.Add(run);
                await _progressPlayDbContext.SaveChangesAsync();
                
                // Log the start of validation
                var startLog = new DataTransferLog
                {
                    RunId = run.RunId,
                    LogTime = DateTime.UtcNow,
                    LogLevel = "Information",
                    Message = $"Starting data validation for configuration ID {request.ConfigurationId}"
                };
                
                _progressPlayDbContext.DataTransferLogs.Add(startLog);
                await _progressPlayDbContext.SaveChangesAsync();
                
                try
                {
                    // Run validation
                    var tables = request.Tables?.Count > 0 
                        ? request.Tables 
                        : _migrationService.GetProcessedTables();
                    
                    var validationResults = await _validationService.ValidateAsync(tables);
                    
                    int successCount = 0;
                    int failureCount = 0;
                    
                    foreach (var result in validationResults)
                    {
                        response.ValidationMessages.Add(new ValidationMessage
                        {
                            TableName = result.TableName,
                            ValidationType = result.ValidationType,
                            Success = result.Success,
                            Details = result.Details,
                            ErrorMessage = result.ErrorMessage
                        });
                        
                        // Log each validation result
                        var resultLog = new DataTransferLog
                        {
                            RunId = run.RunId,
                            LogTime = DateTime.UtcNow,
                            LogLevel = result.Success ? "Information" : "Warning",
                            Message = $"Validation {result.ValidationType} for table {result.TableName}: " +
                                     (result.Success ? "Passed" : "Failed") +
                                     (string.IsNullOrEmpty(result.Details) ? "" : $" - {result.Details}")
                        };
                        
                        _progressPlayDbContext.DataTransferLogs.Add(resultLog);
                        
                        if (result.Success)
                            successCount++;
                        else
                            failureCount++;
                    }
                    
                    // Update run status
                    run.Status = "Completed";
                    run.SuccessfulTablesCount = successCount;
                    run.FailedTablesCount = failureCount;
                    run.EndTime = DateTime.UtcNow;
                    run.ElapsedMs = (long)(run.EndTime.Value - run.StartTime).TotalMilliseconds;
                    
                    // Log completion
                    var completionLog = new DataTransferLog
                    {
                        RunId = run.RunId,
                        LogTime = DateTime.UtcNow,
                        LogLevel = "Information",
                        Message = $"Validation completed with {successCount} successes and {failureCount} failures"
                    };
                    
                    _progressPlayDbContext.DataTransferLogs.Add(completionLog);
                }
                catch (Exception ex)
                {
                    // Update run record with failure status
                    run.Status = "Failed";
                    run.EndTime = DateTime.UtcNow;
                    run.ElapsedMs = (long)(run.EndTime.Value - run.StartTime).TotalMilliseconds;
                    
                    // Log the error
                    var errorLog = new DataTransferLog
                    {
                        RunId = run.RunId,
                        LogTime = DateTime.UtcNow,
                        LogLevel = "Error",
                        Message = $"Error during data validation: {ex.Message}",
                        Exception = ex.ToString()
                    };
                    
                    _progressPlayDbContext.DataTransferLogs.Add(errorLog);
                    
                    // Rethrow to be caught by the outer try/catch
                    throw;
                }
                finally
                {
                    // Save all changes
                    await _progressPlayDbContext.SaveChangesAsync();
                }
                
                response.Success = !response.ValidationMessages.Any(v => !v.Success);
                response.Message = response.Success 
                    ? "All validations passed successfully" 
                    : "Some validations failed";
                response.RunId = run.RunId;
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during data validation");
                
                response.Success = false;
                response.Message = $"Error during data validation: {ex.Message}";
                
                return StatusCode(500, response);
            }
        }

        [HttpGet("runs")]
        public async Task<ActionResult<IEnumerable<RunSummary>>> GetRuns(int? configurationId = null, int limit = 50)
        {
            try
            {
                var query = _progressPlayDbContext.DataTransferRuns
                    .Include(r => r.Configuration)
                    .AsQueryable();
                
                if (configurationId.HasValue)
                {
                    query = query.Where(r => r.ConfigurationId == configurationId.Value);
                }
                
                var runs = await query
                    .OrderByDescending(r => r.StartTime)
                    .Take(limit)
                    .Select(r => new RunSummary
                    {
                        RunId = r.RunId,
                        ConfigurationId = r.ConfigurationId,
                        ConfigurationName = r.Configuration.ConfigurationName,
                        StartTime = r.StartTime,
                        EndTime = r.EndTime,
                        Status = r.Status,
                        TablesProcessed = r.TotalTablesProcessed,
                        TablesSucceeded = r.SuccessfulTablesCount,
                        TablesFailed = r.FailedTablesCount,
                        RowsProcessed = r.TotalRowsProcessed,
                        ElapsedMs = r.ElapsedMs,
                        AverageRowsPerSecond = r.AverageRowsPerSecond,
                        TriggeredBy = r.TriggeredBy
                    })
                    .ToListAsync();
                
                return Ok(runs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving run history");
                return StatusCode(500, "Error retrieving run history");
            }
        }

        [HttpGet("runs/{runId}")]
        public async Task<ActionResult<RunDetail>> GetRunDetail(int runId)
        {
            try
            {
                var run = await _progressPlayDbContext.DataTransferRuns
                    .Include(r => r.Configuration)
                    .FirstOrDefaultAsync(r => r.RunId == runId);
                
                if (run == null)
                {
                    return NotFound($"Run with ID {runId} not found");
                }
                
                // Get logs for this run
                var logs = await _progressPlayDbContext.DataTransferLogs
                    .Where(l => l.RunId == runId)
                    .OrderBy(l => l.LogTime)
                    .Select(l => new LogEntry
                    {
                        LogId = l.LogId,
                        LogTime = l.LogTime,
                        LogLevel = l.LogLevel,
                        Message = l.Message,
                        Exception = l.Exception
                    })
                    .ToListAsync();
                
                var runDetail = new RunDetail
                {
                    RunId = run.RunId,
                    ConfigurationId = run.ConfigurationId,
                    ConfigurationName = run.Configuration.ConfigurationName,
                    StartTime = run.StartTime,
                    EndTime = run.EndTime,
                    Status = run.Status,
                    TablesProcessed = run.TotalTablesProcessed,
                    TablesSucceeded = run.SuccessfulTablesCount,
                    TablesFailed = run.FailedTablesCount,
                    RowsProcessed = run.TotalRowsProcessed,
                    ElapsedMs = run.ElapsedMs,
                    AverageRowsPerSecond = run.AverageRowsPerSecond,
                    TriggeredBy = run.TriggeredBy,
                    Logs = logs
                };
                
                return Ok(runDetail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving run detail for run ID {RunId}", runId);
                return StatusCode(500, "Error retrieving run detail");
            }
        }
    }

    // Additional response models
    public class RunSummary
    {
        public int RunId { get; set; }
        public int ConfigurationId { get; set; }
        public string ConfigurationName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Status { get; set; }
        public int TablesProcessed { get; set; }
        public int TablesSucceeded { get; set; }
        public int TablesFailed { get; set; }
        public int RowsProcessed { get; set; }
        public long ElapsedMs { get; set; }
        public double AverageRowsPerSecond { get; set; }
        public string TriggeredBy { get; set; }
    }

    public class RunDetail : RunSummary
    {
        public List<LogEntry> Logs { get; set; } = new List<LogEntry>();
    }

    public class LogEntry
    {
        public int LogId { get; set; }
        public DateTime LogTime { get; set; }
        public string LogLevel { get; set; }
        public string Message { get; set; }
        public string Exception { get; set; }
    }
}