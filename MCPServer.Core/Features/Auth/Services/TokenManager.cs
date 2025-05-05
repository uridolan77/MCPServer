using System;
using System.Collections.Generic;
using System.Linq;
using MCPServer.Core.Config;
using MCPServer.Core.Models;
using MCPServer.Core.Features.Auth.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TiktokenSharp;

namespace MCPServer.Core.Features.Auth.Services
{
    public class TokenManager : ITokenManager
    {
        private readonly ILogger<TokenManager> _logger;
        private readonly TokenSettings _tokenSettings;
        private readonly TikToken _tikToken;

        public TokenManager(
            IOptions<TokenSettings> tokenSettings,
            ILogger<TokenManager> logger)
        {
            _logger = logger;
            _tokenSettings = tokenSettings.Value;

            // Initialize the tokenizer for the GPT models (cl100k_base is used by gpt-3.5-turbo and gpt-4)
            _tikToken = TikToken.EncodingForModel("gpt-3.5-turbo");
        }

        public int CountTokens(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            // Use TiktokenSharp for accurate token counting
            return _tikToken.Encode(text).Count;
        }

        public int CountMessageTokens(Message message)
        {
            // Count tokens in the message content
            int contentTokens = CountTokens(message.Content);

            // Add overhead for role (each message follows format: <im_start>{role}\n{content}<im_end>\n)
            // Approximately 4 tokens per message for the formatting
            return contentTokens + 4;
        }

        public int CountContextTokens(SessionContext context)
        {
            // Sum up tokens from all messages
            int messageTokens = context.Messages.Sum(m => m.TokenCount > 0 ? m.TokenCount : CountMessageTokens(m));

            // Add overhead for the conversation format (~10 tokens)
            return messageTokens + 10;
        }

        public SessionContext TrimContextToFitTokenLimit(SessionContext context, int maxTokens)
        {
            // If context is already within limits, return as is
            if (CountContextTokens(context) <= maxTokens)
                return context;

            var trimmedContext = new SessionContext
            {
                SessionId = context.SessionId,
                Metadata = context.Metadata,
                CreatedAt = context.CreatedAt,
                LastUpdatedAt = context.LastUpdatedAt
            };

            // Always keep system message if present
            var systemMessage = context.Messages.FirstOrDefault(m => m.Role.Equals("system", StringComparison.OrdinalIgnoreCase));
            if (systemMessage != null)
            {
                trimmedContext.Messages.Add(systemMessage);
            }

            // Add most recent messages until we hit the token limit
            // Start from the most recent and work backwards
            for (int i = context.Messages.Count - 1; i >= 0; i--)
            {
                var message = context.Messages[i];

                // Skip system message as we've already added it
                if (message.Role.Equals("system", StringComparison.OrdinalIgnoreCase))
                    continue;

                // Calculate tokens with this message added
                var potentialMessages = new List<Message>(trimmedContext.Messages) { message };
                var potentialContext = new SessionContext
                {
                    Messages = potentialMessages
                };

                int potentialTokens = CountContextTokens(potentialContext);

                // If adding this message would exceed the limit, stop
                if (potentialTokens > maxTokens - _tokenSettings.ReservedTokens)
                    break;

                // Otherwise, add the message to our trimmed context
                trimmedContext.Messages.Insert(0, message);
            }

            _logger.LogInformation(
                "Trimmed context from {OriginalMessages} messages to {TrimmedMessages} messages to fit token limit",
                context.Messages.Count, trimmedContext.Messages.Count);

            return trimmedContext;
        }

        public List<LlmMessage> ConvertToLlmMessages(List<Message> messages, string newUserInput)
        {
            var llmMessages = new List<LlmMessage>();

            // Add existing messages
            foreach (var message in messages)
            {
                llmMessages.Add(new LlmMessage
                {
                    Role = message.Role,
                    Content = message.Content
                });
            }

            // Add new user input
            llmMessages.Add(new LlmMessage
            {
                Role = "user",
                Content = newUserInput
            });

            return llmMessages;
        }
    }
}

