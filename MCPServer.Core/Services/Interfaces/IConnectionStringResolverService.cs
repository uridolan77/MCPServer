using System.Threading.Tasks;

namespace MCPServer.Core.Services.Interfaces
{
    public interface IConnectionStringResolverService
    {
        Task<string> ResolveConnectionStringAsync(string connectionStringTemplate);
        
        /// <summary>
        /// Clears all cached secrets and connection strings to force fresh retrieval
        /// </summary>
        void ClearCaches();
    }
}
