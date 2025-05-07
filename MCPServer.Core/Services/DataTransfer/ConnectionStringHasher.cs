using System;
using Microsoft.Extensions.Logging;

namespace MCPServer.Core.Services.DataTransfer
{
    /// <summary>
    /// This class is currently a placeholder. 
    /// Its previous responsibilities for connection string hashing/obfuscation 
    /// have been largely superseded by Azure Key Vault integration.
    /// It can be repurposed for other connection string related utilities if needed in the future,
    /// such as encryption/decryption for non-KeyVault connection strings stored in the database.
    /// </summary>
    public class ConnectionStringHasher
    {
        private readonly ILogger _logger;

        public ConnectionStringHasher(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogInformation("ConnectionStringHasher initialized. Note: Hashing/obfuscation logic is currently minimal due to Key Vault integration.");
        }
    }
}
