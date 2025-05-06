using System.Text;
using MCPServer.API.Auth;
using MCPServer.API.Hubs;
using MCPServer.API.Middleware;
using MCPServer.API.Services;
using MCPServer.Core.Config;
using MCPServer.Core.Data;
using MCPServer.Core.Models;
using MCPServer.Core.Models.Llm;
using MCPServer.Core.Models.Rag;
using MCPServer.Core.Services;
using MCPServer.Core.Services.Interfaces;
using MCPServer.Core.Services.Llm;
using MCPServer.Core.Services.Rag;
using MCPServer.Core.Features.Auth;
using MCPServer.Core.Features.Shared;
using MCPServer.Core.Features.Shared.Services;
using MCPServer.Core.Features.Shared.Services.Interfaces;
using MCPServer.Core.Features.Chat;
using MCPServer.Core.Features.Llm;
using MCPServer.Core.Features.Models;
using MCPServer.Core.Features.Providers;
using MCPServer.Core.Features.Rag;
using MCPServer.Core.Features.Sessions;
using MCPServer.Core.Features.Usage;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Configure JSON serialization to handle circular references
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve;
        options.JsonSerializerOptions.MaxDepth = 64; // Increase max depth if needed
    });

// Add memory cache
builder.Services.AddMemoryCache();

// Register caching service - already registered by AddMemoryCache()

// Add response caching
builder.Services.AddResponseCaching(options =>
{
    options.MaximumBodySize = 1024 * 1024; // 1MB
    options.UseCaseSensitivePaths = false;
});

// For now, we'll use the default API versioning
// We'll implement API versioning in a future update when needed

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    // Create a swagger document for each API version
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "MCP Server API v1",
        Version = "v1",
        Description = "API for the MCP Server application. This API provides endpoints for managing LLM providers, models, and chat functionality. The API is organized by features: Auth, Chat, Llm, Rag, Usage, Models, Providers, and Sessions.",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "MCP Server Team",
            Email = "support@mcpserver.com"
        }
    });

    // Include XML comments if available
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    // Configure JWT authentication for Swagger
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Handle conflicting actions by using the API project's controllers
    options.ResolveConflictingActions(apiDescriptions =>
    {
        return apiDescriptions.FirstOrDefault(api =>
            api.ActionDescriptor.DisplayName != null &&
            api.ActionDescriptor.DisplayName.StartsWith("MCPServer.API"))
            ?? apiDescriptions.First();
    });

    // We'll add operation filters when we implement API versioning
});

// Add configuration
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true);
builder.Configuration.AddEnvironmentVariables();

// Configure specific settings sections
builder.Services.Configure<LlmSettings>(builder.Configuration.GetSection("AppSettings:Llm"));
builder.Services.Configure<TokenSettings>(builder.Configuration.GetSection("AppSettings:Token"));
builder.Services.Configure<AuthSettings>(builder.Configuration.GetSection("AppSettings:Auth"));
builder.Services.Configure<RedisSettings>(builder.Configuration.GetSection("AppSettings:Redis"));
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));

// Add SignalR for real-time communication
builder.Services.AddSignalR();

// Configure CORS with specific origins
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", builder =>
        builder.WithOrigins("http://localhost:2100", "http://localhost:2101") // Web app ports
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials() // Allow credentials
               .WithExposedHeaders("WWW-Authenticate", "Authorization"));

    // Special policy for SignalR
    options.AddPolicy("SignalRPolicy", builder =>
        builder.WithOrigins("http://localhost:2100", "http://localhost:2101") // Web app ports
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials()  // Required for SignalR when using auth
               .WithExposedHeaders("WWW-Authenticate", "Authorization"));
});

// Configure authentication
var authSettings = builder.Configuration.GetSection("AppSettings:Auth").Get<AuthSettings>();
string secretKey;

if (!string.IsNullOrEmpty(authSettings?.Secret))
{
    secretKey = authSettings.Secret;
}
else if (builder.Environment.IsDevelopment())
{
    secretKey = "dev-secret-key-minimum-length-32-chars";
}
else
{
    throw new InvalidOperationException("Authentication secret key is not configured for production");
}

var key = Encoding.ASCII.GetBytes(secretKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
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
        ClockSkew = TimeSpan.FromMinutes(5)
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
                Console.WriteLine($"Token starts with: {tokenString[..Math.Min(20, tokenString.Length)]}...");

                if (path.StartsWithSegments("/hubs"))
                {
                    // Set the token for authentication
                    context.Token = accessToken;
                    Console.WriteLine($"Token set for SignalR authentication");
                }
            }

            return Task.CompletedTask;
        }
    };
})
// Add test authentication handler for development
.AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });

// Register LLM services
builder.Services.AddLlmServices();

// Register LLM service explicitly
builder.Services.AddScoped<MCPServer.Core.Services.Interfaces.ILlmService, MCPServer.Core.Services.LlmService>();

// Register TokenManager service explicitly
builder.Services.AddSingleton<MCPServer.Core.Services.Interfaces.ITokenManager, MCPServer.Core.Services.TokenManager>();

// Register SessionContextService explicitly
builder.Services.AddScoped<MCPServer.Core.Services.Interfaces.ISessionContextService, MCPServer.Core.Services.SessionContextService>();

// Register ChatUsageService explicitly
builder.Services.AddScoped<MCPServer.Core.Services.Interfaces.IChatUsageService, MCPServer.Core.Services.ChatUsageService>();

// Register provider services
builder.Services.AddProviderServices();

