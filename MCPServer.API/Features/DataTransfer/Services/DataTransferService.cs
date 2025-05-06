using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MCPServer.API.Features.DataTransfer.Models;
using MCPServer.Core.Features.DataTransfer.Services;
using Microsoft.Extensions.Logging;

namespace MCPServer.API.Features.DataTransfer.Services
{
    public class DataTransferService
    {
        private readonly ILogger<DataTransferService> _logger;
        private readonly DataMigrationService _migrationService;
        private readonly DataValidationService _validationService;

        public DataTransferService(
            ILogger<DataTransferService> logger,
            DataMigrationService migrationService,
            DataValidationService validationService)
        {
            _logger = logger;
            _migrationService = migrationService;
            _validationService = validationService;
        }

        public async Task<MigrationResponse> ExecuteMigrationAsync(MigrationRequest request)
        {
            var response = new MigrationResponse();
            
            try
            {
                _logger.LogInformation("Executing migration for {Count} tables", request.Tables?.Count ?? 0);
                
                // Configure migration
                if (request.Tables?.Count > 0)
                {
                    _migrationService.SetTableFilter(request.Tables);
                }
                
                if (request.DryRun)
                {
                    _migrationService.EnableDryRun();
                }
                
                // Execute migration
                await _migrationService.RunMigrationAsync();
                
                // Set response data
                response.Success = true;
                response.Message = request.DryRun 
                    ? "Dry run completed successfully" 
                    : "Migration completed successfully";
                
                // If validation is requested, perform it
                if (request.Validate && !request.DryRun)
                {
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
                    }
                    
                    // If any validations failed, update response message
                    if (response.ValidationMessages.Exists(v => !v.Success))
                    {
                        response.Message += ", but some validations failed";
                    }
                }
                
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during migration execution");
                response.Success = false;
                response.Message = $"Error during migration: {ex.Message}";
                
                return response;
            }
        }

        public async Task<MigrationResponse> ExecuteValidationAsync(MigrationRequest request)
        {
            var response = new MigrationResponse();
            
            try
            {
                _logger.LogInformation("Executing validation for {Count} tables", request.Tables?.Count ?? 0);
                
                var tables = request.Tables?.Count > 0 
                    ? request.Tables 
                    : _migrationService.GetProcessedTables();
                
                var validationResults = await _validationService.ValidateAsync(tables);
                
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
                }
                
                response.Success = !response.ValidationMessages.Exists(v => !v.Success);
                response.Message = response.Success 
                    ? "All validations passed successfully" 
                    : "Some validations failed";
                
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during validation execution");
                response.Success = false;
                response.Message = $"Error during validation: {ex.Message}";
                
                return response;
            }
        }
    }
}