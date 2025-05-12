using System;
using System.Collections.Concurrent;
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
        
        // Cache to store resolved secrets from Azure Key Vault
        private readonly ConcurrentDictionary<string, string> _secretCache;
        
        // Regex to find placeholders like {azurevault:vaultName:secretName}
        private static readonly Regex AzureVaultPlaceholderRegex = new Regex(@"\{azurevault:([^:]+):([^}]+)\}", RegexOptions.IgnoreCase);

        // Resolved connection string cache - stores fully resolved connection strings
        private readonly ConcurrentDictionary<string, string> _connectionStringCache;

        public ConnectionStringResolverService(IAzureKeyVaultService keyVaultService, ILogger<ConnectionStringResolverService> logger)
        {
            _keyVaultService = keyVaultService ?? throw new ArgumentNullException(nameof(keyVaultService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _secretCache = new ConcurrentDictionary<string, string>();
            _connectionStringCache = new ConcurrentDictionary<string, string>();
        }

        public async Task<string> ResolveConnectionStringAsync(string connectionStringTemplate)
        {
            if (string.IsNullOrWhiteSpace(connectionStringTemplate))
            {
                return connectionStringTemplate;
            }

            // Check if this connection string template has already been resolved and cached
            if (_connectionStringCache.TryGetValue(connectionStringTemplate, out string cachedConnectionString))
            {
                _logger.LogDebug("Using cached resolved connection string");
                return cachedConnectionString;
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
                string cacheKey = $"{vaultName}:{secretName}";

                _logger.LogDebug("Resolving placeholder: {Placeholder} (Vault: {VaultName}, Secret: {SecretName})", placeholder, vaultName, secretName);

                string secretValue;
                
                // Try to get the secret value from the cache first
                if (_secretCache.TryGetValue(cacheKey, out secretValue))
                {
                    _logger.LogDebug("Using cached secret for '{SecretName}' from vault '{VaultName}'", secretName, vaultName);
                }
                else
                {
                    try
                    {
                        // If not in cache, retrieve from Azure Key Vault
                        secretValue = await _keyVaultService.GetSecretAsync(vaultName, secretName);
                        
                        // Cache the result (even if null, to avoid repeated failed lookups)
                        _secretCache.TryAdd(cacheKey, secretValue);
                        
                        _logger.LogInformation("Cached secret for '{SecretName}' from vault '{VaultName}'", secretName, vaultName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to resolve secret for placeholder '{Placeholder}' (Vault: {VaultName}, Secret: {SecretName}). The placeholder will not be replaced.", placeholder, vaultName, secretName);
                        continue; // Skip to the next placeholder
                    }
                }
                
                if (secretValue == null)
                {
                    _logger.LogWarning("Secret '{SecretName}' from vault '{VaultName}' resolved to null. Placeholder '{Placeholder}' will not be replaced.", secretName, vaultName, placeholder);
                }
                else
                {
                    resolvedConnectionString = resolvedConnectionString.Replace(placeholder, secretValue);
                    _logger.LogDebug("Successfully replaced placeholder '{Placeholder}' with secret from vault '{VaultName}'.", placeholder, vaultName);
                }
            }
            
            // Cache the fully resolved connection string
            _connectionStringCache.TryAdd(connectionStringTemplate, resolvedConnectionString);
            
            _logger.LogInformation("Finished resolving connection string template.");
            return resolvedConnectionString;
        }
        
        // Method to clear caches (useful for refreshing secrets if needed)
        public void ClearCaches()
        {
            _secretCache.Clear();
            _connectionStringCache.Clear();
            _logger.LogInformation("Connection string resolver caches cleared");
        }
    }
}
