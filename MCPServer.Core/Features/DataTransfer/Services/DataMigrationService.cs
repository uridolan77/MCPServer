using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using MCPServer.Core.Features.DataTransfer.Models;

namespace MCPServer.Core.Features.DataTransfer.Services
{
    public class DataMigrationService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<DataMigrationService> _logger;
        private readonly string _sourceConnectionString;
        private readonly string _targetConnectionString;
        private readonly bool _identicalSourceAndTarget;
        private List<TableMapping> _tableMappings;
        private readonly int _batchSize;
        private readonly int _commandTimeout;
        private readonly bool _enableTransaction;
        private readonly string _stateStorePath;
        private readonly Dictionary<string, object> _migrationState;
        private readonly MigrationMonitor _monitor;
        private bool _isDryRun = false;
        private List<string> _tableFilter;


        public DataMigrationService(IConfiguration config, ILogger<DataMigrationService> logger)
        {
            _config = config;
            _logger = logger;
            _sourceConnectionString = config.GetConnectionString("DataTransfer:Source");
            _targetConnectionString = config.GetConnectionString("DataTransfer:Target");
            _identicalSourceAndTarget = config.GetValue<bool>("Settings:IdenticalSourceAndTarget", false);
            // Load table mappings from configuration
            _tableMappings = config.GetSection("DataTransfer:TableMappings").Get<List<TableMapping>>();
            
            // Load additional settings
            _batchSize = config.GetValue<int>("DataTransfer:Settings:BatchSize", 1000);
            _commandTimeout = config.GetValue<int>("DataTransfer:Settings:CommandTimeout", 300);
            _enableTransaction = config.GetValue<bool>("DataTransfer:Settings:EnableTransaction", true);
            _stateStorePath = config.GetValue<string>("DataTransfer:Settings:StateStorePath", "migrationState.json");
            
            // Load or initialize migration state
            _migrationState = LoadMigrationState();
            
            // Initialize monitor
            _monitor = new MigrationMonitor(logger);
        }

        public void SetTableFilter(IEnumerable<string> tableNames)
        {
            _tableFilter = tableNames.ToList();
            _logger.LogInformation("Table filter set to: {TableFilter}", string.Join(", ", _tableFilter));
        }

        public void EnableDryRun()
        {
            _isDryRun = true;
            _logger.LogInformation("Dry run mode enabled - no data will be written to target database");
        }

        public IEnumerable<string> GetProcessedTables()
        {
            return _tableMappings.Where(m => m.Enabled).Select(m => m.SourceTable);
        }

        private Dictionary<string, object> LoadMigrationState()
        {
            if (File.Exists(_stateStorePath))
            {
                try
                {
                    var json = File.ReadAllText(_stateStorePath);
                    return JsonConvert.DeserializeObject<Dictionary<string, object>>(json) 
                           ?? new Dictionary<string, object>();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading migration state, starting fresh");
                    return new Dictionary<string, object>();
                }
            }
            return new Dictionary<string, object>();
        }

        private void SaveMigrationState()
        {
            try
            {
                var json = JsonConvert.SerializeObject(_migrationState);
                File.WriteAllText(_stateStorePath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving migration state");
            }
        }

        public async Task RunMigrationAsync()
        {
            _monitor.StartMigration();
            
            // Apply table filter if specified
            if (_tableFilter != null && _tableFilter.Count > 0)
            {
                _tableMappings = _tableMappings.Where(m => _tableFilter.Contains(m.SourceTable)).ToList();
                _logger.LogInformation("Filtered to {Count} tables", _tableMappings.Count);
            }

            foreach (var mapping in _tableMappings.Where(m => m.Enabled))
            {
                try
                {
                    await MigrateTableAsync(mapping);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error migrating table {SourceTable} to {TargetTable}", mapping.SourceTable, mapping.TargetTable);
                    
                    if (mapping.FailOnError)
                        throw;
                }
            }
            
            _monitor.EndMigration();
        }

        // Modified method to handle retrieving or auto-generating column mappings
        private async Task<List<ColumnMapping>> GetColumnMappingsAsync(TableMapping mapping)
        {
            // If column mappings are already defined and we're not using identical mode, return them
            if (mapping.ColumnMappings?.Count > 0 && !_identicalSourceAndTarget)
            {
                return mapping.ColumnMappings;
            }

            // If we're in identical mode or no column mappings defined, auto-generate them
            await using var sourceConnection = new SqlConnection(_sourceConnectionString);
            await sourceConnection.OpenAsync();

            var query = @"
            SELECT 
                COLUMN_NAME, 
                DATA_TYPE,
                CHARACTER_MAXIMUM_LENGTH,
                IS_NULLABLE
            FROM INFORMATION_SCHEMA.COLUMNS 
            WHERE TABLE_SCHEMA = @Schema 
            AND TABLE_NAME = @TableName
            ORDER BY ORDINAL_POSITION";

            await using var command = new SqlCommand(query, sourceConnection);
            command.Parameters.AddWithValue("@Schema", mapping.SourceSchema);
            command.Parameters.AddWithValue("@TableName", mapping.SourceTable);

            var columnMappings = new List<ColumnMapping>();

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var columnName = reader.GetString(0);
                var dataType = reader.GetString(1);
                var maxLength = reader.IsDBNull(2) ? null : (int?)reader.GetInt32(2);
                var isNullable = reader.GetString(3) == "YES";

                var columnMapping = new ColumnMapping
                {
                    SourceColumn = columnName,
                    TargetColumn = columnName, // Same name for identical mapping
                    DataType = dataType,
                    AllowNull = isNullable
                };

                columnMappings.Add(columnMapping);
            }

            return columnMappings;
        }

