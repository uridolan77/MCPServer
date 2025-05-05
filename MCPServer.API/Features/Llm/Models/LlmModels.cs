using System;
using System.Collections.Generic;
using MCPServer.Core.Models;

namespace MCPServer.API.Features.Llm.Models
{
    /// <summary>
    /// Request model for context-based LLM requests
    /// </summary>
    public class ContextRequest
    {
        public string SessionId { get; set; } = string.Empty;
        public string UserInput { get; set; } = string.Empty;
        public List<Message> Messages { get; set; } = new List<Message>();
    }

    /// <summary>
    /// Request model for MCP operations
    /// </summary>
    public class McpRequest
    {
        /// <summary>
        /// The session ID
        /// </summary>
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// The user's input
        /// </summary>
        public string UserInput { get; set; } = string.Empty;

        /// <summary>
        /// Additional metadata
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// Response model for MCP operations
    /// </summary>
    public class McpResponse
    {
        /// <summary>
        /// The session ID
        /// </summary>
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// The output from the LLM
        /// </summary>
        public string Output { get; set; } = string.Empty;

        /// <summary>
        /// Whether the response is complete
        /// </summary>
        public bool IsComplete { get; set; }

        /// <summary>
        /// The timestamp of the response
        /// </summary>
        public DateTime Timestamp { get; set; }
    }
}
