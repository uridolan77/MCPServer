using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MCPServer.Core.Models.DataTransfer
{
    [Table("DataTransferConfigurations")]
    public class DataTransferConfiguration
    {
        public int ConfigurationId { get; set; }
        
        public string ConfigurationName { get; set; }
        
        public string Description { get; set; }
        
        public int SourceConnectionId { get; set; }
        
        public int DestinationConnectionId { get; set; }
        
        public int BatchSize { get; set; }
        
        public int ReportingFrequency { get; set; }
        
        public bool IsActive { get; set; }
        
        public string CreatedBy { get; set; }
        
        public DateTime CreatedOn { get; set; }
        
        public string LastModifiedBy { get; set; }
        
        public DateTime? LastModifiedOn { get; set; }
        
        // Navigation properties
        public DataTransferConnection SourceConnection { get; set; }
        public DataTransferConnection DestinationConnection { get; set; }
    }
}