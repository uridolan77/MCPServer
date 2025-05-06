namespace MCPServer.Core.Features.DataTransfer.Models
{
    public class ValidationResult
    {
        public string ValidationType { get; set; }
        public string TableName { get; set; }
        public bool Success { get; set; }
        public string Details { get; set; }
        public string ErrorMessage { get; set; }
    }
}