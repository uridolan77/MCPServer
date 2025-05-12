using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MCPServer.Core.Models.DataTransfer
{
    [Table("DataTransferLogs")]
    public class DataTransferLog
    {
        public int LogId { get; set; }
        
        public int RunId { get; set; }
        
        public int? MappingId { get; set; }
        
        public DateTime LogTime { get; set; }
        
        public string LogLevel { get; set; }
        
        public string Message { get; set; }
        
        public string Exception { get; set; }
        
        // Navigation property
        public DataTransferRun Run { get; set; }
    }
}