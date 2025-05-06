using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MCPServer.Core.Features.DataTransfer.Models;

namespace MCPServer.Core.Features.DataTransfer.Services
{
    public class DataValidationService
    {
        private readonly ILogger<DataValidationService> _logger;
        private readonly string _sourceConnectionString;
        private readonly string _targetConnectionString;

        public DataValidationService(IConfiguration config, ILogger<DataValidationService> logger)
        {
            _logger = logger;
            _sourceConnectionString = config.GetConnectionString("DataTransfer:Source");
            _targetConnectionString = config.GetConnectionString("DataTransfer:Target");
        }

        public async Task<IEnumerable<ValidationResult>> ValidateAsync(IEnumerable<string> tablesToValidate)
        {
            var results = new List<ValidationResult>();
            var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
            var tableMappings = config.GetSection("DataTransfer:TableMappings").Get<List<TableMapping>>();

            foreach (var tableName in tablesToValidate)
            {
                var mapping = tableMappings.FirstOrDefault(m => m.SourceTable == tableName && m.Enabled);
                if (mapping != null)
                {
                    var tableResults = await ValidateTableDataAsync(mapping);
                    results.AddRange(tableResults);
                }
            }

            return results;
        }

        private async Task<IEnumerable<ValidationResult>> ValidateTableDataAsync(TableMapping mapping)
        {
            _logger.LogInformation("Starting data validation for table {SourceTable} to {TargetTable}",
                mapping.SourceTable, mapping.TargetTable);

            var validations = new List<ValidationResult>();

            // Validate row counts
            var rowCountValidation = await ValidateRowCountsAsync(mapping);
            validations.Add(rowCountValidation);

            // Validate column existence and data types
            var columnValidation = await ValidateColumnsAsync(mapping);
            validations.Add(columnValidation);

            // Validate key data samples
            if (mapping.IncrementalColumn != null)
            {
                var keyDataValidation = await ValidateKeyDataAsync(mapping);
                validations.Add(keyDataValidation);
            }

            // Log validation results
            foreach (var validation in validations)
            {
                if (validation.Success)
                {
                    _logger.LogInformation("Validation passed: {ValidationType} for {TableName}",
                        validation.ValidationType, mapping.SourceTable);
                }
                else
                {
                    _logger.LogError("Validation failed: {ValidationType} for {TableName}: {ErrorMessage}",
                        validation.ValidationType, mapping.SourceTable, validation.ErrorMessage);
                }
            }

            return validations;
        }

