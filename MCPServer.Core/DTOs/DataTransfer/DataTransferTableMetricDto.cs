// filepath: c:\dev\MCPServer\MCPServer.Core\DTOs\DataTransfer\DataTransferTableMetricDto.cs
using System;

namespace MCPServer.Core.DTOs.DataTransfer
{
    public class DataTransferTableMetricDto
    {
        public int MetricId { get; set; }
        public int RunId { get; set; }
        public int MappingId { get; set; }
        public string SchemaName { get; set; }
        public string TableName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Status { get; set; }
        public long RowsProcessed { get; set; }
        public long ElapsedMs { get; set; }
        public double RowsPerSecond { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime? LastProcessedTimestamp { get; set; }
    }
}