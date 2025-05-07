using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MCPServer.Core.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace MCPServer.Core.Services
{
    public class ConnectionStringResolverService : IConnectionStringResolverService
    {
        private readonly IAzureKeyVaultService _keyVaultService;
        private readonly ILogger<ConnectionStringResolverService> _logger;
        
        // Regex to find placeholders like {azurevault:vaultName:secretName}
        private static readonly Regex AzureVaultPlaceholderRegex = new Regex(@"\{azurevault:([^:]+):([^}]+)\}", RegexOptions.IgnoreCase);

        public ConnectionStringResolverService(IAzureKeyVaultService keyVaultService, ILogger<ConnectionStringResolverService> logger)
        {
            _keyVaultService = keyVaultService ?? throw new ArgumentNullException(nameof(keyVaultService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> ResolveConnectionStringAsync(string connectionStringTemplate)
        {
            if (string.IsNullOrWhiteSpace(connectionStringTemplate))
            {
                return connectionStringTemplate;
            }

            _logger.LogInformation("Attempting to resolve connection string template.");
            
            string resolvedConnectionString = connectionStringTemplate;
            MatchCollection matches = AzureVaultPlaceholderRegex.Matches(connectionStringTemplate);

            if (matches.Count == 0)
            {
                _logger.LogInformation("No Azure Key Vault placeholders found in the connection string template.");
                return connectionStringTemplate;
            }

            _logger.LogInformation("Found {MatchCount} Azure Key Vault placeholder(s) to resolve.", matches.Count);

            foreach (Match match in matches)
            {
                string placeholder = match.Value; // e.g., {azurevault:vaultName:secretName}
                string vaultName = match.Groups[1].Value;
                string secretName = match.Groups[2].Value;

                _logger.LogDebug("Resolving placeholder: {Placeholder} (Vault: {VaultName}, Secret: {SecretName})", placeholder, vaultName, secretName);

                try
                {
                    string secretValue = await _keyVaultService.GetSecretAsync(vaultName, secretName);
                    if (secretValue == null)
                    {
                        _logger.LogWarning("Secret '{SecretName}' from vault '{VaultName}' resolved to null. Placeholder '{Placeholder}' will not be replaced.", secretName, vaultName, placeholder);
                        // Optionally, decide if this should throw an error or leave the placeholder
                        // For now, it leaves the placeholder if the secret value is null.
                    }
                    else
                    {
                        resolvedConnectionString = resolvedConnectionString.Replace(placeholder, secretValue);
                        _logger.LogInformation("Successfully replaced placeholder '{Placeholder}' with secret from vault '{VaultName}'.", placeholder, vaultName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to resolve secret for placeholder '{Placeholder}' (Vault: {VaultName}, Secret: {SecretName}). The placeholder will not be replaced.", placeholder, vaultName, secretName);
                    // Depending on policy, you might want to re-throw, or continue to allow other placeholders to be resolved.
                    // For now, we log and continue, leaving the placeholder in place.
                }
            }
            
            _logger.LogInformation("Finished resolving connection string template.");
            return resolvedConnectionString;
        }
    }
}