        private async Task<ValidationResult> ValidateRowCountsAsync(TableMapping mapping)
        {
            var result = new ValidationResult
            {
                ValidationType = "RowCount",
                TableName = mapping.SourceTable
            };

            try
            {
                var sourceCount = await GetTableRowCountAsync(_sourceConnectionString, 
                    mapping.SourceSchema, mapping.SourceTable, mapping.CustomWhere);
                
                var targetCount = await GetTableRowCountAsync(_targetConnectionString, 
                    mapping.TargetSchema, mapping.TargetTable, mapping.CustomWhere);

                if (sourceCount == targetCount)
                {
                    result.Success = true;
                    result.Details = $"Source: {sourceCount}, Target: {targetCount}";
                }
                else
                {
                    result.Success = false;
                    result.ErrorMessage = $"Row count mismatch. Source: {sourceCount}, Target: {targetCount}";
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Error validating row counts: {ex.Message}";
            }

            return result;
        }

        private async Task<ValidationResult> ValidateColumnsAsync(TableMapping mapping)
        {
            var result = new ValidationResult
            {
                ValidationType = "ColumnSchema",
                TableName = mapping.SourceTable
            };

            try
            {
                var sourceColumns = await GetTableColumnsAsync(_sourceConnectionString, 
                    mapping.SourceSchema, mapping.SourceTable);
                
                var targetColumns = await GetTableColumnsAsync(_targetConnectionString, 
                    mapping.TargetSchema, mapping.TargetTable);

                var errors = new List<string>();
                
                foreach (var colMapping in mapping.ColumnMappings)
                {
                    // Validate source column exists
                    if (!sourceColumns.ContainsKey(colMapping.SourceColumn))
                    {
                        errors.Add($"Source column '{colMapping.SourceColumn}' does not exist");
                        continue;
                    }
                    
                    // Validate target column exists
                    if (!targetColumns.ContainsKey(colMapping.TargetColumn))
                    {
                        errors.Add($"Target column '{colMapping.TargetColumn}' does not exist");
                        continue;
                    }
                    
                    // Validate data types are compatible
                    var sourceType = sourceColumns[colMapping.SourceColumn];
                    var targetType = targetColumns[colMapping.TargetColumn];
                    
                    if (!AreTypesCompatible(sourceType, targetType))
                    {
                        errors.Add($"Column type mismatch for '{colMapping.SourceColumn}'/'{colMapping.TargetColumn}': {sourceType} vs {targetType}");
                    }
                }

                if (errors.Count == 0)
                {
                    result.Success = true;
                    result.Details = $"All {mapping.ColumnMappings.Count} columns validated successfully";
                }
                else
                {
                    result.Success = false;
                    result.ErrorMessage = string.Join("; ", errors);
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Error validating columns: {ex.Message}";
            }

            return result;
        }

        private async Task<ValidationResult> ValidateKeyDataAsync(TableMapping mapping)
        {
            var result = new ValidationResult
            {
                ValidationType = "KeyData",
                TableName = mapping.SourceTable
            };

            try
            {
                // Choose a column to validate (preferably the key or incremental column)
                var columnToValidate = mapping.IncrementalColumn;
                
                // Get a small sample of data to validate
                var sourceSample = await GetColumnSampleAsync(_sourceConnectionString, 
                    mapping.SourceSchema, mapping.SourceTable, columnToValidate);
                
                var matchingCount = 0;
                var mismatchDetails = new List<string>();
                
                foreach (var (key, value) in sourceSample)
                {
                    var targetValue = await GetValueByKeyAsync(_targetConnectionString, 
                        mapping.TargetSchema, mapping.TargetTable, columnToValidate, key);
                    
                    if (AreValuesEqual(value, targetValue))
                    {
                        matchingCount++;
                    }
                    else
                    {
                        mismatchDetails.Add($"Key {key}: Source={value}, Target={targetValue}");
                    }
                    
                    if (mismatchDetails.Count >= 5)
                        break; // Limit the number of mismatch details
                }

                if (mismatchDetails.Count == 0)
                {
                    result.Success = true;
                    result.Details = $"All {sourceSample.Count} sample keys matched";
                }
                else
                {
                    result.Success = false;
                    result.ErrorMessage = $"{mismatchDetails.Count} of {sourceSample.Count} keys mismatched: {string.Join("; ", mismatchDetails)}";
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Error validating key data: {ex.Message}";
            }

            return result;
        }

        private async Task<int> GetTableRowCountAsync(string connectionString, string schema, string tableName, string whereClause = null)
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            
            var query = $"SELECT COUNT(*) FROM [{schema}].[{tableName}]";
            if (!string.IsNullOrEmpty(whereClause))
            {
                query += $" WHERE {whereClause}";
            }
            
            await using var command = new SqlCommand(query, connection);
            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }

        private async Task<Dictionary<string, string>> GetTableColumnsAsync(string connectionString, string schema, string tableName)
        {
            var columns = new Dictionary<string, string>();
            
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            
            var query = @"
                SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_SCHEMA = @Schema AND TABLE_NAME = @TableName";
            
            await using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Schema", schema);
            command.Parameters.AddWithValue("@TableName", tableName);
            
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var columnName = reader.GetString(0);
                var dataType = reader.GetString(1);
                var maxLength = reader.IsDBNull(2) ? null : (int?)reader.GetInt32(2);
                
                var typeStr = dataType;
                if (maxLength.HasValue && maxLength.Value != -1)
                {
                    typeStr += $"({maxLength})";
                }
                else if (maxLength.HasValue && maxLength.Value == -1)
                {
                    typeStr += "(MAX)";
                }
                
                columns[columnName] = typeStr;
            }
            
            return columns;
        }

        private async Task<Dictionary<string, object>> GetColumnSampleAsync(string connectionString, 
            string schema, string tableName, string columnName, int sampleSize = 10)
        {
            var sample = new Dictionary<string, object>();
            
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            
            // Get a sample of rows
            var query = $"SELECT TOP {sampleSize} {columnName} FROM [{schema}].[{tableName}] ORDER BY {columnName}";
            
            await using var command = new SqlCommand(query, connection);
            await using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                var value = reader.GetValue(0);
                sample[value.ToString()] = value;
            }
            
            return sample;
        }

