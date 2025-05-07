import api from './api';

/**
 * Utility function to extract data from nested $values objects in API responses
 * Handles both single and double-nested $values structures
 * Also processes nested $values in object properties
 */
function extractFromNestedValues(data: any): any {
  if (!data) return data;

  // Handle top-level $values
  if (data.$values) {
    // Handle double-nested $values
    if (typeof data.$values === 'object' && !Array.isArray(data.$values) && data.$values.$values) {
      data = data.$values.$values;
    } else {
      data = data.$values;
    }
  }

  // If result is an array, process each item for nested $values
  if (Array.isArray(data)) {
    // Extract single item from array if needed
    if (data.length === 1) {
      return processObjectProperties(data[0]);
    }

    // Process each item in the array
    return data.map(item => processObjectProperties(item));
  }

  // Process object properties
  return processObjectProperties(data);
}

/**
 * Process object properties to extract nested $values
 */
function processObjectProperties(obj: any): any {
  if (!obj || typeof obj !== 'object') return obj;

  const result = {...obj};

  // Process each property for nested $values
  for (const key of Object.keys(result)) {
    if (result[key] && typeof result[key] === 'object') {
      if (result[key].$values) {
        result[key] = extractFromNestedValues(result[key]);
      } else if (Array.isArray(result[key])) {
        result[key] = result[key].map((item: any) =>
          item && typeof item === 'object' && item.$values ?
            extractFromNestedValues(item) : item
        );
      }
    }
  }

  return result;
}

class DataTransferService {
  // Database connections
  static async getConnections() {
    try {
      const response = await api.get('/data-transfer/connections');

      // Extract data from nested $values
      const connections = extractFromNestedValues(response.data);

      // Process each connection to ensure proper casing of property names
      const processedConnections = Array.isArray(connections) ? connections.map(conn => {
        if (!conn) return null;

        // Create a new object with camelCase property names
        const processedConn: any = {};

        // Map each property with proper casing
        if (conn.ConnectionId !== undefined) processedConn.connectionId = conn.ConnectionId;
        else if (conn.connectionId !== undefined) processedConn.connectionId = conn.connectionId;

        if (conn.ConnectionName !== undefined) processedConn.connectionName = conn.ConnectionName;
        else if (conn.connectionName !== undefined) processedConn.connectionName = conn.connectionName;

        if (conn.ConnectionString !== undefined) processedConn.connectionString = conn.ConnectionString;
        else if (conn.connectionString !== undefined) processedConn.connectionString = conn.connectionString;

        if (conn.Description !== undefined) processedConn.description = conn.Description;
        else if (conn.description !== undefined) processedConn.description = conn.description;

        if (conn.IsActive !== undefined) processedConn.isActive = conn.IsActive;
        else if (conn.isActive !== undefined) processedConn.isActive = conn.isActive;

        if (conn.ConnectionAccessLevel !== undefined) processedConn.connectionAccessLevel = conn.ConnectionAccessLevel;
        else if (conn.connectionAccessLevel !== undefined) processedConn.connectionAccessLevel = conn.connectionAccessLevel;

        if (conn.LastTestedOn !== undefined) processedConn.lastTestedOn = conn.LastTestedOn;
        else if (conn.lastTestedOn !== undefined) processedConn.lastTestedOn = conn.lastTestedOn;

        if (conn.CreatedOn !== undefined) processedConn.createdOn = conn.CreatedOn;
        else if (conn.createdOn !== undefined) processedConn.createdOn = conn.createdOn;

        if (conn.LastModifiedOn !== undefined) processedConn.lastModifiedOn = conn.LastModifiedOn;
        else if (conn.lastModifiedOn !== undefined) processedConn.lastModifiedOn = conn.lastModifiedOn;

        if (conn.MaxPoolSize !== undefined) processedConn.maxPoolSize = conn.MaxPoolSize;
        else if (conn.maxPoolSize !== undefined) processedConn.maxPoolSize = conn.maxPoolSize;

        if (conn.MinPoolSize !== undefined) processedConn.minPoolSize = conn.MinPoolSize;
        else if (conn.minPoolSize !== undefined) processedConn.minPoolSize = conn.minPoolSize;

        // Copy any other properties that might exist
        for (const key in conn) {
          if (!processedConn.hasOwnProperty(key.charAt(0).toLowerCase() + key.slice(1))) {
            const camelCaseKey = key.charAt(0).toLowerCase() + key.slice(1);
            processedConn[camelCaseKey] = conn[key];
          }
        }

        return processedConn;
      }).filter(Boolean) : []; // Remove any null entries

      return processedConnections;
    } catch (error) {
      console.error('Error in getConnections:', error);
      return [];
    }
  }

