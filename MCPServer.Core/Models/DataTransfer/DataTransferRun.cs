using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MCPServer.Core.Models.DataTransfer
{
    [Table("DataTransferRuns")]
    public class DataTransferRun
    {
        public int RunId { get; set; }
        
        public int ConfigurationId { get; set; }
        
        public DateTime StartTime { get; set; }
        
        public DateTime? EndTime { get; set; }
        
        public string Status { get; set; }
        
        public int TotalTablesProcessed { get; set; }
        
        public int SuccessfulTablesCount { get; set; }
        
        public int FailedTablesCount { get; set; }
        
        public int TotalRowsProcessed { get; set; }
        
        public long ElapsedMs { get; set; }
        
        public double AverageRowsPerSecond { get; set; }
        
        public string TriggeredBy { get; set; }
        
        // Navigation property
        public DataTransferConfiguration Configuration { get; set; }
    }
}