        private async Task<object> GetValueByKeyAsync(string connectionString, 
            string schema, string tableName, string columnName, string keyValue)
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            
            var query = $"SELECT TOP 1 {columnName} FROM [{schema}].[{tableName}] WHERE {columnName} = @KeyValue";
            
            await using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@KeyValue", keyValue);
            
            var result = await command.ExecuteScalarAsync();
            return result ?? DBNull.Value;
        }

        private bool AreTypesCompatible(string sourceType, string targetType)
        {
            // Basic compatibility check - this could be expanded for more detailed checks
            if (sourceType == targetType)
                return true;
            
            // Check numeric type compatibility
            var numericTypes = new[] { "int", "bigint", "smallint", "tinyint", "decimal", "numeric", "float", "real", "money", "smallmoney" };
            if (numericTypes.Any(t => sourceType.StartsWith(t)) && numericTypes.Any(t => targetType.StartsWith(t)))
                return true;
            
            // Check string type compatibility
            var stringTypes = new[] { "char", "varchar", "nchar", "nvarchar", "text", "ntext" };
            if (stringTypes.Any(t => sourceType.StartsWith(t)) && stringTypes.Any(t => targetType.StartsWith(t)))
                return true;
            
            // Check date type compatibility
            var dateTypes = new[] { "datetime", "smalldatetime", "date", "time", "datetime2", "datetimeoffset" };
            if (dateTypes.Any(t => sourceType.StartsWith(t)) && dateTypes.Any(t => targetType.StartsWith(t)))
                return true;
            
            return false;
        }

        private bool AreValuesEqual(object value1, object value2)
        {
            if (value1 == null && value2 == null)
                return true;
            
            if (value1 == null || value2 == null)
                return false;
            
            if (value1 is DBNull && value2 is DBNull)
                return true;
            
            if (value1 is DBNull || value2 is DBNull)
                return false;
            
            // Handle different numeric types
            if (value1 is IConvertible && value2 is IConvertible)
            {
                try
                {
                    var type = Type.GetTypeCode(value1.GetType());
                    switch (type)
                    {
                        case TypeCode.Int16:
                        case TypeCode.Int32:
                        case TypeCode.Int64:
                        case TypeCode.UInt16:
                        case TypeCode.UInt32:
                        case TypeCode.UInt64:
                        case TypeCode.Byte:
                        case TypeCode.SByte:
                            return Convert.ToInt64(value1) == Convert.ToInt64(value2);
                        
                        case TypeCode.Decimal:
                        case TypeCode.Double:
                        case TypeCode.Single:
                            // Allow for small floating-point rounding differences
                            return Math.Abs(Convert.ToDouble(value1) - Convert.ToDouble(value2)) < 0.000001;
                        
                        case TypeCode.DateTime:
                            var dt1 = Convert.ToDateTime(value1);
                            var dt2 = Convert.ToDateTime(value2);
                            return Math.Abs((dt1 - dt2).TotalSeconds) < 1; // Allow 1 second difference
                    }
                }
                catch
                {
                    // If conversion fails, fall back to string comparison
                }
            }
            
            // Default comparison
            return value1.ToString() == value2.ToString();
        }
    }
}