  static async getConnection(id: number) {
    try {
      const response = await api.get(`/data-transfer/connections/${id}`);
      return extractFromNestedValues(response.data);
    } catch (error) {
      console.error('Error in getConnection:', error);
      throw error;
    }
  }

  static async saveConnection(connection: any) {
    try {
      // Just log the connection data for debugging
      console.log('Saving connection with data:', connection);

      // Both create and update use the same POST endpoint
      const response = await api.post('/data-transfer/connections', connection);
      return response.data;
    } catch (error: any) {
      console.error('Error saving connection:', error);
      throw error;
    }
  }

  static async testConnection(connection: any) {
    try {
      // Just log the connection data for debugging
      console.log('Testing connection with data:', connection);

      const response = await api.post('/data-transfer/connections/test', connection);
      return response.data;
    } catch (error: any) {
      if (error.response && error.response.data) {
        return {
          success: false,
          message: error.response.data.message || error.response.data.error || 'Connection test failed'
        };
      }

      return {
        success: false,
        message: error.message || 'Connection test failed'
      };
    }
  }

  // Database schema operations
  static async getDatabaseSchema(connectionId: number) {
    try {
      // const response = await api.get(`/data-transfer/schema/${connectionId}`);
      // Updated to GET and new endpoint
      const response = await api.get(`/data-transfer/Schema/databases?connectionId=${connectionId}`);
      return extractFromNestedValues(response.data);
    } catch (error) {
      console.error('Error in getDatabaseSchema:', error);
      throw error;
    }
  }

  static async getDatabaseSchemaByConnectionString(connectionString: string) {
    try {
      const response = await api.post('/data-transfer/schema', { connectionString });
      return extractFromNestedValues(response.data);
    } catch (error) {
      console.error('Error in getDatabaseSchemaByConnectionString:', error);
      throw error;
    }
  }

  // Method to fetch database schema using connection data object
  static async fetchDatabaseSchema(connectionData: any) {
    try {
      console.log('Fetching schema using connection data object');
      
      // Make sure connectionData has required properties
      const payload = {
        ...connectionData,
        connectionName: connectionData.connectionName || 'Temporary Connection',
        isActive: connectionData.isActive !== false, // default to true
        maxPoolSize: connectionData.maxPoolSize || 100,
        minPoolSize: connectionData.minPoolSize || 5,
        timeout: connectionData.timeout || 30,
        encrypt: connectionData.encrypt !== false, // default to true
        trustServerCertificate: connectionData.trustServerCertificate !== false, // default to true
      };
      
      // Process connection string if needed
      if (payload.connectionString) {
        // Convert Username= to User ID= if needed (SQL Server format)
        if (payload.connectionString.includes('Username=') && !payload.connectionString.includes('User ID=')) {
          payload.connectionString = payload.connectionString.replace(/Username=([^;]+)/g, "User ID=$1");
        }
        
        // Convert Password= to pwd= if needed
        if (payload.connectionString.includes('Password=') && !payload.connectionString.includes('pwd=')) {
          payload.connectionString = payload.connectionString.replace(/Password=([^;]+)/g, "pwd=$1");
        }
        
        // Set default connection timeout if none exists
        if (!payload.connectionString.match(/Connect\s*Timeout=/i) && 
            !payload.connectionString.match(/Connection\s*Timeout=/i)) {
          payload.connectionString += ';Connection Timeout=30';
        }
        
        console.log('Connection string prepared (first 30 chars):', 
          payload.connectionString.substring(0, 30) + '...');
      }
      
      // Send the request to the API
      const response = await api.post('/data-transfer/connections/schema', payload, {
        timeout: 60000 // Increase timeout to 60 seconds for schema retrieval
      });
      
      return extractFromNestedValues(response.data);
    } catch (error) {
      console.error('Error in fetchDatabaseSchema:', error);
      // Enhance error handling
      if (error.response && error.response.data) {
        // Extract the error message from the response
        const errorData = error.response.data;
        if (typeof errorData === 'object') {
          console.error('Server error details:', errorData);
          // Throw with more detailed message if available
          throw new Error(errorData.message || errorData.error || 'Failed to fetch database schema');
        }
      }
      throw error;
    }
  }

