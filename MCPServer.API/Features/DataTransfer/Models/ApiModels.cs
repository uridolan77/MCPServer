using System.Collections.Generic;

namespace MCPServer.API.Features.DataTransfer.Models
{
    public class MigrationRequest
    {
        public List<string> Tables { get; set; } = new List<string>();
        public bool DryRun { get; set; } = false;
        public bool Validate { get; set; } = true;
    }

    public class MigrationResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<TableMigrationResult> Results { get; set; } = new List<TableMigrationResult>();
        public List<ValidationMessage> ValidationMessages { get; set; } = new List<ValidationMessage>();
    }

    public class TableMigrationResult
    {
        public string TableName { get; set; }
        public bool Success { get; set; }
        public int RowsProcessed { get; set; }
        public string ElapsedTime { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class ValidationMessage
    {
        public string TableName { get; set; }
        public string ValidationType { get; set; }
        public bool Success { get; set; }
        public string Details { get; set; }
        public string ErrorMessage { get; set; }
    }
}