using System;
using System.Collections.Generic;

namespace MCPServer.API.DTOs.DataTransfer
{
    public class RunDetailsDto
    {
        public int RunId { get; set; }
        public int ConfigurationId { get; set; }
        public string ConfigurationName { get; set; }
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
        public List<TableMetricDto> TableMetrics { get; set; } = new List<TableMetricDto>();
    }

    public class TableMetricDto
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