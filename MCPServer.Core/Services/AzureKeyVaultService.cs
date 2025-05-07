using System;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using MCPServer.Core.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace MCPServer.Core.Services
{
    public class AzureKeyVaultService : IAzureKeyVaultService
    {
        private readonly ILogger<AzureKeyVaultService> _logger;

        public AzureKeyVaultService(ILogger<AzureKeyVaultService> logger)
        {
            _logger = logger;
        }

        public async Task<string> GetSecretAsync(string vaultName, string secretName)
        {
            if (string.IsNullOrWhiteSpace(vaultName))
            {
                _logger.LogError("Vault name cannot be null or empty.");
                throw new ArgumentNullException(nameof(vaultName));
            }
            if (string.IsNullOrWhiteSpace(secretName))
            {
                _logger.LogError("Secret name cannot be null or empty.");
                throw new ArgumentNullException(nameof(secretName));
            }

            try
            {
                // Construct the Key Vault URI
                var vaultUri = new Uri($"https://{vaultName}.vault.azure.net/");

                // Create a SecretClient using DefaultAzureCredential
                // DefaultAzureCredential will attempt to authenticate using various methods:
                // - Environment variables
                // - Managed Identity (if deployed to Azure services)
                // - Visual Studio credentials
                // - Azure CLI credentials
                // - Azure PowerShell credentials
                // - Interactive browser credential
                var client = new SecretClient(vaultUri, new DefaultAzureCredential());

                _logger.LogInformation("Attempting to retrieve secret '{SecretName}' from vault '{VaultName}'.", secretName, vaultName);
                KeyVaultSecret secret = await client.GetSecretAsync(secretName);
                _logger.LogInformation("Successfully retrieved secret '{SecretName}' from vault '{VaultName}'.", secretName, vaultName);
                
                return secret.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving secret '{SecretName}' from vault '{VaultName}'. Ensure the application has appropriate permissions and the vault/secret names are correct.", secretName, vaultName);
                throw; // Re-throw the exception to be handled by the caller
            }
        }
    }
}
