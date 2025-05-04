using System;

namespace MCPServer.Core.Exceptions
{
    /// <summary>
    /// Exception thrown when an authentication error occurs with an LLM provider
    /// </summary>
    public class LlmProviderAuthException : Exception
    {
        public string ProviderName { get; }
        public string ErrorDetails { get; }

        public LlmProviderAuthException(string providerName, string message, string errorDetails) 
            : base(message)
        {
            ProviderName = providerName;
            ErrorDetails = errorDetails;
        }

        public LlmProviderAuthException(string providerName, string message, string errorDetails, Exception innerException) 
            : base(message, innerException)
        {
            ProviderName = providerName;
            ErrorDetails = errorDetails;
        }
    }
}