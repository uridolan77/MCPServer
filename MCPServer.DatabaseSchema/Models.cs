using System.Collections.Generic;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace MCPServer.DatabaseSchema;

/// <summary>
/// Represents a database schema metadata structure.
/// </summary>
public class DatabaseMetaSchema
{
    public DatabaseMetaSchema()
    {
        Entities = new List<EntityDefinition>();
        Relationships = new List<Relationship>();
    }

    /// <summary>
    /// Metadata about the schema document itself
    /// </summary>
    public SchemaMetadata Metadata { get; set; }

    /// <summary>
    /// Definitions of entity types in the schema
    /// </summary>
    public List<EntityDefinition> Entities { get; set; }

    /// <summary>
    /// Definitions of relationships between entity types
    /// </summary>
    public List<Relationship> Relationships { get; set; }
}

/// <summary>
/// Metadata about the schema document itself
/// </summary>
public class SchemaMetadata
{
    /// <summary>
    /// Unique identifier for the schema
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// User-friendly name for the schema
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Description of the schema and its purpose
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// When the schema was created
    /// </summary>
    public string Created { get; set; }

    /// <summary>
    /// When the schema was last modified
    /// </summary>
    public string Modified { get; set; }
}

/// <summary>
/// Definition of an entity type in the schema
/// </summary>
public class EntityDefinition
{
    public EntityDefinition()
    {
        Properties = new List<PropertyDefinition>();
        Values = new List<dynamic>();
    }

    /// <summary>
    /// Name of the entity type
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Description of the entity type
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Properties that define the structure of the entity
    /// </summary>
    public List<PropertyDefinition> Properties { get; set; }

    /// <summary>
    /// Values of the entity instances
    /// </summary>
    public List<dynamic> Values { get; set; }
}

/// <summary>
/// Definition of a property in an entity type
/// </summary>
public class PropertyDefinition
{
    /// <summary>
    /// Name of the property
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Data type of the property
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// Description of the property
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Whether the property is required
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// Default value for the property
    /// </summary>
    public object DefaultValue { get; set; }

    /// <summary>
    /// For array types, defines the items in the array
    /// </summary>
    public PropertyItems Items { get; set; }

    /// <summary>
    /// For enum types, defines the possible values
    /// </summary>
    public List<string> Enum { get; set; }
}

/// <summary>
/// For array types, defines the items in the array
/// </summary>
public class PropertyItems
{
    /// <summary>
    /// Type of the items in the array
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// For object types, defines the properties of the object
    /// </summary>
    public Dictionary<string, PropertyDefinition> Properties { get; set; }
}

/// <summary>
/// Definition of a relationship between entity types
/// </summary>
public class Relationship
{
    /// <summary>
    /// Name of the relationship
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Description of the relationship
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Source entity type
    /// </summary>
    public string Source { get; set; }

    /// <summary>
    /// Target entity type
    /// </summary>
    public string Target { get; set; }

    /// <summary>
    /// Cardinality of the relationship (one-to-one, one-to-many, many-to-many)
    /// </summary>
    public string Cardinality { get; set; }

    /// <summary>
    /// Property in the source entity that links to the target
    /// </summary>
    public string SourceProperty { get; set; }

    /// <summary>
    /// Property in the target entity that links to the source
    /// </summary>
    public string TargetProperty { get; set; }
}

/// <summary>
/// Configuration options for the database schema extractor
/// </summary>
public class SchemaExtractionOptions
{
    /// <summary>
    /// Whether to extract view definitions
    /// </summary>
    public bool IncludeViewDefinitions { get; set; } = true;

    /// <summary>
    /// Whether to extract stored procedure definitions
    /// </summary>
    public bool IncludeStoredProcedureDefinitions { get; set; } = true;

    /// <summary>
    /// Whether to extract function definitions
    /// </summary>
    public bool IncludeFunctionDefinitions { get; set; } = true;

    /// <summary>
    /// Whether to extract trigger definitions
    /// </summary>
    public bool IncludeTriggerDefinitions { get; set; } = true;

    /// <summary>
    /// Whether to extract system objects
    /// </summary>
    public bool IncludeSystemObjects { get; set; } = false;

    /// <summary>
    /// Maximum length of object definitions to extract
    /// </summary>
    public int MaxDefinitionLength { get; set; } = 10000;
}

/// <summary>
/// Represents a SQL data type with its properties
/// </summary>
public class SqlDataType
{
    /// <summary>
    /// Name of the data type
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// SQL Server specific type ID
    /// </summary>
    public int TypeId { get; set; }

    /// <summary>
    /// Maximum length of the data type
    /// </summary>
    public int? MaxLength { get; set; }

    /// <summary>
    /// Precision of the data type
    /// </summary>
    public int? Precision { get; set; }

    /// <summary>
    /// Scale of the data type
    /// </summary>
    public int? Scale { get; set; }

    /// <summary>
    /// Whether the data type is a user-defined type
    /// </summary>
    public bool IsUserDefined { get; set; }

