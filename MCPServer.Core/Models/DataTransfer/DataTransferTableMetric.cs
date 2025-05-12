using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MCPServer.Core.Models.DataTransfer
{
    [Table("DataTransferTableMetrics")]
    public class DataTransferTableMetric
    {
        [Key]
        public int MetricId { get; set; }
        
        public int RunId { get; set; }
        
        public int MappingId { get; set; }
        
        [StringLength(100)]
        public string SchemaName { get; set; }
        
        [StringLength(100)]
        public string TableName { get; set; }
        
        public DateTime StartTime { get; set; }
        
        public DateTime? EndTime { get; set; }
        
        [StringLength(20)]
        public string Status { get; set; } = "Running"; // Running, Completed, Failed, Cancelled
        
        public long RowsProcessed { get; set; }
        
        public long ElapsedMs { get; set; }
        
        public double RowsPerSecond { get; set; }
        
        [StringLength(500)]
        public string ErrorMessage { get; set; }
        
        public DateTime? LastProcessedTimestamp { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        [ForeignKey("RunId")]
        public virtual DataTransferRun Run { get; set; }
    }
}