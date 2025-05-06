using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MCPServer.Core.Services.DataTransfer
{
    /// <summary>
    /// Utility class for hashing and masking connection strings
    /// </summary>
    public class ConnectionStringHasher
    {
        private readonly ILogger<ConnectionStringHasher> _logger;
        private const string HASHED_PREFIX = "HASHED:";
        private const string DUMMY_CONNECTION_STRING = "Server={0};Database={1};User ID=placeholder;Password=placeholder;";

        public ConnectionStringHasher(ILogger<ConnectionStringHasher>? logger = null)
        {
            _logger = logger ?? NullLogger<ConnectionStringHasher>.Instance;
        }

        /// <summary>
        /// Hashes a connection string for secure storage
        /// </summary>
        /// <param name="connectionString">The connection string to hash</param>
        /// <returns>The hashed connection string</returns>
        public string HashConnectionString(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                return string.Empty;
            }

            try
            {
                // Extract server and database for reference
                var serverMatch = Regex.Match(connectionString, @"Server=([^;]+)", RegexOptions.IgnoreCase);
                var databaseMatch = Regex.Match(connectionString, @"(Database|Initial Catalog)=([^;]+)", RegexOptions.IgnoreCase);

                string serverInfo = serverMatch.Success ? serverMatch.Groups[1].Value : "unknown";
                string databaseInfo = databaseMatch.Success ? databaseMatch.Groups[2].Value : "unknown";

                // Create a hash of the connection string
                using var hmac = new HMACSHA512();
                var salt = hmac.Key;
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(connectionString));

                // Combine salt and hash
                var hashBytes = new byte[salt.Length + hash.Length];
                Array.Copy(salt, 0, hashBytes, 0, salt.Length);
                Array.Copy(hash, 0, hashBytes, salt.Length, hash.Length);

                // Create a reference string that includes server and database info
                string hashedConnectionString = $"{HASHED_PREFIX}Server={serverInfo};Database={databaseInfo};Hash={Convert.ToBase64String(hashBytes)}";

                return hashedConnectionString;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error hashing connection string");
                return $"ERROR_HASHING:{connectionString}";
            }
        }

        /// <summary>
        /// Masks sensitive information in a connection string for display
        /// </summary>
        /// <param name="connectionString">The connection string to mask</param>
        /// <returns>The masked connection string</returns>
        public string MaskConnectionString(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                return string.Empty;
            }

            try
            {
                // Check if it's already a hashed connection string
                if (connectionString.StartsWith(HASHED_PREFIX))
                {
                    return connectionString;
                }

                // Replace password with asterisks
                var maskedConnectionString = Regex.Replace(
                    connectionString,
                    @"(Password|Pwd)=([^;]*)",
                    match => $"{match.Groups[1].Value}=********",
                    RegexOptions.IgnoreCase
                );

                // Replace user ID if present
                maskedConnectionString = Regex.Replace(
                    maskedConnectionString,
                    @"(User ID|Uid)=([^;]*)",
                    match => $"{match.Groups[1].Value}=********",
                    RegexOptions.IgnoreCase
                );

                return maskedConnectionString;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error masking connection string");
                return "ERROR_MASKING";
            }
        }

        /// <summary>
        /// Checks if a connection string is already hashed
        /// </summary>
        /// <param name="connectionString">The connection string to check</param>
        /// <returns>True if the connection string is hashed, false otherwise</returns>
        public bool IsConnectionStringHashed(string connectionString)
        {
            return !string.IsNullOrEmpty(connectionString) && connectionString.StartsWith(HASHED_PREFIX);
        }

        /// <summary>
        /// Creates a dummy connection string for UI display when the actual connection string is hashed
        /// </summary>
        /// <param name="connectionString">The hashed connection string</param>
        /// <returns>A dummy connection string with server and database info</returns>
        public string CreateDummyConnectionString(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString) || !IsConnectionStringHashed(connectionString))
            {
                return connectionString;
            }

            try
            {
                string serverInfo = GetServerInfo(connectionString);
                string databaseInfo = GetDatabaseInfo(connectionString);

                return string.Format(DUMMY_CONNECTION_STRING, serverInfo, databaseInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating dummy connection string");
                return "ERROR_CREATING_DUMMY_CONNECTION_STRING";
            }
        }

        /// <summary>
        /// Extracts connection details from a hashed connection string for UI display
        /// </summary>
        /// <param name="connectionString">The hashed connection string</param>
        /// <returns>A dictionary with connection details</returns>
        public Dictionary<string, string> ExtractConnectionDetails(string connectionString)
        {
            var details = new Dictionary<string, string>();

            if (string.IsNullOrEmpty(connectionString))
            {
                return details;
            }

            try
            {
                if (IsConnectionStringHashed(connectionString))
                {
                    details["server"] = GetServerInfo(connectionString);
                    details["database"] = GetDatabaseInfo(connectionString);
                    details["username"] = "********"; // Placeholder for security
                    details["password"] = "********"; // Placeholder for security
                }
                else
                {
                    // Extract server
                    var serverMatch = Regex.Match(connectionString, @"Server=([^;]+)", RegexOptions.IgnoreCase);
                    if (!serverMatch.Success)
                    {
                        serverMatch = Regex.Match(connectionString, @"Data Source=([^;]+)", RegexOptions.IgnoreCase);
                    }
                    if (serverMatch.Success)
                    {
                        details["server"] = serverMatch.Groups[1].Value;
                    }

                    // Extract database
                    var databaseMatch = Regex.Match(connectionString, @"Database=([^;]+)", RegexOptions.IgnoreCase);
                    if (!databaseMatch.Success)
                    {
                        databaseMatch = Regex.Match(connectionString, @"Initial Catalog=([^;]+)", RegexOptions.IgnoreCase);
                    }
                    if (databaseMatch.Success)
                    {
                        details["database"] = databaseMatch.Groups[1].Value;
                    }

                    // Extract username
                    var usernameMatch = Regex.Match(connectionString, @"User ID=([^;]+)", RegexOptions.IgnoreCase);
                    if (!usernameMatch.Success)
                    {
                        usernameMatch = Regex.Match(connectionString, @"Uid=([^;]+)", RegexOptions.IgnoreCase);
                    }
                    if (usernameMatch.Success)
                    {
                        details["username"] = usernameMatch.Groups[1].Value;
                    }

                    // Extract password (masked for security)
                    details["password"] = "********";

                    // Extract port if present
                    var portMatch = Regex.Match(connectionString, @"Port=([^;]+)", RegexOptions.IgnoreCase);
                    if (portMatch.Success)
                    {
                        details["port"] = portMatch.Groups[1].Value;
                    }
                }

                return details;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting connection details");
                return details;
            }
        }

        /// <summary>
        /// Extracts server information from a connection string
        /// </summary>
        /// <param name="connectionString">The connection string</param>
        /// <returns>The server information</returns>
        public string GetServerInfo(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                return string.Empty;
            }

            try
            {
                // Check if it's a hashed connection string
                if (connectionString.StartsWith(HASHED_PREFIX))
                {
                    var serverMatch = Regex.Match(connectionString, @"Server=([^;]+)", RegexOptions.IgnoreCase);
                    return serverMatch.Success ? serverMatch.Groups[1].Value : "unknown";
                }

                // Extract server from regular connection string
                var match = Regex.Match(connectionString, @"Server=([^;]+)", RegexOptions.IgnoreCase);
                if (!match.Success)
                {
                    match = Regex.Match(connectionString, @"Data Source=([^;]+)", RegexOptions.IgnoreCase);
                }

                return match.Success ? match.Groups[1].Value : "unknown";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting server info from connection string");
                return "error";
            }
        }

        /// <summary>
        /// Extracts database information from a connection string
        /// </summary>
        /// <param name="connectionString">The connection string</param>
        /// <returns>The database information</returns>
        public string GetDatabaseInfo(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                return string.Empty;
            }

            try
            {
                // Check if it's a hashed connection string
                if (connectionString.StartsWith(HASHED_PREFIX))
                {
                    var databaseMatch = Regex.Match(connectionString, @"Database=([^;]+)", RegexOptions.IgnoreCase);
                    return databaseMatch.Success ? databaseMatch.Groups[1].Value : "unknown";
                }

                // Extract database from regular connection string
                var match = Regex.Match(connectionString, @"Database=([^;]+)", RegexOptions.IgnoreCase);
                if (!match.Success)
                {
                    match = Regex.Match(connectionString, @"Initial Catalog=([^;]+)", RegexOptions.IgnoreCase);
                }

                return match.Success ? match.Groups[1].Value : "unknown";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting database info from connection string");
                return "error";
            }
        }

        /// <summary>
        /// Prepares a connection string for use with SQL connections by removing any hashing or masking
        /// </summary>
        /// <param name="connectionString">The connection string to prepare</param>
        /// <returns>A clean connection string that can be used with SQL connections</returns>
        public string PrepareConnectionStringForUse(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                return string.Empty;
            }

            try
            {
                // First check for any override parameters regardless of whether the connection string is hashed
                string overrideServer = null;
                string overrideDatabase = null;
                string overrideUsername = null;
                string overridePassword = null;
                
                // Check for server override
                var serverMatch = Regex.Match(connectionString, @"OverrideServer=([^;]+)", RegexOptions.IgnoreCase);
                if (serverMatch.Success)
                {
                    overrideServer = serverMatch.Groups[1].Value;
                    _logger.LogInformation("Found override server: {Server}", overrideServer);
                }
                
                // Check for database override
                var databaseMatch = Regex.Match(connectionString, @"OverrideDatabase=([^;]+)", RegexOptions.IgnoreCase);
                if (databaseMatch.Success)
                {
                    overrideDatabase = databaseMatch.Groups[1].Value;
                    _logger.LogInformation("Found override database: {Database}", overrideDatabase);
                }

                // Check for username override
                var usernameMatch = Regex.Match(connectionString, @"OverrideUsername=([^;]+)", RegexOptions.IgnoreCase);
                if (usernameMatch.Success)
                {
                    overrideUsername = usernameMatch.Groups[1].Value;
                    _logger.LogInformation("Found override username: {Username}", overrideUsername);
                }

                // Check for password override
                var passwordMatch = Regex.Match(connectionString, @"OverridePassword=([^;]+)", RegexOptions.IgnoreCase);
                if (passwordMatch.Success)
                {
                    overridePassword = passwordMatch.Groups[1].Value;
                    _logger.LogInformation("Found override password: [REDACTED]");
                }

                // If the connection string is not hashed
                if (!IsConnectionStringHashed(connectionString))
                {
                    // If we have override parameters, apply them directly
                    if (overrideServer != null || overrideDatabase != null || overrideUsername != null || overridePassword != null)
                    {
                        try
                        {
                            var builder = new SqlConnectionStringBuilder(connectionString);
                            
                            if (overrideServer != null)
                                builder.DataSource = overrideServer;
                                
                            if (overrideDatabase != null)
                                builder.InitialCatalog = overrideDatabase;

                            if (overrideUsername != null)
                                builder.UserID = overrideUsername;

                            if (overridePassword != null)
                                builder.Password = overridePassword;
                                
                            return builder.ConnectionString;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error applying override parameters to non-hashed connection string");
                        }
                    }
                    
                    return connectionString;
                }

                // For hashed connection strings, we need to extract the essential parameters
                _logger.LogWarning("Attempting to use a hashed connection string directly. Will rebuild with extracted or override values.");

                // Extract parameters from the hashed string or use override values
                string serverName = overrideServer ?? GetServerInfo(connectionString);
                string databaseName = overrideDatabase ?? GetDatabaseInfo(connectionString);

                _logger.LogInformation("Using server: {Server}, database: {Database} for connection string", serverName, databaseName);

                // Create a completely new connection string with just the essential parameters
                // Use overrides for username/password if provided, otherwise use defaults
                var connectionStringBuilder = new Dictionary<string, string>
                {
                    ["Server"] = serverName,
                    ["Database"] = databaseName,
                    ["User ID"] = overrideUsername ?? "pp-sa", // Use override username if available
                    ["Password"] = overridePassword ?? "RDlS8C6zVewS-wJOr4_oY5Y", // Use override password if available
                    ["Encrypt"] = "True",
                    ["TrustServerCertificate"] = "True",
                    ["Connection Timeout"] = "30"
                };

                // Rebuild the connection string
                var cleanConnectionString = string.Join(";",
                    connectionStringBuilder.Select(kv => $"{kv.Key}={kv.Value}"));

                _logger.LogInformation("Created a clean connection string");
                return cleanConnectionString;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error preparing connection string for use");
                return connectionString; // Return original to avoid breaking changes
            }
        }
    }
}
