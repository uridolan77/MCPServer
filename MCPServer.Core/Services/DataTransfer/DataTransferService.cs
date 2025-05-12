using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MCPServer.Core.Services.DataTransfer
{
    // Stub class to maintain compatibility with existing code
    public class DataTransferService
    {
        // Common types needed for compilation
        public class ConnectionDto 
        {
            public int ConnectionId { get; set; }
            public string ConnectionName { get; set; }
            public string ConnectionString { get; set; }
            public string ConnectionAccessLevel { get; set; }
            // Add any properties needed for the project to compile
        }
        
        public class DataTransferConfigurationDto
        {
            public int ConfigurationId { get; set; }
            public string ConfigurationName { get; set; }
            // Add any properties needed for the project to compile
        }
    }
}