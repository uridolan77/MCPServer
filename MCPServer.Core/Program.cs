using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MCPServer;
using MCPServer.Core.Config;
using MCPServer.Core.Data;
using MCPServer.Core.Data.DataSeeding;
// Hub reference removed
using MCPServer.Core.Services;
using MCPServer.Core.Services.Interfaces;
using MCPServer.Core.Services.Llm;
using MCPServer.Utils;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using MySql.EntityFrameworkCore.Extensions;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add configuration
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true);
builder.Configuration.AddEnvironmentVariables();

// Configure specific settings sections
builder.Services.Configure<LlmSettings>(builder.Configuration.GetSection("AppSettings:Llm"));
builder.Services.Configure<RedisSettings>(builder.Configuration.GetSection("AppSettings:Redis"));
builder.Services.Configure<TokenSettings>(builder.Configuration.GetSection("AppSettings:Token"));
builder.Services.Configure<AuthSettings>(builder.Configuration.GetSection("AppSettings:Auth"));

// Keep AppSettings for backward compatibility during transition
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));

// Core project should not serve API endpoints or UI
// This project is for business logic and services only

// CORS configuration removed - not needed in Core project

// Register Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var redisSettings = builder.Configuration.GetSection("AppSettings:Redis").Get<RedisSettings>();
    var connectionString = redisSettings?.ConnectionString ?? "localhost:6379";
    var options = ConfigurationOptions.Parse(connectionString);
    options.AbortOnConnectFail = false;
    return ConnectionMultiplexer.Connect(options);
});

// Register HttpClient for LLM service
builder.Services.AddHttpClient<ILlmService, LlmService>();

// Register LLM services and factories
builder.Services.AddHttpClient("OpenAI");
builder.Services.AddHttpClient("Anthropic");
builder.Services.AddScoped<MCPServer.Core.Services.Llm.ILlmProviderFactory, MCPServer.Core.Services.Llm.OpenAiProviderFactory>();
builder.Services.AddScoped<MCPServer.Core.Services.Llm.ILlmProviderFactory, MCPServer.Core.Services.Llm.AnthropicProviderFactory>();
builder.Services.AddScoped<ILlmService, LlmService>();

// Configure Entity Framework
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<McpServerDbContext>(options =>
    options.UseMySQL(connectionString));

// Configure Authentication
var authSettings = builder.Configuration.GetSection("AppSettings:Auth").Get<AuthSettings>();
if (authSettings == null || string.IsNullOrEmpty(authSettings.Secret))
{
    throw new InvalidOperationException("Authentication secret key is not configured. Please check AppSettings:Auth:Secret in appsettings.json.");
}

var key = Encoding.ASCII.GetBytes(authSettings.Secret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = authSettings?.Issuer,
        ValidateAudience = true,
        ValidAudience = authSettings?.Audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    // Configure for SignalR
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            // Get access_token from query string
            var accessToken = context.Request.Query["access_token"];

            // Log the request path and token presence for debugging
            var path = context.HttpContext.Request.Path;

            Console.WriteLine($"OnMessageReceived: Path={path}, HasToken={!string.IsNullOrEmpty(accessToken)}");
            if (!string.IsNullOrEmpty(accessToken))
            {
                // First 20 chars of token for debugging (don't log full token in production)
                string tokenString = accessToken.ToString();
                Console.WriteLine($"Token starts with: {tokenString.Substring(0, Math.Min(20, tokenString.Length))}...");

                if (path.StartsWithSegments("/hubs"))
                {
                    // Set the token for authentication
                    context.Token = accessToken;
                    Console.WriteLine($"Token set for SignalR authentication");
                }
            }

            return Task.CompletedTask;
        },

        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"JWT Auth Failed: {context.Exception.GetType().Name}: {context.Exception.Message}");
            if (context.Exception.InnerException != null)
            {
                Console.WriteLine($"Inner Exception: {context.Exception.InnerException.Message}");
            }

            // Include token debugging information
            if (context.Request.Query.TryGetValue("access_token", out var token))
            {
                string tokenString = token.ToString();
                var tokenStart = tokenString.Substring(0, Math.Min(20, tokenString.Length));
                Console.WriteLine($"Failed token starts with: {tokenStart}...");
            }

            return Task.CompletedTask;
        },

        OnTokenValidated = context =>
        {
            Console.WriteLine($"JWT Token Validated for {context.Principal?.Identity?.Name}");
            Console.WriteLine($"Claims: {string.Join(", ", context.Principal?.Claims.Select(c => $"{c.Type}={c.Value}") ?? new string[0])}");
            return Task.CompletedTask;
        }
    };
});

// Add Authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
    options.AddPolicy("RequireUserRole", policy => policy.RequireRole("User"));
});

// Register services
builder.Services.AddScoped<IContextService, MySqlContextService>();
builder.Services.AddSingleton<ITokenManager, TokenManager>();
builder.Services.AddScoped<ILlmService, LlmService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ILlmProviderService, LlmProviderService>();

// Register RAG services
builder.Services.AddHttpClient<IEmbeddingService, MCPServer.Core.Services.Rag.EmbeddingService>();
builder.Services.AddScoped<IDocumentService, MCPServer.Core.Services.Rag.MySqlDocumentService>();
builder.Services.AddScoped<IVectorDbService, MCPServer.Core.Services.Rag.MySqlVectorDbService>();
builder.Services.AddScoped<IRagService, MCPServer.Core.Services.Rag.RagService>();

// Register database initializer and seeders
builder.Services.AddScoped<MCPServer.Core.Services.DatabaseInitializer>();
builder.Services.AddScoped<MCPServer.Core.Services.LlmProviderSeeder>();

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// Core project should not configure HTTP pipeline
// This is now handled by the API project

// Initialize the database
using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<MCPServer.Core.Services.DatabaseInitializer>();
    await initializer.InitializeAsync();

    // OpenAI API key can be provided via environment variable or directly passed here
    // for development purposes
    if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OPENAI_API_KEY")))
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<McpServerDbContext>();
        string devApiKey = "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";
        dbContext.SeedLlmProviderCredential(devApiKey);
    }
}

// Core project should not run as a web application
// This line is commented out to prevent the Core project from starting a web server
