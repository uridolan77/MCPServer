using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MCPServer.API.Features.DataTransfer.Models;
using MCPServer.Core.Features.DataTransfer.Models;

namespace MCPServer.API.Features.DataTransfer.Controllers
{
    [ApiController]
    [Route("api/datatransfer/[controller]")]
    [Authorize]
    public class SchemaController : ControllerBase
    {
        private readonly ILogger<SchemaController> _logger;
        private readonly IConfiguration _configuration;

        public SchemaController(
            ILogger<SchemaController> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        [HttpGet("databases")]
        public async Task<ActionResult<IEnumerable<string>>> GetDatabases([FromQuery] string? connectionId, [FromQuery] string? connectionString)
        {
            try
            {
                string? effectiveConnectionString = null;

                if (!string.IsNullOrEmpty(connectionString))
                {
                    effectiveConnectionString = connectionString;
                }
                else if (!string.IsNullOrEmpty(connectionId))
                {
                    effectiveConnectionString = GetConnectionString(connectionId);
                }
                
                if (string.IsNullOrEmpty(effectiveConnectionString))
                {
                    return BadRequest("Connection ID or Connection String is required and must be valid.");
                }

                var databases = new List<string>();
                
                using (var connection = new SqlConnection(effectiveConnectionString))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new SqlCommand(
                        "SELECT name FROM sys.databases WHERE name NOT IN ('master', 'tempdb', 'model', 'msdb') ORDER BY name", 
                        connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                databases.Add(reader.GetString(0));
                            }
                        }
                    }
                }
                
