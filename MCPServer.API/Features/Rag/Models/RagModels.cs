namespace MCPServer.API.Features.Rag.Models
{
    /// <summary>
    /// Request model for generating answers
    /// </summary>
    public class AnswerRequest
    {
        public string Question { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response model for generated answers
    /// </summary>
    public class AnswerResponse
    {
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
    }
}