        private async Task MigrateTableAsync(TableMapping mapping)
        {
            _logger.LogInformation("Starting migration for table {SourceTable} to {TargetTable}", mapping.SourceTable, mapping.TargetTable);
            _monitor.StartTableMigration(mapping.SourceTable);

            // Get column mappings (auto-generated if needed)
            var columnMappings = await GetColumnMappingsAsync(mapping);

            // If we're using identical mode, ensure the mapping's ColumnMappings is set
            if (_identicalSourceAndTarget)
            {
                mapping.ColumnMappings = columnMappings;
            }

            // Get the last migration timestamp or ID
            var lastValue = GetLastMigrationValue(mapping);
            _logger.LogInformation("Last migration value for {SourceTable}: {LastValue}", mapping.SourceTable, lastValue);

            // Create query for incremental load
            string query = BuildIncrementalQuery(mapping, lastValue);
            _logger.LogDebug("Query: {Query}", query);

            await using var sourceConnection = new SqlConnection(_sourceConnectionString);
            await sourceConnection.OpenAsync();

            await using var targetConnection = new SqlConnection(_targetConnectionString);
            if (!_isDryRun)
            {
                await targetConnection.OpenAsync();
            }

            // Set up command
            await using var command = new SqlCommand(query, sourceConnection)
            {
                CommandTimeout = _commandTimeout
            };

            // Execute the query and process in batches
            await using var reader = await command.ExecuteReaderAsync();
            
            int totalRows = 0;
            int currentBatch = 0;
            
            // Get column schema from reader
            var schemaTable = reader.GetSchemaTable();
            var columns = new List<string>();
            if (schemaTable != null)
            {
                foreach (DataRow row in schemaTable.Rows)
                {
                    columns.Add(row["ColumnName"].ToString());
                }
            }

            // Process data in batches
            var batchData = new List<object[]>();
            object newLastValue = lastValue;

            while (await reader.ReadAsync())
            {
                var rowData = new object[reader.FieldCount];
                reader.GetValues(rowData);
                batchData.Add(rowData);
                currentBatch++;
                totalRows++;

                // Update the tracking value if applicable
                if (mapping.IncrementalType != "None")
                {
                    int trackingColumnIndex = columns.IndexOf(mapping.IncrementalColumn);
                    if (trackingColumnIndex >= 0)
                    {
                        var currentValue = reader.GetValue(trackingColumnIndex);
                        if (mapping.IncrementalType == "DateTime")
                        {
                            if (currentValue is DateTime dateTime && 
                                (newLastValue == null || (newLastValue is DateTime lastDateTime && lastDateTime < dateTime)))
                            {
                                newLastValue = dateTime;
                            }
                        }
                        else if (mapping.IncrementalType == "Int" || mapping.IncrementalType == "BigInt")
                        {
                            if (currentValue is long longValue && 
                                (newLastValue == null || (newLastValue is long lastLong && lastLong < longValue)))
                            {
                                newLastValue = longValue;
                            }
                            else if (currentValue is int intValue && 
                                     (newLastValue == null || (newLastValue is int lastInt && lastInt < intValue)))
                            {
                                newLastValue = intValue;
                            }
                        }
                    }
                }

                // Process the batch if batch size is reached
                if (currentBatch >= _batchSize)
                {
                    if (!_isDryRun)
                    {
                        await BulkInsertDataAsync(targetConnection, mapping, columns, batchData);
                    }
                    _logger.LogInformation("Processed batch of {Count} rows for {TargetTable}", currentBatch, mapping.TargetTable);
                    _monitor.UpdateTableProgress(mapping.SourceTable, totalRows);
                    batchData.Clear();
                    currentBatch = 0;
                }
            }

            // Process any remaining records
            if (batchData.Count > 0 && !_isDryRun)
            {
                await BulkInsertDataAsync(targetConnection, mapping, columns, batchData);
                _logger.LogInformation("Processed final batch of {Count} rows for {TargetTable}", currentBatch, mapping.TargetTable);
            }

            // Update the last migration value
            if (mapping.IncrementalType != "None" && newLastValue != null && !object.Equals(newLastValue, lastValue))
            {
                SetLastMigrationValue(mapping, newLastValue);
                if (!_isDryRun)
                {
                    SaveMigrationState();
                }
            }

            _monitor.EndTableMigration(mapping.SourceTable, totalRows);
            _logger.LogInformation("Completed migration for table {SourceTable} to {TargetTable}. Total rows: {TotalRows}", mapping.SourceTable, mapping.TargetTable, totalRows);
        }

