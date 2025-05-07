using System.Threading.Tasks;

namespace MCPServer.Core.Services.Interfaces
{
    public interface IAzureKeyVaultService
    {
        Task<string> GetSecretAsync(string vaultName, string secretName);
    }
}
