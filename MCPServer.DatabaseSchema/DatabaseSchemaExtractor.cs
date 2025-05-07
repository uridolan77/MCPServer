using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCPServer.DatabaseSchema
{
    /// <summary>
    /// Extracts database schema information from SQL Server and generates a JSON representation
    /// </summary>
    public class DatabaseSchemaExtractor
    {
        private readonly string _connectionString;

        public DatabaseSchemaExtractor(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Extracts the database schema and generates a JSON representation
        /// </summary>
        /// <returns>JSON string representing the database schema</returns>
        public async Task<string> ExtractSchemaAsJsonAsync(string databaseName)
        {
            var schema = await ExtractSchemaAsync(databaseName);
            return JsonConvert.SerializeObject(schema, Formatting.Indented);
        }

        /// <summary>
        /// Extracts the database schema and returns it as a DatabaseMetaSchema object
        /// </summary>
        /// <returns>DatabaseMetaSchema object representing the database schema</returns>
        public async Task<DatabaseMetaSchema> ExtractSchemaAsync(string databaseName)
        {
            var schema = new DatabaseMetaSchema
            {
                Metadata = new SchemaMetadata
                {
                    Id = $"{databaseName.ToLower()}-schema-v1",
                    Name = $"{databaseName} Database Schema",
                    Description = $"Schema definition for the {databaseName} database",
                    Created = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    Modified = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                }
            };

            // Create entity definitions
            var databaseEntity = CreateDatabaseEntity(databaseName);
            var schemaEntity = CreateSchemaEntity();
            var tableEntity = CreateTableEntity();
            var columnEntity = CreateColumnEntity();
            var primaryKeyEntity = CreatePrimaryKeyEntity();
            var foreignKeyEntity = CreateForeignKeyEntity();
            var indexEntity = CreateIndexEntity();
            var viewEntity = CreateViewEntity();
            var storedProcedureEntity = CreateStoredProcedureEntity();
            var functionEntity = CreateFunctionEntity();
            var triggerEntity = CreateTriggerEntity();
                
            // Add entities to schema
            schema.Entities.Add(databaseEntity);
            schema.Entities.Add(schemaEntity);
            schema.Entities.Add(tableEntity);
            schema.Entities.Add(columnEntity);
            schema.Entities.Add(primaryKeyEntity);
            schema.Entities.Add(foreignKeyEntity);
            schema.Entities.Add(indexEntity);
            schema.Entities.Add(viewEntity);
            schema.Entities.Add(storedProcedureEntity);
            schema.Entities.Add(functionEntity);
            schema.Entities.Add(triggerEntity);

            // Add relationships to schema
            schema.Relationships.AddRange(CreateRelationships());

            // Extract schema values
            var databaseValues = await ExtractDatabaseInfoAsync(databaseName);
            databaseEntity.Values = new List<dynamic> { databaseValues };

            var schemaValues = await ExtractSchemasAsync(databaseName);
            schemaEntity.Values = schemaValues;

            var tableValues = await ExtractTablesAsync(databaseName);
            tableEntity.Values = tableValues;

            var columnValues = await ExtractColumnsAsync(databaseName);
            columnEntity.Values = columnValues;

            var primaryKeyValues = await ExtractPrimaryKeysAsync(databaseName);
            primaryKeyEntity.Values = primaryKeyValues;

            var foreignKeyValues = await ExtractForeignKeysAsync(databaseName);
            foreignKeyEntity.Values = foreignKeyValues;

            var indexValues = await ExtractIndexesAsync(databaseName);
            indexEntity.Values = indexValues;

            var viewValues = await ExtractViewsAsync(databaseName);
            viewEntity.Values = viewValues;

            var storedProcedureValues = await ExtractStoredProceduresAsync(databaseName);
            storedProcedureEntity.Values = storedProcedureValues;

            var functionValues = await ExtractFunctionsAsync(databaseName);
            functionEntity.Values = functionValues;

            var triggerValues = await ExtractTriggersAsync(databaseName);
            triggerEntity.Values = triggerValues;

            return schema;
        }

        #region Entity Creation Methods

        private EntityDefinition CreateDatabaseEntity(string databaseName)
        {
            return new EntityDefinition
            {
                Name = "Database",
                Description = "Represents a database instance",
                Properties = new List<PropertyDefinition>
                {
                    new PropertyDefinition { Name = "id", Type = "string", Description = "Unique identifier for the database", Required = true },
                    new PropertyDefinition { Name = "name", Type = "string", Description = "Name of the database", Required = true },
                    new PropertyDefinition { Name = "description", Type = "string", Description = "Description of the database purpose" },
                    new PropertyDefinition { Name = "vendor", Type = "string", Description = "Database vendor (SQL Server, Oracle, MySQL, etc.)" },
                    new PropertyDefinition { Name = "version", Type = "string", Description = "Database version" },
                    new PropertyDefinition { Name = "created", Type = "datetime", Description = "When the database was created" },
                    new PropertyDefinition { Name = "lastModified", Type = "datetime", Description = "When the database was last modified" },
                    new PropertyDefinition { Name = "collation", Type = "string", Description = "Default collation of the database" },
                    new PropertyDefinition { Name = "characterSet", Type = "string", Description = "Default character set of the database" }
                }
            };
        }

        private EntityDefinition CreateSchemaEntity()
        {
            return new EntityDefinition
            {
                Name = "Schema",
                Description = "Represents a schema within a database",
                Properties = new List<PropertyDefinition>
                {
                    new PropertyDefinition { Name = "id", Type = "string", Description = "Unique identifier for the schema", Required = true },
                    new PropertyDefinition { Name = "databaseId", Type = "string", Description = "Reference to the parent database", Required = true },
                    new PropertyDefinition { Name = "name", Type = "string", Description = "Name of the schema", Required = true },
                    new PropertyDefinition { Name = "description", Type = "string", Description = "Description of the schema purpose" },
                    new PropertyDefinition { Name = "owner", Type = "string", Description = "Owner of the schema" },
                    new PropertyDefinition { Name = "created", Type = "datetime", Description = "When the schema was created" },
                    new PropertyDefinition { Name = "lastModified", Type = "datetime", Description = "When the schema was last modified" }
                }
            };
        }

        private EntityDefinition CreateTableEntity()
        {
            return new EntityDefinition
            {
                Name = "Table",
                Description = "Represents a table in a database schema",
                Properties = new List<PropertyDefinition>
                {
                    new PropertyDefinition { Name = "id", Type = "string", Description = "Unique identifier for the table", Required = true },
                    new PropertyDefinition { Name = "schemaId", Type = "string", Description = "Reference to the parent schema", Required = true },
                    new PropertyDefinition { Name = "name", Type = "string", Description = "Name of the table", Required = true },
                    new PropertyDefinition { Name = "description", Type = "string", Description = "Description of the table purpose" },
                    new PropertyDefinition { Name = "type", Type = "string", Description = "Type of table (regular, temporary, view, etc.)", DefaultValue = "regular" },
                    new PropertyDefinition { Name = "created", Type = "datetime", Description = "When the table was created" },
                    new PropertyDefinition { Name = "lastModified", Type = "datetime", Description = "When the table was last modified" },
                    new PropertyDefinition { Name = "isSystem", Type = "boolean", Description = "Whether the table is a system table", DefaultValue = false },
                    new PropertyDefinition { Name = "estimatedRows", Type = "integer", Description = "Estimated number of rows in the table" }
                }
            };
        }

        private EntityDefinition CreateColumnEntity()
        {
            return new EntityDefinition
            {
                Name = "Column",
                Description = "Represents a column in a database table",
                Properties = new List<PropertyDefinition>
                {
                    new PropertyDefinition { Name = "id", Type = "string", Description = "Unique identifier for the column", Required = true },
                    new PropertyDefinition { Name = "tableId", Type = "string", Description = "Reference to the parent table", Required = true },
                    new PropertyDefinition { Name = "name", Type = "string", Description = "Name of the column", Required = true },
                    new PropertyDefinition { Name = "description", Type = "string", Description = "Description of the column" },
                    new PropertyDefinition { Name = "ordinalPosition", Type = "integer", Description = "Position of the column in the table", Required = true },
                    new PropertyDefinition { Name = "dataType", Type = "string", Description = "Data type of the column", Required = true },
                    new PropertyDefinition { Name = "precision", Type = "integer", Description = "Precision for numeric data types" },
                    new PropertyDefinition { Name = "scale", Type = "integer", Description = "Scale for numeric data types" },
                    new PropertyDefinition { Name = "maxLength", Type = "integer", Description = "Maximum length for string data types" },
                    new PropertyDefinition { Name = "isNullable", Type = "boolean", Description = "Whether the column can contain NULL values", DefaultValue = true },
                    new PropertyDefinition { Name = "defaultValue", Type = "string", Description = "Default value for the column" },
                    new PropertyDefinition { Name = "isIdentity", Type = "boolean", Description = "Whether the column is an identity/auto-increment column", DefaultValue = false },
                    new PropertyDefinition { Name = "identitySeed", Type = "integer", Description = "Seed value for identity columns" },
                    new PropertyDefinition { Name = "identityIncrement", Type = "integer", Description = "Increment value for identity columns" },
                    new PropertyDefinition { Name = "isPrimaryKey", Type = "boolean", Description = "Whether the column is part of the primary key", DefaultValue = false }
                }
            };
        }

        private EntityDefinition CreatePrimaryKeyEntity()
        {
            return new EntityDefinition
            {
                Name = "PrimaryKey",
                Description = "Represents a primary key constraint",
                Properties = new List<PropertyDefinition>
                {
                    new PropertyDefinition { Name = "id", Type = "string", Description = "Unique identifier for the primary key", Required = true },
                    new PropertyDefinition { Name = "tableId", Type = "string", Description = "Reference to the parent table", Required = true },
                    new PropertyDefinition { Name = "name", Type = "string", Description = "Name of the primary key constraint", Required = true },
                    new PropertyDefinition 
                    { 
                        Name = "columns", 
                        Type = "array", 
                        Description = "Columns that make up the primary key", 
                        Required = true,
                        Items = new PropertyItems
                        {
                            Type = "object",
                            Properties = new Dictionary<string, PropertyDefinition>
                            {
                                { "columnId", new PropertyDefinition { Type = "string", Description = "Reference to the column" } },
                                { "order", new PropertyDefinition { Type = "integer", Description = "Order of the column in the primary key" } },
                                { "direction", new PropertyDefinition 
                                    { 
                                        Type = "string", 
                                        Description = "Sort direction (ASC, DESC)", 
                                        Enum = new List<string> { "ASC", "DESC" },
                                        DefaultValue = "ASC"
                                    } 
                                }
                            }
                        }
                    },
                    new PropertyDefinition { Name = "isClustered", Type = "boolean", Description = "Whether the primary key is clustered", DefaultValue = true }
                }
            };
        }

        private EntityDefinition CreateForeignKeyEntity()
        {
            return new EntityDefinition
            {
                Name = "ForeignKey",
                Description = "Represents a foreign key constraint",
                Properties = new List<PropertyDefinition>
                {
                    new PropertyDefinition { Name = "id", Type = "string", Description = "Unique identifier for the foreign key", Required = true },
                    new PropertyDefinition { Name = "name", Type = "string", Description = "Name of the foreign key constraint", Required = true },
                    new PropertyDefinition { Name = "tableId", Type = "string", Description = "Reference to the child table", Required = true },
                    new PropertyDefinition { Name = "referencedTableId", Type = "string", Description = "Reference to the parent table", Required = true },
                    new PropertyDefinition 
                    { 
                        Name = "columnMappings", 
                        Type = "array", 
                        Description = "Mapping between child and parent columns", 
                        Required = true,
                        Items = new PropertyItems
                        {
                            Type = "object",
                            Properties = new Dictionary<string, PropertyDefinition>
                            {
                                { "columnId", new PropertyDefinition { Type = "string", Description = "Reference to the child column" } },
                                { "referencedColumnId", new PropertyDefinition { Type = "string", Description = "Reference to the parent column" } }
                            }
                        }
                    },
                    new PropertyDefinition 
                    { 
                        Name = "updateRule", 
                        Type = "string", 
                        Description = "Action to take on update",
                        Enum = new List<string> { "NO ACTION", "CASCADE", "SET NULL", "SET DEFAULT", "RESTRICT" },
                        DefaultValue = "NO ACTION"
                    },
                    new PropertyDefinition 
                    { 
                        Name = "deleteRule", 
                        Type = "string", 
                        Description = "Action to take on delete",
                        Enum = new List<string> { "NO ACTION", "CASCADE", "SET NULL", "SET DEFAULT", "RESTRICT" },
                        DefaultValue = "NO ACTION"
                    }
                }
            };
        }

        private EntityDefinition CreateIndexEntity()
        {
            return new EntityDefinition
            {
                Name = "Index",
                Description = "Represents an index on a table",
                Properties = new List<PropertyDefinition>
                {
                    new PropertyDefinition { Name = "id", Type = "string", Description = "Unique identifier for the index", Required = true },
                    new PropertyDefinition { Name = "tableId", Type = "string", Description = "Reference to the parent table", Required = true },
                    new PropertyDefinition { Name = "name", Type = "string", Description = "Name of the index", Required = true },
                    new PropertyDefinition { Name = "isUnique", Type = "boolean", Description = "Whether the index is unique", DefaultValue = false },
                    new PropertyDefinition { Name = "isClustered", Type = "boolean", Description = "Whether the index is clustered", DefaultValue = false },
                    new PropertyDefinition 
                    { 
                        Name = "columns", 
                        Type = "array", 
                        Description = "Columns that make up the index", 
                        Required = true,
                        Items = new PropertyItems
                        {
                            Type = "object",
                            Properties = new Dictionary<string, PropertyDefinition>
                            {
                                { "columnId", new PropertyDefinition { Type = "string", Description = "Reference to the column" } },
                                { "order", new PropertyDefinition { Type = "integer", Description = "Order of the column in the index" } },
                                { "direction", new PropertyDefinition 
                                    { 
                                        Type = "string", 
                                        Description = "Sort direction (ASC, DESC)", 
                                        Enum = new List<string> { "ASC", "DESC" },
                                        DefaultValue = "ASC"
                                    } 
                                }
                            }
                        }
                    }
                }
            };
        }

        private EntityDefinition CreateViewEntity()
        {
            return new EntityDefinition
            {
                Name = "View",
                Description = "Represents a view in a database schema",
                Properties = new List<PropertyDefinition>
                {
                    new PropertyDefinition { Name = "id", Type = "string", Description = "Unique identifier for the view", Required = true },
                    new PropertyDefinition { Name = "schemaId", Type = "string", Description = "Reference to the parent schema", Required = true },
                    new PropertyDefinition { Name = "name", Type = "string", Description = "Name of the view", Required = true },
                    new PropertyDefinition { Name = "description", Type = "string", Description = "Description of the view purpose" },
                    new PropertyDefinition { Name = "definition", Type = "string", Description = "SQL definition of the view", Required = true },
                    new PropertyDefinition { Name = "created", Type = "datetime", Description = "When the view was created" },
                    new PropertyDefinition { Name = "lastModified", Type = "datetime", Description = "When the view was last modified" }
                }
            };
        }

        private EntityDefinition CreateStoredProcedureEntity()
        {
            return new EntityDefinition
            {
                Name = "StoredProcedure",
                Description = "Represents a stored procedure in a database schema",
                Properties = new List<PropertyDefinition>
                {
                    new PropertyDefinition { Name = "id", Type = "string", Description = "Unique identifier for the stored procedure", Required = true },
                    new PropertyDefinition { Name = "schemaId", Type = "string", Description = "Reference to the parent schema", Required = true },
                    new PropertyDefinition { Name = "name", Type = "string", Description = "Name of the stored procedure", Required = true },
                    new PropertyDefinition { Name = "description", Type = "string", Description = "Description of the stored procedure purpose" },
                    new PropertyDefinition { Name = "definition", Type = "string", Description = "SQL definition of the stored procedure", Required = true },
                    new PropertyDefinition { Name = "created", Type = "datetime", Description = "When the stored procedure was created" },
                    new PropertyDefinition { Name = "lastModified", Type = "datetime", Description = "When the stored procedure was last modified" }
                }
            };
        }

        private EntityDefinition CreateFunctionEntity()
        {
            return new EntityDefinition
            {
                Name = "Function",
                Description = "Represents a function in a database schema",
                Properties = new List<PropertyDefinition>
                {
                    new PropertyDefinition { Name = "id", Type = "string", Description = "Unique identifier for the function", Required = true },
                    new PropertyDefinition { Name = "schemaId", Type = "string", Description = "Reference to the parent schema", Required = true },
                    new PropertyDefinition { Name = "name", Type = "string", Description = "Name of the function", Required = true },
                    new PropertyDefinition { Name = "description", Type = "string", Description = "Description of the function purpose" },
                    new PropertyDefinition { Name = "definition", Type = "string", Description = "SQL definition of the function", Required = true },
                    new PropertyDefinition { Name = "returnType", Type = "string", Description = "Return type of the function", Required = true },
                    new PropertyDefinition 
                    { 
                        Name = "functionType", 
                        Type = "string", 
                        Description = "Type of function (scalar, table-valued, aggregate)", 
                        Required = true,
                        Enum = new List<string> { "SCALAR", "TABLE", "AGGREGATE" }
                    },
                    new PropertyDefinition { Name = "created", Type = "datetime", Description = "When the function was created" },
                    new PropertyDefinition { Name = "lastModified", Type = "datetime", Description = "When the function was last modified" }
                }
            };
        }

        private EntityDefinition CreateTriggerEntity()
        {
            return new EntityDefinition
            {
                Name = "Trigger",
                Description = "Represents a trigger in a database schema",
                Properties = new List<PropertyDefinition>
                {
                    new PropertyDefinition { Name = "id", Type = "string", Description = "Unique identifier for the trigger", Required = true },
                    new PropertyDefinition { Name = "tableId", Type = "string", Description = "Reference to the parent table", Required = true },
                    new PropertyDefinition { Name = "name", Type = "string", Description = "Name of the trigger", Required = true },
                    new PropertyDefinition { Name = "description", Type = "string", Description = "Description of the trigger purpose" },
                    new PropertyDefinition { Name = "definition", Type = "string", Description = "SQL definition of the trigger", Required = true },
                    new PropertyDefinition 
                    { 
                        Name = "eventType", 
                        Type = "string", 
                        Description = "Event that activates the trigger (INSERT, UPDATE, DELETE)", 
                        Required = true,
                        Enum = new List<string> { "INSERT", "UPDATE", "DELETE", "INSERT,UPDATE", "INSERT,DELETE", "UPDATE,DELETE", "INSERT,UPDATE,DELETE" }
                    },
                    new PropertyDefinition 
                    { 
                        Name = "timing", 
                        Type = "string", 
                        Description = "When the trigger fires (BEFORE, AFTER, INSTEAD OF)", 
                        Required = true,
                        Enum = new List<string> { "BEFORE", "AFTER", "INSTEAD OF" }
                    },
                    new PropertyDefinition { Name = "created", Type = "datetime", Description = "When the trigger was created" },
                    new PropertyDefinition { Name = "lastModified", Type = "datetime", Description = "When the trigger was last modified" }
                }
            };
        }

        private List<Relationship> CreateRelationships()
        {
            return new List<Relationship>
            {
                new Relationship
                {
                    Name = "DatabaseToSchema",
                    Description = "Relationship between a database and its schemas",
                    Source = "Database",
                    Target = "Schema",
                    Cardinality = "one-to-many",
                    SourceProperty = "id",
                    TargetProperty = "databaseId"
                },
                new Relationship
                {
                    Name = "SchemaToTable",
                    Description = "Relationship between a schema and its tables",
                    Source = "Schema",
                    Target = "Table",
                    Cardinality = "one-to-many",
                    SourceProperty = "id",
                    TargetProperty = "schemaId"
                },
                new Relationship
                {
                    Name = "TableToColumn",
                    Description = "Relationship between a table and its columns",
                    Source = "Table",
                    Target = "Column",
                    Cardinality = "one-to-many",
                    SourceProperty = "id",
                    TargetProperty = "tableId"
                },
                new Relationship
                {
                    Name = "TableToPrimaryKey",
                    Description = "Relationship between a table and its primary key",
                    Source = "Table",
                    Target = "PrimaryKey",
                    Cardinality = "one-to-one",
                    SourceProperty = "id",
                    TargetProperty = "tableId"
                },
                new Relationship
                {
                    Name = "TableToForeignKey",
                    Description = "Relationship between a table and its foreign keys",
                    Source = "Table",
                    Target = "ForeignKey",
                    Cardinality = "one-to-many",
                    SourceProperty = "id",
                    TargetProperty = "tableId"
                },
                new Relationship
                {
                    Name = "TableToIndex",
                    Description = "Relationship between a table and its indexes",
                    Source = "Table",
                    Target = "Index",
                    Cardinality = "one-to-many",
                    SourceProperty = "id",
                    TargetProperty = "tableId"
                },
                new Relationship
                {
                    Name = "SchemaToView",
                    Description = "Relationship between a schema and its views",
                    Source = "Schema",
                    Target = "View",
                    Cardinality = "one-to-many",
                    SourceProperty = "id",
                    TargetProperty = "schemaId"
                },
                new Relationship
                {
                    Name = "SchemaToStoredProcedure",
                    Description = "Relationship between a schema and its stored procedures",
                    Source = "Schema",
                    Target = "StoredProcedure",
                    Cardinality = "one-to-many",
                    SourceProperty = "id",
                    TargetProperty = "schemaId"
                },
                new Relationship
                {
                    Name = "SchemaToFunction",
                    Description = "Relationship between a schema and its functions",
                    Source = "Schema",
                    Target = "Function",
                    Cardinality = "one-to-many",
                    SourceProperty = "id",
                    TargetProperty = "schemaId"
                },
                new Relationship
                {
                    Name = "TableToTrigger",
                    Description = "Relationship between a table and its triggers",
                    Source = "Table",
                    Target = "Trigger",
                    Cardinality = "one-to-many",
                    SourceProperty = "id",
                    TargetProperty = "tableId"
                },
                new Relationship
                {
                    Name = "ForeignKeyToReferencedTable",
                    Description = "Relationship between a foreign key and its referenced table",
                    Source = "ForeignKey",
                    Target = "Table",
                    Cardinality = "many-to-one",
                    SourceProperty = "referencedTableId",
                    TargetProperty = "id"
                }
            };
        }

        #endregion

        #region Schema Extraction Methods

        private async Task<dynamic> ExtractDatabaseInfoAsync(string databaseName)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Query to get database information
            var query = @"
                SELECT
                    DB_NAME() as name,
                    create_date as created,
                    compatibility_level as version,
                    collation_name as collation
                FROM sys.databases
                WHERE name = @DatabaseName";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@DatabaseName", databaseName);

            using var reader = await command.ExecuteReaderAsync();
            
            if (!await reader.ReadAsync())
            {
                throw new Exception($"Database '{databaseName}' not found");
            }

            return new
            {
                id = databaseName.ToLower(),
                name = reader["name"]?.ToString(),
                description = $"Database '{databaseName}'",
                vendor = "SQL Server",
                version = reader["version"]?.ToString(),
                created = reader["created"] != DBNull.Value ? ((DateTime)reader["created"]).ToString("yyyy-MM-ddTHH:mm:ssZ") : null,
                collation = reader["collation"]?.ToString(),
                characterSet = "UTF-16"  // SQL Server uses UTF-16 for Unicode data
            };
        }

        private async Task<List<dynamic>> ExtractSchemasAsync(string databaseName)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Query to get schema information
            var query = @"
                SELECT
                    s.schema_id as id,
                    s.name,
                    u.name as owner,
                    s.create_date as created,
                    s.modify_date as modified
                FROM sys.schemas s
                JOIN sys.database_principals u ON s.principal_id = u.principal_id
                ORDER BY s.name";

            using var command = new SqlCommand(query, connection);
            
            using var reader = await command.ExecuteReaderAsync();
            
            var schemas = new List<dynamic>();
            
            while (await reader.ReadAsync())
            {
                schemas.Add(new
                {
                    id = reader["name"]?.ToString()?.ToLower(),
                    databaseId = databaseName.ToLower(),
                    name = reader["name"]?.ToString(),
                    description = $"Schema '{reader["name"]}' in database '{databaseName}'",
                    owner = reader["owner"]?.ToString(),
                    created = reader["created"] != DBNull.Value ? ((DateTime)reader["created"]).ToString("yyyy-MM-ddTHH:mm:ssZ") : null,
                    lastModified = reader["modified"] != DBNull.Value ? ((DateTime)reader["modified"]).ToString("yyyy-MM-ddTHH:mm:ssZ") : null
                });
            }
            
            return schemas;
        }

        private async Task<List<dynamic>> ExtractTablesAsync(string databaseName)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Query to get table information
            var query = @"
                SELECT
                    t.object_id as id,
                    s.name as schema_name,
                    t.name,
                    CASE WHEN is_ms_shipped = 1 THEN 1 ELSE 0 END as is_system,
                    create_date as created,
                    modify_date as modified,
                    p.rows as estimated_rows
                FROM sys.tables t
                JOIN sys.schemas s ON t.schema_id = s.schema_id
                JOIN sys.indexes i ON t.object_id = i.object_id AND i.index_id <= 1
                JOIN sys.partitions p ON i.object_id = p.object_id AND i.index_id = p.index_id
                ORDER BY s.name, t.name";

            using var command = new SqlCommand(query, connection);
            
            using var reader = await command.ExecuteReaderAsync();
            
            var tables = new List<dynamic>();
            
            while (await reader.ReadAsync())
            {
                var schemaName = reader["schema_name"]?.ToString()?.ToLower();
                var tableName = reader["name"]?.ToString()?.ToLower();
                
                tables.Add(new
                {
                    id = $"{schemaName}.{tableName}",
                    schemaId = schemaName,
                    name = reader["name"]?.ToString(),
                    description = $"Table '{reader["schema_name"]}.{reader["name"]}' in database '{databaseName}'",
                    type = "regular",
                    created = reader["created"] != DBNull.Value ? ((DateTime)reader["created"]).ToString("yyyy-MM-ddTHH:mm:ssZ") : null,
                    lastModified = reader["modified"] != DBNull.Value ? ((DateTime)reader["modified"]).ToString("yyyy-MM-ddTHH:mm:ssZ") : null,
                    isSystem = (int)reader["is_system"] == 1,
                    estimatedRows = (long)reader["estimated_rows"]
                });
            }
            
            return tables;
        }

        private async Task<List<dynamic>> ExtractColumnsAsync(string databaseName)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Query to get column information
            var query = @"
                SELECT
                    c.object_id as table_id,
                    c.column_id as id,
                    s.name as schema_name,
                    t.name as table_name,
                    c.name,
                    c.column_id as ordinal_position,
                    ty.name as data_type,
                    c.max_length,
                    c.precision,
                    c.scale,
                    c.is_nullable,
                    c.is_identity,
                    OBJECT_DEFINITION(c.default_object_id) as default_value,
                    ic.seed_value as identity_seed,
                    ic.increment_value as identity_increment,
                    CASE WHEN pk.column_id IS NULL THEN 0 ELSE 1 END as is_primary_key
                FROM sys.columns c
                JOIN sys.tables t ON c.object_id = t.object_id
                JOIN sys.schemas s ON t.schema_id = s.schema_id
                JOIN sys.types ty ON c.user_type_id = ty.user_type_id
                LEFT JOIN sys.identity_columns ic ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                LEFT JOIN (
                    SELECT i.object_id, ic.column_id
                    FROM sys.indexes i
                    JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
                    WHERE i.is_primary_key = 1
                ) pk ON c.object_id = pk.object_id AND c.column_id = pk.column_id
                ORDER BY s.name, t.name, c.column_id";

            using var command = new SqlCommand(query, connection);
            
            using var reader = await command.ExecuteReaderAsync();
            
            var columns = new List<dynamic>();
            
            while (await reader.ReadAsync())
            {
                var schemaName = reader["schema_name"]?.ToString()?.ToLower();
                var tableName = reader["table_name"]?.ToString()?.ToLower();
                var columnName = reader["name"]?.ToString()?.ToLower();
                string tableId = $"{schemaName}.{tableName}";
                
                columns.Add(new
                {
                    id = $"{tableId}.{columnName}",
                    tableId = tableId,
                    name = reader["name"]?.ToString(),
                    description = $"Column '{reader["name"]}' in table '{reader["schema_name"]}.{reader["table_name"]}'",
                    ordinalPosition = (int)reader["ordinal_position"],
                    dataType = reader["data_type"]?.ToString(),
                    precision = reader["precision"] != DBNull.Value ? (int)reader["precision"] : (int?)null,
                    scale = reader["scale"] != DBNull.Value ? (int)reader["scale"] : (int?)null,
                    maxLength = reader["max_length"] != DBNull.Value ? (short)reader["max_length"] : (short?)null,
                    isNullable = (bool)reader["is_nullable"],
                    defaultValue = reader["default_value"] != DBNull.Value ? reader["default_value"]?.ToString() : null,
                    isIdentity = (bool)reader["is_identity"],
                    identitySeed = reader["identity_seed"] != DBNull.Value ? (decimal)reader["identity_seed"] : (decimal?)null,
                    identityIncrement = reader["identity_increment"] != DBNull.Value ? (decimal)reader["identity_increment"] : (decimal?)null,
                    isPrimaryKey = (int)reader["is_primary_key"] == 1
                });
            }
            
            return columns;
        }

        private async Task<List<dynamic>> ExtractPrimaryKeysAsync(string databaseName)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Query to get primary key information
            var query = @"
                SELECT
                    i.object_id as table_id,
                    i.name,
                    s.name as schema_name,
                    t.name as table_name,
                    i.is_clustered,
                    c.name as column_name,
                    ic.key_ordinal as column_ordinal,
                    ic.is_descending_key
                FROM sys.indexes i
                JOIN sys.tables t ON i.object_id = t.object_id
                JOIN sys.schemas s ON t.schema_id = s.schema_id
                JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
                JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
                WHERE i.is_primary_key = 1
                ORDER BY s.name, t.name, i.name, ic.key_ordinal";

            using var command = new SqlCommand(query, connection);
            
            using var reader = await command.ExecuteReaderAsync();
            
            // Group by primary key constraint
            var pkGroups = new Dictionary<string, dynamic>();
            
            while (await reader.ReadAsync())
            {
                var schemaName = reader["schema_name"]?.ToString()?.ToLower();
                var tableName = reader["table_name"]?.ToString()?.ToLower();
                string tableId = $"{schemaName}.{tableName}";
                string pkName = reader["name"]?.ToString() ?? "";
                string pkId = $"{tableId}.{pkName.ToLower()}";
                
                if (!pkGroups.ContainsKey(pkId))
                {
                    pkGroups[pkId] = new
                    {
                        id = pkId,
                        tableId = tableId,
                        name = pkName,
                        columns = new List<dynamic>(),
                        isClustered = (bool)reader["is_clustered"]
                    };
                }
                
                var columnName = reader["column_name"]?.ToString()?.ToLower();
                var columnId = $"{tableId}.{columnName}";
                var columnOrdinal = (int)reader["column_ordinal"];
                var isDescending = (bool)reader["is_descending_key"];
                
                ((List<dynamic>)pkGroups[pkId].columns).Add(new
                {
                    columnId = columnId,
                    order = columnOrdinal,
                    direction = isDescending ? "DESC" : "ASC"
                });
            }
            
            return pkGroups.Values.ToList();
        }

        private async Task<List<dynamic>> ExtractForeignKeysAsync(string databaseName)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Query to get foreign key information
            var query = @"
                SELECT
                    fk.object_id as id,
                    fk.name,
                    s1.name as schema_name,
                    t1.name as table_name,
                    s2.name as referenced_schema_name,
                    t2.name as referenced_table_name,
                    c1.name as column_name,
                    c2.name as referenced_column_name,
                    fk.update_referential_action as update_rule,
                    fk.delete_referential_action as delete_rule,
                    fkc.constraint_column_id as column_ordinal
                FROM sys.foreign_keys fk
                JOIN sys.tables t1 ON fk.parent_object_id = t1.object_id
                JOIN sys.schemas s1 ON t1.schema_id = s1.schema_id
                JOIN sys.tables t2 ON fk.referenced_object_id = t2.object_id
                JOIN sys.schemas s2 ON t2.schema_id = s2.schema_id
                JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
                JOIN sys.columns c1 ON fkc.parent_object_id = c1.object_id AND fkc.parent_column_id = c1.column_id
                JOIN sys.columns c2 ON fkc.referenced_object_id = c2.object_id AND fkc.referenced_column_id = c2.column_id
                ORDER BY s1.name, t1.name, fk.name, fkc.constraint_column_id";

            using var command = new SqlCommand(query, connection);
            
            using var reader = await command.ExecuteReaderAsync();
            
            // Helper method for referential action names
            string GetReferentialActionName(int actionId)
            {
                return actionId switch
                {
                    0 => "NO ACTION",
                    1 => "CASCADE",
                    2 => "SET NULL",
                    3 => "SET DEFAULT",
                    _ => "NO ACTION"
                };
            }
            
            // Group by foreign key constraint
            var fkGroups = new Dictionary<string, dynamic>();
            
            while (await reader.ReadAsync())
            {
                var schemaName = reader["schema_name"]?.ToString()?.ToLower();
                var tableName = reader["table_name"]?.ToString()?.ToLower();
                var refSchemaName = reader["referenced_schema_name"]?.ToString()?.ToLower();
                var refTableName = reader["referenced_table_name"]?.ToString()?.ToLower();
                
                string tableId = $"{schemaName}.{tableName}";
                string referencedTableId = $"{refSchemaName}.{refTableName}";
                string fkName = reader["name"]?.ToString() ?? "";
                string fkId = $"{tableId}.{fkName.ToLower()}";
                
                if (!fkGroups.ContainsKey(fkId))
                {
                    fkGroups[fkId] = new
                    {
                        id = fkId,
                        name = fkName,
                        tableId = tableId,
                        referencedTableId = referencedTableId,
                        columnMappings = new List<dynamic>(),
                        updateRule = GetReferentialActionName((int)reader["update_rule"]),
                        deleteRule = GetReferentialActionName((int)reader["delete_rule"])
                    };
                }
                
                var columnName = reader["column_name"]?.ToString()?.ToLower();
                var refColumnName = reader["referenced_column_name"]?.ToString()?.ToLower();
                
                var columnId = $"{tableId}.{columnName}";
                var referencedColumnId = $"{referencedTableId}.{refColumnName}";
                
                ((List<dynamic>)fkGroups[fkId].columnMappings).Add(new
                {
                    columnId = columnId,
                    referencedColumnId = referencedColumnId
                });
            }
            
            return fkGroups.Values.ToList();
        }

        private async Task<List<dynamic>> ExtractIndexesAsync(string databaseName)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Query to get index information
            var query = @"
                SELECT
                    i.object_id as table_id,
                    i.index_id as id,
                    i.name,
                    s.name as schema_name,
                    t.name as table_name,
                    i.is_unique,
                    i.is_primary_key,
                    i.is_unique_constraint,
                    i.is_disabled,
                    i.is_clustered,
                    c.name as column_name,
                    ic.key_ordinal as column_ordinal,
                    ic.is_descending_key,
                    ic.is_included_column
                FROM sys.indexes i
                JOIN sys.tables t ON i.object_id = t.object_id
                JOIN sys.schemas s ON t.schema_id = s.schema_id
                JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
                JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
                WHERE i.is_primary_key = 0 AND i.is_unique_constraint = 0
                    AND i.type > 0  -- Exclude heaps
                    AND i.is_hypothetical = 0  -- Exclude hypothetical indexes
                ORDER BY s.name, t.name, i.name, ic.key_ordinal";

            using var command = new SqlCommand(query, connection);
            
            using var reader = await command.ExecuteReaderAsync();
            
            // Group by index
            var indexGroups = new Dictionary<string, dynamic>();
            
            while (await reader.ReadAsync())
            {
                var schemaName = reader["schema_name"]?.ToString()?.ToLower();
                var tableName = reader["table_name"]?.ToString()?.ToLower();
                
                string tableId = $"{schemaName}.{tableName}";
                int indexId = (int)reader["id"];
                string indexName = reader["name"]?.ToString() ?? "";
                string indexIdStr = $"{tableId}.{indexName.ToLower()}";
                
                if (!indexGroups.ContainsKey(indexIdStr))
                {
                    indexGroups[indexIdStr] = new
                    {
                        id = indexIdStr,
                        tableId = tableId,
                        name = indexName,
                        isUnique = (bool)reader["is_unique"],
                        isClustered = (bool)reader["is_clustered"],
                        isDisabled = (bool)reader["is_disabled"],
                        columns = new List<dynamic>()
                    };
                }
                
                var columnName = reader["column_name"]?.ToString()?.ToLower();
                var columnId = $"{tableId}.{columnName}";
                var columnOrdinal = (int)reader["column_ordinal"];
                var isDescending = (bool)reader["is_descending_key"];
                var isIncluded = (bool)reader["is_included_column"];
                
                ((List<dynamic>)indexGroups[indexIdStr].columns).Add(new
                {
                    columnId = columnId,
                    order = columnOrdinal,
                    direction = isDescending ? "DESC" : "ASC",
                    isIncluded = isIncluded
                });
            }
            
            return indexGroups.Values.ToList();
        }

        private async Task<List<dynamic>> ExtractViewsAsync(string databaseName)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Query to get view information
            var query = @"
                SELECT
                    v.object_id as id,
                    s.name as schema_name,
                    v.name,
                    OBJECT_DEFINITION(v.object_id) as definition,
                    v.create_date as created,
                    v.modify_date as modified
                FROM sys.views v
                JOIN sys.schemas s ON v.schema_id = s.schema_id
                ORDER BY s.name, v.name";

            using var command = new SqlCommand(query, connection);
            
            using var reader = await command.ExecuteReaderAsync();
            
            var views = new List<dynamic>();
            
            while (await reader.ReadAsync())
            {
                var schemaName = reader["schema_name"]?.ToString()?.ToLower();
                var viewName = reader["name"]?.ToString()?.ToLower();
                
                views.Add(new
                {
                    id = $"{schemaName}.{viewName}",
                    schemaId = schemaName,
                    name = reader["name"]?.ToString(),
                    description = $"View '{reader["schema_name"]}.{reader["name"]}' in database '{databaseName}'",
                    definition = reader["definition"]?.ToString(),
                    created = reader["created"] != DBNull.Value ? ((DateTime)reader["created"]).ToString("yyyy-MM-ddTHH:mm:ssZ") : null,
                    lastModified = reader["modified"] != DBNull.Value ? ((DateTime)reader["modified"]).ToString("yyyy-MM-ddTHH:mm:ssZ") : null
                });
            }
            
            return views;
        }

        private async Task<List<dynamic>> ExtractStoredProceduresAsync(string databaseName)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Query to get stored procedure information
            var query = @"
                SELECT
                    p.object_id as id,
                    s.name as schema_name,
                    p.name,
                    OBJECT_DEFINITION(p.object_id) as definition,
                    p.create_date as created,
                    p.modify_date as modified
                FROM sys.procedures p
                JOIN sys.schemas s ON p.schema_id = s.schema_id
                ORDER BY s.name, p.name";

            using var command = new SqlCommand(query, connection);
            
            using var reader = await command.ExecuteReaderAsync();
            
            var procedures = new List<dynamic>();
            
            while (await reader.ReadAsync())
            {
                var schemaName = reader["schema_name"]?.ToString()?.ToLower();
                var procName = reader["name"]?.ToString()?.ToLower();
                
                procedures.Add(new
                {
                    id = $"{schemaName}.{procName}",
                    schemaId = schemaName,
                    name = reader["name"]?.ToString(),
                    description = $"Stored procedure '{reader["schema_name"]}.{reader["name"]}' in database '{databaseName}'",
                    definition = reader["definition"]?.ToString(),
                    created = reader["created"] != DBNull.Value ? ((DateTime)reader["created"]).ToString("yyyy-MM-ddTHH:mm:ssZ") : null,
                    lastModified = reader["modified"] != DBNull.Value ? ((DateTime)reader["modified"]).ToString("yyyy-MM-ddTHH:mm:ssZ") : null
                });
            }
            
            return procedures;
        }

        private async Task<List<dynamic>> ExtractFunctionsAsync(string databaseName)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Query to get function information
            var query = @"
                SELECT
                    f.object_id as id,
                    s.name as schema_name,
                    f.name,
                    OBJECT_DEFINITION(f.object_id) as definition,
                    f.create_date as created,
                    f.modify_date as modified,
                    CASE
                        WHEN f.type = 'FN' THEN 'SCALAR'
                        WHEN f.type = 'TF' THEN 'TABLE'
                        WHEN f.type = 'AF' THEN 'AGGREGATE'
                        ELSE f.type
                    END as function_type,
                    t.name as return_type
                FROM sys.objects f
                JOIN sys.schemas s ON f.schema_id = s.schema_id
                LEFT JOIN sys.return_types rt ON f.object_id = rt.object_id
                LEFT JOIN sys.types t ON rt.user_type_id = t.user_type_id
                WHERE f.type IN ('FN', 'IF', 'TF', 'AF')
                ORDER BY s.name, f.name";

            using var command = new SqlCommand(query, connection);
            
            using var reader = await command.ExecuteReaderAsync();
            
            var functions = new List<dynamic>();
            
            while (await reader.ReadAsync())
            {
                var schemaName = reader["schema_name"]?.ToString()?.ToLower();
                var funcName = reader["name"]?.ToString()?.ToLower();
                
                functions.Add(new
                {
                    id = $"{schemaName}.{funcName}",
                    schemaId = schemaName,
                    name = reader["name"]?.ToString(),
                    description = $"Function '{reader["schema_name"]}.{reader["name"]}' in database '{databaseName}'",
                    definition = reader["definition"]?.ToString(),
                    returnType = reader["return_type"] != DBNull.Value ? reader["return_type"]?.ToString() : "unknown",
                    functionType = reader["function_type"]?.ToString(),
                    created = reader["created"] != DBNull.Value ? ((DateTime)reader["created"]).ToString("yyyy-MM-ddTHH:mm:ssZ") : null,
                    lastModified = reader["modified"] != DBNull.Value ? ((DateTime)reader["modified"]).ToString("yyyy-MM-ddTHH:mm:ssZ") : null
                });
            }
            
            return functions;
        }

        private async Task<List<dynamic>> ExtractTriggersAsync(string databaseName)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Query to get trigger information
            var query = @"
                SELECT
                    tr.object_id as id,
                    s.name as schema_name,
                    t.name as table_name,
                    tr.name,
                    OBJECT_DEFINITION(tr.object_id) as definition,
                    tr.create_date as created,
                    tr.modify_date as modified,
                    CASE WHEN OBJECTPROPERTY(tr.object_id, 'ExecIsInsertTrigger') = 1 THEN 1 ELSE 0 END as is_insert,
                    CASE WHEN OBJECTPROPERTY(tr.object_id, 'ExecIsUpdateTrigger') = 1 THEN 1 ELSE 0 END as is_update,
                    CASE WHEN OBJECTPROPERTY(tr.object_id, 'ExecIsDeleteTrigger') = 1 THEN 1 ELSE 0 END as is_delete,
                    CASE 
                        WHEN OBJECTPROPERTY(tr.object_id, 'ExecIsAfterTrigger') = 1 THEN 'AFTER'
                        WHEN OBJECTPROPERTY(tr.object_id, 'ExecIsInsteadOfTrigger') = 1 THEN 'INSTEAD OF'
                        ELSE NULL
                    END as timing
                FROM sys.triggers tr
                JOIN sys.tables t ON tr.parent_id = t.object_id
                JOIN sys.schemas s ON t.schema_id = s.schema_id
                WHERE tr.is_disabled = 0
                ORDER BY s.name, t.name, tr.name";

            using var command = new SqlCommand(query, connection);
            
            using var reader = await command.ExecuteReaderAsync();
            
            var triggers = new List<dynamic>();
            
            while (await reader.ReadAsync())
            {
                var schemaName = reader["schema_name"]?.ToString()?.ToLower();
                var tableName = reader["table_name"]?.ToString()?.ToLower();
                var triggerName = reader["name"]?.ToString()?.ToLower();
                
                string tableId = $"{schemaName}.{tableName}";
                
                // Determine the event
                var isInsert = (int)reader["is_insert"] == 1;
                var isUpdate = (int)reader["is_update"] == 1;
                var isDelete = (int)reader["is_delete"] == 1;
                
                var eventList = new List<string>();
                if (isInsert) eventList.Add("INSERT");
                if (isUpdate) eventList.Add("UPDATE");
                if (isDelete) eventList.Add("DELETE");
                
                var eventString = string.Join(",", eventList);
                
                triggers.Add(new
                {
                    id = $"{tableId}.{triggerName}",
                    tableId = tableId,
                    name = reader["name"]?.ToString(),
                    description = $"Trigger '{reader["name"]}' on table '{reader["schema_name"]}.{reader["table_name"]}' in database '{databaseName}'",
                    definition = reader["definition"]?.ToString(),
                    eventType = eventString,
                    timing = reader["timing"]?.ToString(),
                    created = reader["created"] != DBNull.Value ? ((DateTime)reader["created"]).ToString("yyyy-MM-ddTHH:mm:ssZ") : null,
                    lastModified = reader["modified"] != DBNull.Value ? ((DateTime)reader["modified"]).ToString("yyyy-MM-ddTHH:mm:ssZ") : null
                });
            }
            
            return triggers;
        }

        #endregion
    }
}