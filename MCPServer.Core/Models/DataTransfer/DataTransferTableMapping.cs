using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MCPServer.Core.Models.DataTransfer
{
    [Table("DataTransferTableMappings")]
    public class DataTransferTableMapping
    {
        public int MappingId { get; set; }
        
        public int ConfigurationId { get; set; }
        
        public string SchemaName { get; set; }
        
        public string TableName { get; set; }
        
        public string TimestampColumnName { get; set; }
        
        public string OrderByColumn { get; set; }
        
        public string CustomWhereClause { get; set; }
        
        public bool IsActive { get; set; }
        
        public int Priority { get; set; }
        
        // Navigation property
        public DataTransferConfiguration Configuration { get; set; }
    }
}