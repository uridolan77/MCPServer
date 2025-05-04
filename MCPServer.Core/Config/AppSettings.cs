namespace MCPServer.Core.Config
{
    public class LlmSettings
    {
        public string Provider { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public double Temperature { get; set; } = 0.7;
        public int MaxTokens { get; set; } = 2000;
    }

    public class RedisSettings
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string InstanceName { get; set; } = string.Empty;
        public int SessionExpiryMinutes { get; set; } = 60;
    }

    public class TokenSettings
    {
        public int MaxTokensPerMessage { get; set; } = 4000;
        public int MaxContextTokens { get; set; } = 16000;
        public int ReservedTokens { get; set; } = 1000;
    }

    public class AuthSettings
    {
        public string Secret { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int AccessTokenExpirationMinutes { get; set; } = 60;
        public int RefreshTokenExpirationDays { get; set; } = 7;
    }

    public class AppSettings
    {
        public LlmSettings Llm { get; set; } = new LlmSettings();
        public RedisSettings Redis { get; set; } = new RedisSettings();
        public TokenSettings Token { get; set; } = new TokenSettings();
        public AuthSettings Auth { get; set; } = new AuthSettings();
    }
}