// Configure Entity Framework
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<McpServerDbContext>(options =>
    options.UseMySQL(connectionString));

// Register DbContextFactory for background operations - making it scoped instead of singleton
builder.Services.AddDbContextFactory<McpServerDbContext>(options =>
    options.UseMySQL(connectionString), ServiceLifetime.Scoped);

// Register feature-based services
builder.Services.AddAuthServices();
builder.Services.AddSharedServices();
builder.Services.AddChatServices();
builder.Services.AddLlmServices();
builder.Services.AddModelServices();
builder.Services.AddProviderServices();
builder.Services.AddRagServices();
builder.Services.AddSessionServices();
builder.Services.AddUsageServices();

// Register repositories
builder.Services.AddRepository<LlmProvider>();
builder.Services.AddRepository<LlmModel>();
builder.Services.AddRepository<Document>();
builder.Services.AddRepository<Chunk>();
builder.Services.AddRepository<ChatUsageLog>();

// Note: We'll use the UnitOfWork pattern to access repositories
// The UnitOfWork will create the appropriate repository instances as needed

// Register repository implementations
builder.Services.AddScoped<MCPServer.Core.Data.Repositories.ILlmProviderRepository, MCPServer.Core.Data.Repositories.LlmProviderRepository>();
builder.Services.AddScoped<MCPServer.Core.Data.Repositories.ILlmModelRepository, MCPServer.Core.Data.Repositories.LlmModelRepository>();

// Register caching service
builder.Services.AddScoped<MCPServer.Core.Features.Shared.Services.Interfaces.ICachingService, MCPServer.Core.Features.Shared.Services.CachingService>();

// Register database initializer and seeders
builder.Services.AddScoped<MCPServer.Core.Services.DatabaseInitializer>();
builder.Services.AddScoped<MCPServer.Core.Data.LlmProviderSeeder>();

// Register DataTransfer services
builder.Services.AddScoped<MCPServer.Core.Services.DataTransfer.DataTransferService>();
builder.Services.AddScoped<MCPServer.Core.Services.DataTransfer.ConnectionStringHasher>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();

    // Add request logging middleware
    app.Use(async (context, next) =>
    {
        // Log the request
        var request = context.Request;
        Console.WriteLine($"Request: {request.Method} {request.Path}{request.QueryString} {request.Protocol}");
        Console.WriteLine($"Host: {request.Host}");
        Console.WriteLine($"Content-Type: {request.ContentType}");
        Console.WriteLine($"Content-Length: {request.ContentLength}");
        Console.WriteLine($"Scheme: {request.Scheme}");

        // Log headers
        Console.WriteLine("Headers:");
        foreach (var header in request.Headers)
        {
            Console.WriteLine($"  {header.Key}: {header.Value}");
        }

        // Call the next middleware
        await next();

        // Log the response
        var response = context.Response;
        Console.WriteLine($"Response: {response.StatusCode} {response.ContentType}");
    });

    // Enable Swagger UI in development
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "MCP Server API v1");
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "MCP Server API Documentation";

        // Enable try it out for all operations
        options.EnableTryItOutByDefault();

        // Display request duration
        options.DisplayRequestDuration();

        // Use modern UI
        options.DefaultModelsExpandDepth(-1); // Hide models section by default
        options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List); // Show operations collapsed

        // Add additional UI customizations
        options.EnableDeepLinking(); // Allow direct linking to operations
        options.EnableFilter(); // Enable filtering operations
        options.EnableValidator(); // Enable request validation

        // Add custom CSS for better readability
        options.InjectStylesheet("/swagger-ui/custom.css");
    });
}

// Apply CORS policies first - must be before any other middleware
app.UseCors("CorsPolicy"); // Default CORS policy for API endpoints

// Add response caching middleware
app.UseResponseCaching();

// Add global exception handling middleware
app.UseGlobalExceptionHandler();

// Only use HTTPS redirection in production
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Add authentication middleware
app.UseAuthentication();
app.UseAuthorization();

// Enable serving static files for Swagger UI
app.UseStaticFiles();

// Add a middleware to redirect root requests to the web app
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value?.ToLower();

    // Redirect root, index.html, and login.html to the web app
    if (path == "/" || path == "/index.html" || path == "/login.html")
    {
        context.Response.Redirect("http://localhost:2100");
        return;
    }

    await next();
});

// Log registered controllers for debugging
var controllers = app.Services.GetServices<ControllerActionEndpointConventionBuilder>();
Console.WriteLine($"Registered controllers: {controllers.Count()}");

// Map controllers
app.MapControllers();

// Use the SignalR-specific CORS policy for the hub endpoint
app.MapHub<McpHub>("/hubs/mcp").RequireCors("SignalRPolicy");

// Initialize the database
using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<MCPServer.Core.Services.DatabaseInitializer>();
    await initializer.InitializeAsync();

    // Seed LLM providers and models
    var providerSeeder = scope.ServiceProvider.GetRequiredService<MCPServer.Core.Data.LlmProviderSeeder>();
    await providerSeeder.SeedAsync();
    Console.WriteLine("LLM providers and models seeded successfully");

    // OpenAI API key should be provided via environment variable or user secrets
    // Do not hardcode API keys in production code
    if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OPENAI_API_KEY")) && app.Environment.IsDevelopment())
    {
        Console.WriteLine("Setting the OpenAI API key in the database");
        var dbContext = scope.ServiceProvider.GetRequiredService<McpServerDbContext>();
        dbContext.SeedLlmProviderCredential("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");
    }
}

app.Run();