        private object GetLastMigrationValue(TableMapping mapping)
        {
            string key = $"{mapping.SourceTable}_{mapping.IncrementalColumn}";
            
            if (_migrationState.TryGetValue(key, out var value))
            {
                if (mapping.IncrementalType == "DateTime" && value is string dateStr)
                {
                    return DateTime.Parse(dateStr);
                }
                return value;
            }
            
            return mapping.IncrementalStartValue;
        }

        private void SetLastMigrationValue(TableMapping mapping, object value)
        {
            string key = $"{mapping.SourceTable}_{mapping.IncrementalColumn}";
            _migrationState[key] = value;
        }

        private string BuildIncrementalQuery(TableMapping mapping, object lastValue)
        {
            var columnList = string.Join(", ", mapping.ColumnMappings.Select(c => c.SourceColumn));
            var query = new StringBuilder();
            
            query.Append($"SELECT {columnList} FROM {mapping.SourceSchema}.{mapping.SourceTable}");
            
            if (mapping.IncrementalType != "None" && lastValue != null)
            {
                string compareOperator = mapping.IncrementalCompareOperator ?? ">";
                
                if (mapping.IncrementalType == "DateTime" && lastValue is DateTime)
                {
                    var dateTime = (DateTime)lastValue;
                    query.Append($" WHERE {mapping.IncrementalColumn} {compareOperator} '{dateTime:yyyy-MM-dd HH:mm:ss.fff}'");
                }
                else
                {
                    query.Append($" WHERE {mapping.IncrementalColumn} {compareOperator} {lastValue}");
                }
            }
            
            if (!string.IsNullOrEmpty(mapping.CustomWhere))
            {
                query.Append(mapping.IncrementalType != "None" && lastValue != null ? " AND " : " WHERE ");
                query.Append(mapping.CustomWhere);
            }
            
            if (!string.IsNullOrEmpty(mapping.OrderBy))
            {
                query.Append($" ORDER BY {mapping.OrderBy}");
            }
            else if (mapping.IncrementalType != "None")
            {
                query.Append($" ORDER BY {mapping.IncrementalColumn}");
            }
            
            if (mapping.TopN > 0)
            {
                // Add TOP N clause for SQL Server
                query.Insert(7, $" TOP {mapping.TopN} ");
            }
            
            return query.ToString();
        }

