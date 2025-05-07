using System.Threading.Tasks;

namespace MCPServer.Core.Services.Interfaces
{
    public interface IConnectionStringResolverService
    {
        Task<string> ResolveConnectionStringAsync(string connectionStringTemplate);
    }
}
