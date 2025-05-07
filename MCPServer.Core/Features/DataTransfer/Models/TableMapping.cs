using System.Collections.Generic;

namespace MCPServer.Core.Features.DataTransfer.Models
{
    public class TableMapping
    {
        public string SourceSchema { get; set; } = "dbo";
        public string SourceTable { get; set; }
        public string TargetSchema { get; set; } = "dbo";
        public string TargetTable { get; set; }
        public bool Enabled { get; set; } = true;
        public bool FailOnError { get; set; } = true;
        public string IncrementalType { get; set; } = "None"; // None, DateTime, Int, BigInt
        public string IncrementalColumn { get; set; }
        public string IncrementalCompareOperator { get; set; } = ">";
        public object IncrementalStartValue { get; set; }
        public string CustomWhere { get; set; }
        public string OrderBy { get; set; }
        public int TopN { get; set; }
        public BulkCopyOptions BulkCopyOptions { get; set; }

        // Allow ColumnMappings to be set programmatically
        public List<ColumnMapping> ColumnMappings { get; set; } = new List<ColumnMapping>();

        // Add a property for additional bulk copy options that would be used with identical mapping
        public BulkCopyOptions AutoBulkCopyOptions { get; set; } = new BulkCopyOptions
        {
            KeepIdentity = true,
            KeepNulls = true,
            TableLock = false,
            Timeout = 600
        };
    }

    public class ColumnMapping
    {
        public string SourceColumn { get; set; }
        public string TargetColumn { get; set; }
        public string DataType { get; set; }
        public bool AllowNull { get; set; } = true;
        public object DefaultValue { get; set; }
        public string Transformation { get; set; }
        public string TransformationFormat { get; set; }
    }

    public class BulkCopyOptions
    {
        public bool CheckConstraints { get; set; }
        public bool KeepIdentity { get; set; }
        public bool KeepNulls { get; set; }
        public bool TableLock { get; set; }
        public bool FireTriggers { get; set; }
        public int? Timeout { get; set; }
    }
}