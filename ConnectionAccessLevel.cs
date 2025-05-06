namespace MCPServer.Core.Services.DataTransfer
{
    /// <summary>
    /// Defines the access level for a database connection
    /// </summary>
    public enum ConnectionAccessLevel
    {
        /// <summary>
        /// Connection can be used for reading data only
        /// </summary>
        ReadOnly,
        
        /// <summary>
        /// Connection can be used for writing data only
        /// </summary>
        WriteOnly,
        
        /// <summary>
        /// Connection can be used for both reading and writing data
        /// </summary>
        ReadWrite
    }
}
