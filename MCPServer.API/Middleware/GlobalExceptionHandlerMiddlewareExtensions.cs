using Microsoft.AspNetCore.Builder;

namespace MCPServer.API.Middleware
{
    public static class GlobalExceptionHandlerMiddlewareExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandler(
            this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<GlobalExceptionHandlerMiddleware>();
        }
    }
}