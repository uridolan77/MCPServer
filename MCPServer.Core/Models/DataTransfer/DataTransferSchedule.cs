using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MCPServer.Core.Models.DataTransfer
{
    [Table("DataTransferSchedule")]
    public class DataTransferSchedule
    {
        public int ScheduleId { get; set; }
        
        public int ConfigurationId { get; set; }
        
        public string ScheduleType { get; set; }
        
        public TimeSpan? StartTime { get; set; }
        
        public int? Frequency { get; set; }
        
        public string FrequencyUnit { get; set; }
        
        public string WeekDays { get; set; }
        
        public string MonthDays { get; set; }
        
        public bool IsActive { get; set; }
        
        public DateTime? LastRunTime { get; set; }
        
        public DateTime? NextRunTime { get; set; }
        
        public string CreatedBy { get; set; }
        
        public DateTime CreatedOn { get; set; }
        
        public string LastModifiedBy { get; set; }
        
        public DateTime? LastModifiedOn { get; set; }
        
        // Navigation property
        public DataTransferConfiguration Configuration { get; set; }
    }
}