  // Configuration operations
  static async getConfigurations() {
    try {
      const response = await api.get('/data-transfer/configurations');

      // Extract data from nested $values
      const configurations = extractFromNestedValues(response.data);

      // Process each configuration to ensure proper casing of property names
      const processedConfigurations = Array.isArray(configurations) ? configurations.map(config => {
        if (!config) return null;

        // Create a new object with camelCase property names
        const processedConfig: any = {};

        // Map each property with proper casing
        if (config.ConfigurationId !== undefined) processedConfig.configurationId = config.ConfigurationId;
        else if (config.configurationId !== undefined) processedConfig.configurationId = config.configurationId;

        if (config.ConfigurationName !== undefined) processedConfig.configurationName = config.ConfigurationName;
        else if (config.configurationName !== undefined) processedConfig.configurationName = config.configurationName;

        if (config.Description !== undefined) processedConfig.description = config.Description;
        else if (config.description !== undefined) processedConfig.description = config.description;

        if (config.IsActive !== undefined) processedConfig.isActive = config.IsActive;
        else if (config.isActive !== undefined) processedConfig.isActive = config.isActive;

        if (config.BatchSize !== undefined) processedConfig.batchSize = config.BatchSize;
        else if (config.batchSize !== undefined) processedConfig.batchSize = config.batchSize;

        if (config.ReportingFrequency !== undefined) processedConfig.reportingFrequency = config.ReportingFrequency;
        else if (config.reportingFrequency !== undefined) processedConfig.reportingFrequency = config.reportingFrequency;

        // Process nested objects like SourceConnection and DestinationConnection
        if (config.SourceConnection) {
          processedConfig.sourceConnection = {
            connectionId: config.SourceConnection.ConnectionId || config.SourceConnection.connectionId,
            connectionName: config.SourceConnection.ConnectionName || config.SourceConnection.connectionName,
            connectionString: config.SourceConnection.ConnectionString || config.SourceConnection.connectionString,
            description: config.SourceConnection.Description || config.SourceConnection.description || '',
            isActive: config.SourceConnection.IsActive !== undefined ? config.SourceConnection.IsActive :
                     (config.SourceConnection.isActive !== undefined ? config.SourceConnection.isActive : true)
          };
        } else if (config.sourceConnection) {
          processedConfig.sourceConnection = config.sourceConnection;
        }

        if (config.DestinationConnection) {
          processedConfig.destinationConnection = {
            connectionId: config.DestinationConnection.ConnectionId || config.DestinationConnection.connectionId,
            connectionName: config.DestinationConnection.ConnectionName || config.DestinationConnection.connectionName,
            connectionString: config.DestinationConnection.ConnectionString || config.DestinationConnection.connectionString,
            description: config.DestinationConnection.Description || config.DestinationConnection.description || '',
            isActive: config.DestinationConnection.IsActive !== undefined ? config.DestinationConnection.IsActive :
                     (config.DestinationConnection.isActive !== undefined ? config.DestinationConnection.isActive : true)
          };
        } else if (config.destinationConnection) {
          processedConfig.destinationConnection = config.destinationConnection;
        }

        // Process table mappings array
        if (config.TableMappings && Array.isArray(config.TableMappings)) {
          processedConfig.tableMappings = config.TableMappings.map((mapping: any) => ({
            tableMappingId: mapping.TableMappingId || mapping.tableMappingId,
            sourceTable: mapping.SourceTable || mapping.sourceTable,
            destinationTable: mapping.DestinationTable || mapping.destinationTable,
            isActive: mapping.IsActive !== undefined ? mapping.IsActive :
                     (mapping.isActive !== undefined ? mapping.isActive : true)
          }));
        } else if (config.tableMappings && Array.isArray(config.tableMappings)) {
          processedConfig.tableMappings = config.tableMappings;
        } else {
          processedConfig.tableMappings = [];
        }

        // Copy any other properties that might exist
        for (const key in config) {
          if (!processedConfig.hasOwnProperty(key.charAt(0).toLowerCase() + key.slice(1))) {
            const camelCaseKey = key.charAt(0).toLowerCase() + key.slice(1);
            processedConfig[camelCaseKey] = config[key];
          }
        }

        return processedConfig;
      }).filter(Boolean) : []; // Remove any null entries

      return processedConfigurations;
    } catch (error) {
      console.error('Error in getConfigurations:', error);
      return [];
    }
  }

  static async getConfiguration(id: number) {
    try {
      const response = await api.get(`/data-transfer/configurations/${id}`);
      return extractFromNestedValues(response.data);
    } catch (error) {
      console.error('Error in getConfiguration:', error);
      throw error;
    }
  }

  static async saveConfiguration(configuration: any) {
    if (configuration.configurationId) {
      // Update existing configuration
      const response = await api.put(`/data-transfer/configurations/${configuration.configurationId}`, configuration);
      return response.data;
    } else {
      // Create new configuration
      const response = await api.post('/data-transfer/configurations', configuration);
      return response.data;
    }
  }