        private async Task BulkInsertDataAsync(SqlConnection connection, TableMapping mapping, 
                                  List<string> columns, List<object[]> data)
        {
            if (data.Count == 0) return;

            using var dataTable = new DataTable();
            
            // Create columns in the DataTable
            foreach (var colMapping in mapping.ColumnMappings)
            {
                var type = GetColumnType(colMapping.DataType);
                var column = new DataColumn(colMapping.TargetColumn, type)
                {
                    AllowDBNull = colMapping.AllowNull
                };
                dataTable.Columns.Add(column);
            }
            
            // Add rows to the DataTable
            foreach (var rowData in data)
            {
                var row = dataTable.NewRow();
                for (int i = 0; i < columns.Count; i++)
                {
                    var sourceColumn = columns[i];
                    var mappedColumn = mapping.ColumnMappings.FirstOrDefault(c => c.SourceColumn == sourceColumn);
                    
                    if (mappedColumn != null)
                    {
                        int mappedIndex = mapping.ColumnMappings.IndexOf(mappedColumn);
                        
                        if (rowData[i] == DBNull.Value && !mappedColumn.AllowNull)
                        {
                            // Apply default value if specified
                            row[mappedColumn.TargetColumn] = mappedColumn.DefaultValue ?? DBNull.Value;
                        }
                        else
                        {
                            // Apply any transformations if needed
                            row[mappedColumn.TargetColumn] = ApplyTransformation(rowData[i], mappedColumn);
                        }
                    }
                }
                dataTable.Rows.Add(row);
            }

            // Use SqlBulkCopy for efficient insertion
            using var bulkCopy = new SqlBulkCopy(connection)
            {
                DestinationTableName = $"{mapping.TargetSchema}.{mapping.TargetTable}",
                BulkCopyTimeout = _commandTimeout
            };
            
            // Map columns
            foreach (var colMapping in mapping.ColumnMappings)
            {
                bulkCopy.ColumnMappings.Add(colMapping.TargetColumn, colMapping.TargetColumn);
            }
            
            // Set options based on configuration
            if (mapping.BulkCopyOptions != null)
            {
                var options = SqlBulkCopyOptions.Default;
                
                if (mapping.BulkCopyOptions.CheckConstraints)
                    options |= SqlBulkCopyOptions.CheckConstraints;
                
                if (mapping.BulkCopyOptions.KeepIdentity)
                    options |= SqlBulkCopyOptions.KeepIdentity;
                
                if (mapping.BulkCopyOptions.KeepNulls)
                    options |= SqlBulkCopyOptions.KeepNulls;
                
                if (mapping.BulkCopyOptions.TableLock)
                    options |= SqlBulkCopyOptions.TableLock;
                
                if (mapping.BulkCopyOptions.FireTriggers)
                    options |= SqlBulkCopyOptions.FireTriggers;
                
                bulkCopy.BulkCopyTimeout = mapping.BulkCopyOptions.Timeout ?? _commandTimeout;
            }
            
            try
            {
                await bulkCopy.WriteToServerAsync(dataTable);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk insert to {TargetTable}", mapping.TargetTable);
                throw;
            }
        }

        private Type GetColumnType(string dataType)
        {
            return dataType?.ToLowerInvariant() switch
            {
                "int" => typeof(int),
                "bigint" => typeof(long),
                "smallint" => typeof(short),
                "tinyint" => typeof(byte),
                "bit" => typeof(bool),
                "decimal" or "money" or "smallmoney" => typeof(decimal),
                "float" => typeof(double),
                "real" => typeof(float),
                "datetime" or "smalldatetime" or "date" or "datetime2" => typeof(DateTime),
                "time" => typeof(TimeSpan),
                "uniqueidentifier" => typeof(Guid),
                _ => typeof(string)
            };
        }

        private object ApplyTransformation(object value, ColumnMapping mapping)
        {
            if (value == DBNull.Value)
            {
                // If value is DBNull, return either DBNull or the default value based on AllowNull
                if (mapping.AllowNull)
                    return DBNull.Value;
                
                // Use default value if specified, otherwise use DBNull
                return mapping.DefaultValue != null ? mapping.DefaultValue : DBNull.Value;
            }

            // Apply any transformations defined in ColumnMapping
            if (!string.IsNullOrEmpty(mapping.Transformation))
            {
                // Simple transformations can be handled here
                switch (mapping.Transformation)
                {
                    case "ToUpper":
                        {
                            string result = value?.ToString()?.ToUpperInvariant();
                            return result != null ? result : DBNull.Value;
                        }
                    case "ToLower":
                        {
                            string result = value?.ToString()?.ToLowerInvariant();
                            return result != null ? result : DBNull.Value;
                        }
                    case "Trim":
                        {
                            string result = value?.ToString()?.Trim();
                            return result != null ? result : DBNull.Value;
                        }
                    case "ConvertToInt":
                        return Convert.ToInt32(value);
                    case "ConvertToDecimal":
                        return Convert.ToDecimal(value);
                    case "FormatDateTime":
                        return value is DateTime dt ? dt.ToString(mapping.TransformationFormat) : value;
                    // Add more transformation types as needed
                }
            }

            return value;
        }
    }
}