                return Ok(databases);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving databases");
                return StatusCode(500, $"Error retrieving databases: {ex.Message}");
            }
        }

        [HttpGet("schemas")]
        public async Task<ActionResult<IEnumerable<string>>> GetSchemas(
            [FromQuery] string connectionId, 
            [FromQuery] string database)
        {
            try
            {
                string connectionString = GetConnectionString(connectionId);
                
                if (string.IsNullOrEmpty(connectionString))
                {
                    return BadRequest("Invalid connection ID or missing connection string");
                }
                
                // Add or update the database name in the connection string
                var builder = new SqlConnectionStringBuilder(connectionString)
                {
                    InitialCatalog = database
                };

                var schemas = new List<string>();
                
                using (var connection = new SqlConnection(builder.ConnectionString))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new SqlCommand(
                        "SELECT schema_name FROM information_schema.schemata ORDER BY schema_name", 
                        connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                schemas.Add(reader.GetString(0));
                            }
                        }
                    }
                }
                
                return Ok(schemas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving schemas");
                return StatusCode(500, $"Error retrieving schemas: {ex.Message}");
            }
        }

        [HttpGet("tables")]
        public async Task<ActionResult<IEnumerable<TableInfo>>> GetTables(
            [FromQuery] string connectionId,
            [FromQuery] string database,
            [FromQuery] string schema = "dbo")
        {
            try
            {
                string connectionString = GetConnectionString(connectionId);
                
                if (string.IsNullOrEmpty(connectionString))
                {
                    return BadRequest("Invalid connection ID or missing connection string");
                }
                
                // Add or update the database name in the connection string
                var builder = new SqlConnectionStringBuilder(connectionString)
                {
                    InitialCatalog = database
                };

                var tables = new List<TableInfo>();
                
                using (var connection = new SqlConnection(builder.ConnectionString))
                {
                    await connection.OpenAsync();
                    
                    // Get tables
                    string query = @"
                        SELECT 
                            t.TABLE_NAME, 
                            t.TABLE_TYPE,
                            (SELECT COUNT(1) FROM INFORMATION_SCHEMA.COLUMNS c WHERE c.TABLE_SCHEMA = t.TABLE_SCHEMA AND c.TABLE_NAME = t.TABLE_NAME) AS COLUMN_COUNT,
                            (SELECT COUNT(1) FROM sys.indexes i 
                             INNER JOIN sys.tables tab ON i.object_id = tab.object_id
                             INNER JOIN sys.schemas s ON tab.schema_id = s.schema_id
                             WHERE i.is_primary_key = 1 AND tab.name = t.TABLE_NAME AND s.name = t.TABLE_SCHEMA) AS HAS_PRIMARY_KEY
                        FROM 
                            INFORMATION_SCHEMA.TABLES t
                        WHERE 
                            t.TABLE_SCHEMA = @Schema
                        ORDER BY 
                            t.TABLE_NAME";
                    
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Schema", schema);
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                tables.Add(new TableInfo
                                {
                                    Name = reader.GetString(0),
                                    Type = reader.GetString(1),
                                    ColumnCount = reader.GetInt32(2),
                                    HasPrimaryKey = reader.GetInt32(3) > 0
                                });
                            }
                        }
                    }
                }
                
                return Ok(tables);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tables");
                return StatusCode(500, $"Error retrieving tables: {ex.Message}");
            }
        }

        [HttpGet("columns")]
        public async Task<ActionResult<IEnumerable<ColumnInfo>>> GetColumns(
            [FromQuery] string connectionId,
            [FromQuery] string database,
            [FromQuery] string tableName,
            [FromQuery] string schema = "dbo")
        {
            try
            {
                string connectionString = GetConnectionString(connectionId);
                
                if (string.IsNullOrEmpty(connectionString))
                {
                    return BadRequest("Invalid connection ID or missing connection string");
                }
                
                // Add or update the database name in the connection string
                var builder = new SqlConnectionStringBuilder(connectionString)
                {
                    InitialCatalog = database
                };

                var columns = new List<ColumnInfo>();
                
                using (var connection = new SqlConnection(builder.ConnectionString))
                {
                    await connection.OpenAsync();
                    
                    // Get columns with extended properties
                    string query = @"
                        SELECT 
                            c.COLUMN_NAME,
                            c.DATA_TYPE,
                            c.CHARACTER_MAXIMUM_LENGTH,
                            c.NUMERIC_PRECISION,
                            c.NUMERIC_SCALE,
                            c.IS_NULLABLE,
                            COLUMNPROPERTY(OBJECT_ID(c.TABLE_SCHEMA + '.' + c.TABLE_NAME), c.COLUMN_NAME, 'IsIdentity') as IS_IDENTITY,
                            (
                                SELECT COUNT(1) FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE k
                                WHERE k.TABLE_SCHEMA = c.TABLE_SCHEMA 
                                  AND k.TABLE_NAME = c.TABLE_NAME 
                                  AND k.COLUMN_NAME = c.COLUMN_NAME
                                  AND EXISTS (
                                    SELECT 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                                    WHERE tc.CONSTRAINT_NAME = k.CONSTRAINT_NAME
                                      AND tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
                                  )
                            ) as IS_PRIMARY_KEY
                        FROM 
                            INFORMATION_SCHEMA.COLUMNS c
                        WHERE 
                            c.TABLE_SCHEMA = @Schema AND c.TABLE_NAME = @TableName
                        ORDER BY 
                            c.ORDINAL_POSITION";
                    
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Schema", schema);
                        command.Parameters.AddWithValue("@TableName", tableName);
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var column = new ColumnInfo
                                {
                                    Name = reader.GetString(0),
                                    DataType = reader.GetString(1),
                                    MaxLength = reader.IsDBNull(2) ? null : (int?)reader.GetInt32(2),
                                    Precision = reader.IsDBNull(3) ? null : (int?)reader.GetInt32(3),
                                    Scale = reader.IsDBNull(4) ? null : (int?)reader.GetInt32(4),
                                    IsNullable = reader.GetString(5) == "YES",
                                    IsIdentity = reader.GetInt32(6) == 1,
                                    IsPrimaryKey = reader.GetInt32(7) > 0
                                };
                                
                                // Format the full data type including length/precision/scale
                                column.FullDataType = FormatFullDataType(column);
                                
                                columns.Add(column);
                            }
                        }
                    }
                }
                
                return Ok(columns);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving columns");
                return StatusCode(500, $"Error retrieving columns: {ex.Message}");
            }
        }

        [HttpPost("suggest-mapping")]
        public async Task<ActionResult<TableMapping>> SuggestMapping(
            [FromQuery] string sourceConnectionId,
            [FromQuery] string targetConnectionId,
            [FromQuery] string sourceDatabase,
            [FromQuery] string targetDatabase,
            [FromQuery] string sourceTable,
            [FromQuery] string? targetTable = null, // Made targetTable nullable
            [FromQuery] string sourceSchema = "dbo",
            [FromQuery] string targetSchema = "dbo")
        {
            try
            {
                // If target table not specified, use source table name
                if (string.IsNullOrEmpty(targetTable))
                {
                    targetTable = sourceTable;
                }
                
                string sourceConnectionString = GetConnectionString(sourceConnectionId);
                string targetConnectionString = GetConnectionString(targetConnectionId);
                
                if (string.IsNullOrEmpty(sourceConnectionString) || string.IsNullOrEmpty(targetConnectionString))
                {
                    return BadRequest("Invalid connection ID or missing connection string");
                }
                
                // Add database names to connection strings
                var sourceBuilder = new SqlConnectionStringBuilder(sourceConnectionString)
                {
                    InitialCatalog = sourceDatabase
                };
                
                var targetBuilder = new SqlConnectionStringBuilder(targetConnectionString)
                {
                    InitialCatalog = targetDatabase
                };

                // Get source and target columns
                var sourceColumns = await GetTableColumns(sourceBuilder.ConnectionString, sourceTable, sourceSchema);
                var targetColumns = await GetTableColumns(targetBuilder.ConnectionString, targetTable, targetSchema);
                
                if (sourceColumns.Count == 0)
                {
                    return NotFound($"Source table {sourceSchema}.{sourceTable} not found or has no columns");
                }
                
                if (targetColumns.Count == 0)
                {
                    return NotFound($"Target table {targetSchema}.{targetTable} not found or has no columns");
                }

                // Create table mapping
                var mapping = new TableMapping
                {
                    SourceSchema = sourceSchema,
                    SourceTable = sourceTable,
                    TargetSchema = targetSchema,
                    TargetTable = targetTable,
                    Enabled = true,
                    FailOnError = true
                };
                
                // Find potential timestamp column for incremental loading
                var dateColumns = sourceColumns.FindAll(c => 
                    c.DataType?.Contains("date") == true || 
                    c.DataType?.Contains("time") == true);
                
                if (dateColumns.Count > 0)
                {
                    // Prefer columns named LastModified, UpdatedAt, etc.
                    var updateColumn = dateColumns.Find(c => 
                        c.Name?.Contains("Last") == true || 
                        c.Name?.Contains("Update") == true || 
                        c.Name?.Contains("Modified") == true);
                    
                    if (updateColumn != null)
                    {
                        mapping.IncrementalType = "DateTime";
                        mapping.IncrementalColumn = updateColumn.Name!;
                        mapping.IncrementalCompareOperator = ">";
                        mapping.IncrementalStartValue = DateTime.Now.AddMonths(-1).ToString("yyyy-MM-dd HH:mm:ss");
                    }
                }
                
                // If no date column, look for identity column
                if (string.IsNullOrEmpty(mapping.IncrementalColumn))
                {
                    var identityColumn = sourceColumns.Find(c => c.IsIdentity || c.IsPrimaryKey);
                    
                    if (identityColumn != null && 
                        (identityColumn.DataType?.Contains("int") == true || identityColumn.DataType?.Contains("bigint") == true))
                    {
                        mapping.IncrementalType = identityColumn.DataType.Contains("bigint") ? "BigInt" : "Int";
                        mapping.IncrementalColumn = identityColumn.Name!;
                        mapping.IncrementalCompareOperator = ">";
                        mapping.IncrementalStartValue = 0;
                    }
                }
                
                // Create column mappings
                foreach (var sourceCol in sourceColumns)
                {
                    // Try to find matching column in target (exact match or case insensitive)
                    var targetCol = targetColumns.Find(c => c.Name == sourceCol.Name) ?? 
                                   targetColumns.Find(c => string.Equals(c.Name, sourceCol.Name, StringComparison.OrdinalIgnoreCase));
                    
                    if (targetCol != null)
                    {
                        mapping.ColumnMappings.Add(new ColumnMapping
                        {
                            SourceColumn = sourceCol.Name!,
                            TargetColumn = targetCol.Name!,
                            DataType = targetCol.DataType!,
                            AllowNull = targetCol.IsNullable
                        });
                    }
                }
                
                // Add bulk copy options
                mapping.BulkCopyOptions = new BulkCopyOptions
                {
                    KeepIdentity = true,
                    KeepNulls = true,
                    TableLock = true,
                    Timeout = 600
                };
                
                return Ok(mapping);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error suggesting table mapping");
                return StatusCode(500, $"Error suggesting table mapping: {ex.Message}");
            }
        }

        private string? GetConnectionString(string connectionId)
        {
            // In a production environment, you would get this from a database
            // For now, we'll use hardcoded values based on ConnectionStrings section in config
            return connectionId.ToLowerInvariant() switch
            {
                "source" => _configuration.GetConnectionString("DataTransfer:Source"),
                "target" => _configuration.GetConnectionString("DataTransfer:Target"),
                // Attempt to parse as int for future DB lookup, for now, it won't resolve numeric IDs here
                _ => int.TryParse(connectionId, out _) ? null : _configuration.GetConnectionString(connectionId) 
            };
        }
        
        private async Task<List<ColumnInfo>> GetTableColumns(string connectionString, string tableName, string schema)
        {
            var columns = new List<ColumnInfo>();
            
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                
                string query = @"
                    SELECT 
                        c.COLUMN_NAME,
                        c.DATA_TYPE,
                        c.CHARACTER_MAXIMUM_LENGTH,
                        c.NUMERIC_PRECISION,
                        c.NUMERIC_SCALE,
                        c.IS_NULLABLE,
                        COLUMNPROPERTY(OBJECT_ID(c.TABLE_SCHEMA + '.' + c.TABLE_NAME), c.COLUMN_NAME, 'IsIdentity') as IS_IDENTITY,
                        (
                            SELECT COUNT(1) FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE k
                            WHERE k.TABLE_SCHEMA = c.TABLE_SCHEMA 
                              AND k.TABLE_NAME = c.TABLE_NAME 
                              AND k.COLUMN_NAME = c.COLUMN_NAME
                              AND EXISTS (
                                SELECT 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                                WHERE tc.CONSTRAINT_NAME = k.CONSTRAINT_NAME
                                  AND tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
                              )
                        ) as IS_PRIMARY_KEY
                    FROM 
                        INFORMATION_SCHEMA.COLUMNS c
                    WHERE 
                        c.TABLE_SCHEMA = @Schema AND c.TABLE_NAME = @TableName
                    ORDER BY 
                        c.ORDINAL_POSITION";
                
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Schema", schema);
                    command.Parameters.AddWithValue("@TableName", tableName);
                    
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var column = new ColumnInfo
                            {
                                Name = reader.GetString(0),
                                DataType = reader.GetString(1),
                                MaxLength = reader.IsDBNull(2) ? null : (int?)reader.GetInt32(2),
                                Precision = reader.IsDBNull(3) ? null : (int?)reader.GetInt32(3),
                                Scale = reader.IsDBNull(4) ? null : (int?)reader.GetInt32(4),
                                IsNullable = reader.GetString(5) == "YES",
                                IsIdentity = reader.GetInt32(6) == 1,
                                IsPrimaryKey = reader.GetInt32(7) > 0
                            };
                            
                            column.FullDataType = FormatFullDataType(column);
                            
                            columns.Add(column);
                        }
                    }
                }
            }
            
            return columns;
        }
        
        private string FormatFullDataType(ColumnInfo column)
        {
            if (column.DataType == null) return string.Empty;
            switch (column.DataType.ToLowerInvariant())
            {
                case "char":
                case "varchar":
                case "nchar":
                case "nvarchar":
                    if (column.MaxLength == -1)
                        return $"{column.DataType}(max)";
                    return $"{column.DataType}({column.MaxLength})";
                
                case "decimal":
                case "numeric":
                    return $"{column.DataType}({column.Precision}, {column.Scale})";
                
                default:
                    return column.DataType;
            }
        }

        public class TableInfo
        {
            public string? Name { get; set; } // Made Name nullable
            public string? Type { get; set; } // Made Type nullable
            public int ColumnCount { get; set; }
            public bool HasPrimaryKey { get; set; }
        }

        public class ColumnInfo
        {
            public string? Name { get; set; } // Made Name nullable
            public string? DataType { get; set; } // Made DataType nullable
            public string? FullDataType { get; set; } // Made FullDataType nullable
            public int? MaxLength { get; set; }
            public int? Precision { get; set; }
            public int? Scale { get; set; }
            public bool IsNullable { get; set; }
            public bool IsIdentity { get; set; }
            public bool IsPrimaryKey { get; set; }
        }
    }
}