  static async deleteConfiguration(id: number) {
    const response = await api.delete(`/data-transfer/configurations/${id}`);
    return response.data;
  }

  static async testConfiguration(id: number) {
    const response = await api.post(`/data-transfer/configurations/${id}/test`);
    return response.data;
  }

  static async executeTransfer(id: number) {
    const response = await api.post(`/data-transfer/configurations/${id}/execute`);
    return response.data;
  }

  // Run history operations
  static async getRunHistory(configurationId: number = 0) {
    try {
      const url = configurationId
        ? `/data-transfer/history?configurationId=${configurationId}`
        : '/data-transfer/history';

      const response = await api.get(url);

      // Extract data from nested $values
      const runHistory = extractFromNestedValues(response.data);

      // Process each run history item to ensure proper casing of property names
      const processedRunHistory = Array.isArray(runHistory) ? runHistory.map(item => {
        if (!item) return null;

        // Create a new object with camelCase property names
        const processedItem: any = {};

        // Map each property with proper casing
        if (item.RunId !== undefined) processedItem.runId = item.RunId;
        else if (item.runId !== undefined) processedItem.runId = item.runId;

        if (item.ConfigurationId !== undefined) processedItem.configurationId = item.ConfigurationId;
        else if (item.configurationId !== undefined) processedItem.configurationId = item.configurationId;

        if (item.ConfigurationName !== undefined) processedItem.configurationName = item.ConfigurationName;
        else if (item.configurationName !== undefined) processedItem.configurationName = item.configurationName;

        if (item.StartTime !== undefined) processedItem.startTime = item.StartTime;
        else if (item.startTime !== undefined) processedItem.startTime = item.startTime;

        if (item.EndTime !== undefined) processedItem.endTime = item.EndTime;
        else if (item.endTime !== undefined) processedItem.endTime = item.endTime;

        if (item.Status !== undefined) processedItem.status = item.Status;
        else if (item.status !== undefined) processedItem.status = item.status;

        if (item.TablesProcessed !== undefined) processedItem.tablesProcessed = item.TablesProcessed;
        else if (item.tablesProcessed !== undefined) processedItem.tablesProcessed = item.tablesProcessed;

        if (item.RowsProcessed !== undefined) processedItem.rowsProcessed = item.RowsProcessed;
        else if (item.rowsProcessed !== undefined) processedItem.rowsProcessed = item.rowsProcessed;

        if (item.ElapsedTime !== undefined) processedItem.elapsedTime = item.ElapsedTime;
        else if (item.elapsedTime !== undefined) processedItem.elapsedTime = item.elapsedTime;

        // Copy any other properties that might exist
        for (const key in item) {
          if (!processedItem.hasOwnProperty(key.charAt(0).toLowerCase() + key.slice(1))) {
            const camelCaseKey = key.charAt(0).toLowerCase() + key.slice(1);
            processedItem[camelCaseKey] = item[key];
          }
        }

        return processedItem;
      }).filter(Boolean) : []; // Remove any null entries

      return processedRunHistory;
    } catch (error) {
      console.error('Error in getRunHistory:', error);
      return [];
    }
  }

  static async getRunDetails(runId: number) {
    try {
      const response = await api.get(`/data-transfer/history/${runId}`);
      return extractFromNestedValues(response.data);
    } catch (error) {
      console.error('Error in getRunDetails:', error);
      throw error;
    }
  }

  // Schema operations
  static async saveSchema(schema: any) {
    const response = await api.post('/data-transfer/schema/save', schema);
    return response.data;
  }

  static async getSchemas() {
    try {
      const response = await api.get('/data-transfer/schemas');
      return extractFromNestedValues(response.data);
    } catch (error) {
      console.error('Error in getSchemas:', error);
      return [];
    }
  }

  static async getSchema(id: number) { // This 'id' seems to be a configuration ID not a connection ID
    try {
      // This likely refers to a saved configuration, not directly a database schema by connection ID.
      // If it's meant to get schema for a connection ID, it should be getDatabaseSchema(id)
      const response = await api.get(`/data-transfer/configurations/${id}`);
      // Assuming the configuration might contain connection details to then fetch the schema,
      // or this method is misnamed/misused in the original context.
      // For now, keeping it as fetching configuration, as the path suggests.
      // If it was intended to fetch schema by a direct ID that's not a connectionId,
      // the backend doesn't seem to support that directly via an ID like '14' for a schema.
      return extractFromNestedValues(response.data);
    } catch (error) {
      console.error('Error in getSchema:', error);
      throw error;
    }
  }
}

export default DataTransferService;