    /// <summary>
    /// SQL data type category
    /// </summary>
    public SqlDataTypeCategory Category { get; set; }
}

/// <summary>
/// Categories of SQL data types
/// </summary>
public enum SqlDataTypeCategory
{
    /// <summary>
    /// Exact numeric types (int, bigint, smallint, tinyint, bit, decimal, numeric, money, smallmoney)
    /// </summary>
    ExactNumeric,

    /// <summary>
    /// Approximate numeric types (float, real)
    /// </summary>
    ApproximateNumeric,

    /// <summary>
    /// Date and time types (date, time, datetime, datetime2, datetimeoffset, smalldatetime)
    /// </summary>
    DateTime,

    /// <summary>
    /// Character string types (char, varchar, text)
    /// </summary>
    CharacterString,

    /// <summary>
    /// Unicode character string types (nchar, nvarchar, ntext)
    /// </summary>
    UnicodeCharacterString,

    /// <summary>
    /// Binary types (binary, varbinary, image)
    /// </summary>
    Binary,

    /// <summary>
    /// Other types (cursor, rowversion, hierarchyid, uniqueidentifier, sql_variant, xml, table, spatial types)
    /// </summary>
    Other
}

/// <summary>
/// Helper class for mapping SQL Server data types to .NET types
/// </summary>
public static class SqlTypeMapper
{
    /// <summary>
    /// Maps a SQL Server data type to a .NET type
    /// </summary>
    /// <param name="sqlTypeName">SQL Server data type name</param>
    /// <returns>Corresponding .NET type name</returns>
    public static string MapToNetType(string sqlTypeName)
    {
        return sqlTypeName.ToLower() switch
        {
            "bit" => "bool",
            "tinyint" => "byte",
            "smallint" => "short",
            "int" => "int",
            "bigint" => "long",
            "decimal" or "numeric" or "money" or "smallmoney" => "decimal",
            "float" => "double",
            "real" => "float",
            "date" or "datetime" or "datetime2" or "smalldatetime" => "DateTime",
            "datetimeoffset" => "DateTimeOffset",
            "time" => "TimeSpan",
            "char" or "varchar" or "text" => "string",
            "nchar" or "nvarchar" or "ntext" => "string",
            "binary" or "varbinary" or "image" => "byte[]",
            "uniqueidentifier" => "Guid",
            "xml" => "string",
            "sql_variant" => "object",
            _ => "object"
        };
    }

    /// <summary>
    /// Gets the category of a SQL Server data type
    /// </summary>
    /// <param name="sqlTypeName">SQL Server data type name</param>
    /// <returns>Data type category</returns>
    public static SqlDataTypeCategory GetCategory(string sqlTypeName)
    {
        return sqlTypeName.ToLower() switch
        {
            "bit" or "tinyint" or "smallint" or "int" or "bigint" or "decimal" or "numeric" or "money" or "smallmoney" => SqlDataTypeCategory.ExactNumeric,
            "float" or "real" => SqlDataTypeCategory.ApproximateNumeric,
            "date" or "time" or "datetime" or "datetime2" or "datetimeoffset" or "smalldatetime" => SqlDataTypeCategory.DateTime,
            "char" or "varchar" or "text" => SqlDataTypeCategory.CharacterString,
            "nchar" or "nvarchar" or "ntext" => SqlDataTypeCategory.UnicodeCharacterString,
            "binary" or "varbinary" or "image" => SqlDataTypeCategory.Binary,
            _ => SqlDataTypeCategory.Other
        };
    }
}

/// <summary>
/// Represents a parameter in a stored procedure or function
/// </summary>
public class DatabaseParameterInfo
{
    /// <summary>
    /// Name of the parameter
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Data type of the parameter
    /// </summary>
    public string DataType { get; set; }

    /// <summary>
    /// Maximum length of the parameter
    /// </summary>
    public int? MaxLength { get; set; }

    /// <summary>
    /// Precision of the parameter
    /// </summary>
    public int? Precision { get; set; }

    /// <summary>
    /// Scale of the parameter
    /// </summary>
    public int? Scale { get; set; }

    /// <summary>
    /// Whether the parameter is an output parameter
    /// </summary>
    public bool IsOutput { get; set; }

    /// <summary>
    /// Default value of the parameter
    /// </summary>
    public string DefaultValue { get; set; }

    /// <summary>
    /// Order of the parameter
    /// </summary>
    public int ParameterOrder { get; set; }
}

/// <summary>
/// Contains information about a SQL Server dependency
/// </summary>
public class DatabaseDependency
{
    /// <summary>
    /// Name of the dependency
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Type of the dependency (e.g., Table, View, Procedure)
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// Schema of the dependency
    /// </summary>
    public string Schema { get; set; }

    /// <summary>
    /// Whether the dependency is a referenced object (if false, it's a referencing object)
    /// </summary>
    public bool IsReferenced { get; set; }
}