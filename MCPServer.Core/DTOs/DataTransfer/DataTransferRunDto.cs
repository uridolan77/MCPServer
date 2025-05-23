// filepath: c:\dev\MCPServer\MCPServer.Core\DTOs\DataTransfer\DataTransferRunDto.cs
using System;
using System.Collections.Generic;

namespace MCPServer.Core.DTOs.DataTransfer
{
    public class DataTransferRunDto
    {
        public int RunId { get; set; }
        public int ConfigurationId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Status { get; set; }
        public long TotalRowsProcessed { get; set; }
        public long ElapsedMs { get; set; }
        public double AverageRowsPerSecond { get; set; }
        public int TotalTablesProcessed { get; set; }
        public int SuccessfulTablesCount { get; set; }
        public int FailedTablesCount { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class DataTransferRunDetailDto : DataTransferRunDto
    {
        public IEnumerable<DataTransferTableMetricDto> TableMetrics { get; set; }
    }
}
