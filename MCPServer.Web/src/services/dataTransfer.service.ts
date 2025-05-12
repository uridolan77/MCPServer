import { Connection } from '../pages/transfer/types/Connection';
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
      const response = await api.get('/datatransfer/connections');

      // Extract data from nested $values
      const connections = extractFromNestedValues(response.data);
      
      console.log('Raw connections from API:', connections);

      // Process each connection to match our standardized Connection interface
      const processedConnections = Array.isArray(connections) ? connections.map(conn => {
        if (!conn) return null;

        console.log('Processing connection:', conn);

        // Create a standardized Connection object that matches our database model exactly
        return {
          connectionId: conn.ConnectionId || conn.connectionId || conn.id || 0,
          connectionName: conn.ConnectionName || conn.connectionName || conn.name || '',
          connectionString: conn.ConnectionString || conn.connectionString || '',
          description: conn.Description || conn.description || '',
          isActive: conn.IsActive !== undefined ? conn.IsActive : 
                  (conn.isActive !== undefined ? conn.isActive : true),
          
          // Database connection details
          server: conn.Server || conn.server || '',
          port: conn.Port || conn.port || null,
          database: conn.Database || conn.database || '',
          username: conn.Username || conn.username || '',
          password: conn.Password || conn.password || '',
          additionalParameters: conn.AdditionalParameters || conn.additionalParameters || '',
          
          // Connection settings
          isConnectionValid: conn.IsConnectionValid || conn.isConnectionValid || false,
          minPoolSize: conn.MinPoolSize || conn.minPoolSize || null,
          maxPoolSize: conn.MaxPoolSize || conn.maxPoolSize || null,
          timeout: conn.Timeout || conn.timeout || null,
          trustServerCertificate: conn.TrustServerCertificate !== undefined ? conn.TrustServerCertificate :
                                (conn.trustServerCertificate !== undefined ? conn.trustServerCertificate : true),
          encrypt: conn.Encrypt !== undefined ? conn.Encrypt : 
                 (conn.encrypt !== undefined ? conn.encrypt : true),
          
          // Metadata
          createdBy: conn.CreatedBy || conn.createdBy || 'System',
          createdOn: conn.CreatedOn || conn.createdOn || null,
          lastModifiedBy: conn.LastModifiedBy || conn.lastModifiedBy || 'System',
          lastModifiedOn: conn.LastModifiedOn || conn.lastModifiedOn || null,
          lastTestedOn: conn.LastTestedOn || conn.lastTestedOn || null,
          
          // Access control
          connectionAccessLevel: conn.ConnectionAccessLevel || conn.connectionAccessLevel || 'ReadOnly',
          
          // Computed properties based on connectionAccessLevel
          isSource: conn.IsSource !== undefined ? conn.IsSource :
                  (conn.isSource !== undefined ? conn.isSource : 
                   (conn.ConnectionAccessLevel === 'ReadOnly' || conn.ConnectionAccessLevel === 'ReadWrite' || 
                    conn.connectionAccessLevel === 'ReadOnly' || conn.connectionAccessLevel === 'ReadWrite')),
          isDestination: conn.IsDestination !== undefined ? conn.IsDestination :
                       (conn.isDestination !== undefined ? conn.isDestination : 
                        (conn.ConnectionAccessLevel === 'WriteOnly' || conn.ConnectionAccessLevel === 'ReadWrite' || 
                         conn.connectionAccessLevel === 'WriteOnly' || conn.connectionAccessLevel === 'ReadWrite'))
        };
      }).filter(Boolean) : []; // Remove any null entries

      return processedConnections;
    } catch (error) {
      console.error('Error in getConnections:', error);
      return [];
    }
  }

  static async getConnection(id: number) {
    try {
      const response = await api.get(`/datatransfer/connections/${id}?edit=true`);
      const connectionData = extractFromNestedValues(response.data);
      
      console.log('Raw connection data from API:', connectionData);
      
      // Create a standardized Connection object that matches our database model exactly
      return {
        connectionId: connectionData.ConnectionId || connectionData.connectionId || connectionData.id || 0,
        connectionName: connectionData.ConnectionName || connectionData.connectionName || connectionData.name || '',
        connectionString: connectionData.ConnectionString || connectionData.connectionString || '',
        description: connectionData.Description || connectionData.description || '',
        isActive: connectionData.IsActive !== undefined ? connectionData.IsActive : 
                (connectionData.isActive !== undefined ? connectionData.isActive : true),
        
        // Database connection details
        server: connectionData.Server || connectionData.server || '',
        port: connectionData.Port || connectionData.port || null,
        database: connectionData.Database || connectionData.database || '',
        username: connectionData.Username || connectionData.username || '',
        password: connectionData.Password || connectionData.password || '',
        additionalParameters: connectionData.AdditionalParameters || connectionData.additionalParameters || '',
        
        // Connection settings
        isConnectionValid: connectionData.IsConnectionValid || connectionData.isConnectionValid || false,
        minPoolSize: connectionData.MinPoolSize || connectionData.minPoolSize || null,
        maxPoolSize: connectionData.MaxPoolSize || connectionData.maxPoolSize || null,
        timeout: connectionData.Timeout || connectionData.timeout || null,
        trustServerCertificate: connectionData.TrustServerCertificate !== undefined ? connectionData.TrustServerCertificate :
                              (connectionData.trustServerCertificate !== undefined ? connectionData.trustServerCertificate : true),
        encrypt: connectionData.Encrypt !== undefined ? connectionData.Encrypt : 
               (connectionData.encrypt !== undefined ? connectionData.encrypt : true),
        
        // Metadata
        createdBy: connectionData.CreatedBy || connectionData.createdBy || 'System',
        createdOn: connectionData.CreatedOn || connectionData.createdOn || null,
        lastModifiedBy: connectionData.LastModifiedBy || connectionData.lastModifiedBy || 'System',
        lastModifiedOn: connectionData.LastModifiedOn || connectionData.lastModifiedOn || null,
        lastTestedOn: connectionData.LastTestedOn || connectionData.lastTestedOn || null,
        
        // Access control
        connectionAccessLevel: connectionData.ConnectionAccessLevel || connectionData.connectionAccessLevel || 'ReadOnly',
        
        // Computed properties based on connectionAccessLevel
        isSource: connectionData.IsSource !== undefined ? connectionData.IsSource :
                (connectionData.isSource !== undefined ? connectionData.isSource : 
                 (connectionData.ConnectionAccessLevel === 'ReadOnly' || connectionData.ConnectionAccessLevel === 'ReadWrite' || 
                  connectionData.connectionAccessLevel === 'ReadOnly' || connectionData.connectionAccessLevel === 'ReadWrite')),
        isDestination: connectionData.IsDestination !== undefined ? connectionData.IsDestination :
                     (connectionData.isDestination !== undefined ? connectionData.isDestination : 
                      (connectionData.ConnectionAccessLevel === 'WriteOnly' || connectionData.ConnectionAccessLevel === 'ReadWrite' || 
                       connectionData.connectionAccessLevel === 'WriteOnly' || connectionData.connectionAccessLevel === 'ReadWrite'))
      };
    } catch (error) {
      console.error('Error in getConnection:', error);
      throw error;
    }
  }

  static async saveConnection(connection: Connection) {
    try {
      // Just log the connection data for debugging
      console.log('Saving connection with data:', connection);

      // Both create and update use the same POST endpoint
      const response = await api.post('/datatransfer/connections', connection);
      return response.data;
    } catch (error: any) {
      console.error('Error saving connection:', error);
      throw error;
    }
  }

  static async testConnection(connection: Connection) {
    try {
      // Just log the connection data for debugging
      console.log('Testing connection with data:', connection);

      const response = await api.post('/datatransfer/connections/test', connection);
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
      // Ensure connectionId is valid
      if (!connectionId || connectionId <= 0) {
        console.error('Invalid connectionId provided:', connectionId);
        throw new Error('Connection ID is required and must be valid');
      }
      
      console.log('Fetching database schema with connectionId:', connectionId);
      const response = await api.get(`/datatransfer/schema/databases?connectionId=${connectionId}`);
      return extractFromNestedValues(response.data);
    } catch (error) {
      console.error('Error in getDatabaseSchema:', error);
      throw error;
    }
  }

  static async getDatabaseSchemaByConnectionString(connectionString: string) {
    try {
      const response = await api.post('/datatransfer/schema', { connectionString });
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
      const response = await api.post('/datatransfer/connections/schema', payload, {
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
      const response = await api.get('/datatransfer/configurations');

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
      const response = await api.get(`/datatransfer/configurations/${id}`);
      return extractFromNestedValues(response.data);
    } catch (error) {
      console.error('Error in getConfiguration:', error);
      throw error;
    }
  }

  static async saveConfiguration(configuration: any) {
    if (configuration.configurationId) {
      // Update existing configuration
      const response = await api.put(`/datatransfer/configurations/${configuration.configurationId}`, configuration);
      return response.data;
    } else {
      // Create new configuration
      const response = await api.post('/datatransfer/configurations', configuration);
      return response.data;
    }
  }

  static async deleteConfiguration(id: number) {
    const response = await api.delete(`/datatransfer/configurations/${id}`);
    return response.data;
  }

  static async testConfiguration(id: number) {
    const response = await api.post(`/datatransfer/configurations/${id}/test`);
    return response.data;
  }

  static async executeTransfer(id: number) {
    const response = await api.post(`/datatransfer/configurations/${id}/execute`);
    return response.data;
  }

  // Run history operations
  static async getRunHistory(configurationId: number = 0) {
    try {
      const url = configurationId
        ? `/datatransfer/runs?configurationId=${configurationId}`
        : '/datatransfer/runs';

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
      const response = await api.get(`/datatransfer/runs/${runId}`);
      return extractFromNestedValues(response.data);
    } catch (error) {
      console.error('Error in getRunDetails:', error);
      throw error;
    }
  }

  // Schema operations
  static async saveSchema(schema: any) {
    const response = await api.post('/datatransfer/schema/save', schema);
    return response.data;
  }

  static async getSchemas() {
    try {
      const response = await api.get('/datatransfer/schemas');
      return extractFromNestedValues(response.data);
    } catch (error) {
      console.error('Error in getSchemas:', error);
      return [];
    }
  }

  static async getSchema(id: number) { // This 'id' seems to be a configuration ID not a connection ID
    try {
      const response = await api.get(`/datatransfer/configurations/${id}`);
      return extractFromNestedValues(response.data);
    } catch (error) {
      console.error('Error in getSchema:', error);
      throw error;
    }
  }
}

export default DataTransferService;