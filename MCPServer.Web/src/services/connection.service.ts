import { Connection } from '../pages/transfer/types/Connection';
import api from './api';
import DataTransferUtilsService from './dataTransferUtils.service';

/**
 * Service for managing database connections in the data transfer system
 */
class ConnectionService {
  /**
   * Get all database connections
   */
  static async getConnections() {
    const response = await api.get('/datatransfer/connections');
    const connections = DataTransferUtilsService.extractFromNestedValues(response.data);
    
    if (!Array.isArray(connections)) return [];
    
    // Give each connection a unique ID and ensure all required properties exist
    let uniqueIdCounter = Date.now(); // Use timestamp as starting point for generating unique IDs
    
    return connections.map(conn => {
      // Ensure each connection has a connectionId (either from API or generated)
      const connectionId = conn.ConnectionId !== undefined ? conn.ConnectionId : 
                          (conn.connectionId !== undefined ? conn.connectionId : 
                          -(uniqueIdCounter++)); // Use negative counter for temp IDs
      
      return {
        connectionId: connectionId, // Ensure ID is always present
        connectionName: conn.ConnectionName || conn.connectionName || '',
        connectionString: conn.ConnectionString || conn.connectionString || '',
        connectionAccessLevel: conn.ConnectionAccessLevel || conn.connectionAccessLevel || 'ReadOnly',
        description: conn.Description || conn.description || '',
        server: conn.Server || conn.server || '',
        port: conn.Port !== undefined ? conn.Port : (conn.port !== undefined ? conn.port : null),
        database: conn.Database || conn.database || '',
        username: conn.Username || conn.username || '',
        password: conn.Password || conn.password || '',
        additionalParameters: conn.AdditionalParameters || conn.additionalParameters || '',
        isActive: conn.IsActive !== undefined ? conn.IsActive : (conn.isActive !== undefined ? conn.isActive : true),
        isConnectionValid: conn.IsConnectionValid !== undefined ? conn.IsConnectionValid : (conn.isConnectionValid !== undefined ? conn.isConnectionValid : null), // <--- ENSURED MAPPING
        minPoolSize: conn.MinPoolSize !== undefined ? conn.MinPoolSize : (conn.minPoolSize !== undefined ? conn.minPoolSize : 5),
        maxPoolSize: conn.MaxPoolSize !== undefined ? conn.MaxPoolSize : (conn.maxPoolSize !== undefined ? conn.maxPoolSize : 100),
        timeout: conn.Timeout !== undefined ? conn.Timeout : (conn.timeout !== undefined ? conn.timeout : 30),
        trustServerCertificate: conn.TrustServerCertificate !== undefined ? conn.TrustServerCertificate : 
                               (conn.trustServerCertificate !== undefined ? conn.trustServerCertificate : true),
        encrypt: conn.Encrypt !== undefined ? conn.Encrypt : (conn.encrypt !== undefined ? conn.encrypt : true),
        createdBy: conn.CreatedBy || conn.createdBy || 'System',
        createdOn: conn.CreatedOn || conn.createdOn || null,
        lastModifiedBy: conn.LastModifiedBy || conn.lastModifiedBy || 'System',
        lastModifiedOn: conn.LastModifiedOn || conn.lastModifiedOn || null,
        lastTestedOn: conn.LastTestedOn || conn.lastTestedOn || null,
        
        // Derived properties - handle case where connectionAccessLevel is missing
        isSource: (conn.ConnectionAccessLevel === 'ReadOnly' || conn.ConnectionAccessLevel === 'ReadWrite' || 
                  conn.connectionAccessLevel === 'ReadOnly' || conn.connectionAccessLevel === 'ReadWrite' ||
                  (conn.isSource !== undefined ? conn.isSource : false)),
        isDestination: (conn.ConnectionAccessLevel === 'WriteOnly' || conn.ConnectionAccessLevel === 'ReadWrite' || 
                       conn.connectionAccessLevel === 'WriteOnly' || conn.connectionAccessLevel === 'ReadWrite' ||
                       (conn.isDestination !== undefined ? conn.isDestination : false))
      };
    }).filter(Boolean); // Remove any null values
  }

  /**
   * Get a connection by ID
   */
  static async getConnection(id: number) {
    const response = await api.get(`/datatransfer/connections/${id}?edit=true`);
    const connectionData = DataTransferUtilsService.extractFromNestedValues(response.data);
    
    return {
      connectionId: connectionData.ConnectionId,
      connectionName: connectionData.ConnectionName,
      connectionString: connectionData.ConnectionString,
      connectionAccessLevel: connectionData.ConnectionAccessLevel,
      description: connectionData.Description,
      server: connectionData.Server,
      port: connectionData.Port,
      database: connectionData.Database,
      username: connectionData.Username,
      password: connectionData.Password,
      additionalParameters: connectionData.AdditionalParameters,
      isActive: connectionData.IsActive,
      isConnectionValid: connectionData.IsConnectionValid, // <--- ENSURED MAPPING
      minPoolSize: connectionData.MinPoolSize,
      maxPoolSize: connectionData.MaxPoolSize,
      timeout: connectionData.Timeout,
      trustServerCertificate: connectionData.TrustServerCertificate,
      encrypt: connectionData.Encrypt,
      createdBy: connectionData.CreatedBy,
      createdOn: connectionData.CreatedOn,
      lastModifiedBy: connectionData.LastModifiedBy,
      lastModifiedOn: connectionData.LastModifiedOn,
      lastTestedOn: connectionData.LastTestedOn,
      // Derived properties
      isSource: connectionData.ConnectionAccessLevel === 'ReadOnly' || connectionData.ConnectionAccessLevel === 'ReadWrite',
      isDestination: connectionData.ConnectionAccessLevel === 'WriteOnly' || connectionData.ConnectionAccessLevel === 'ReadWrite'
    };
  }

  /**
   * Save a connection (create or update)
   */
  static async saveConnection(connection: Connection) {
    const response = await api.post('/datatransfer/connections', connection);
    return response.data;
  }

  /**
   * Test a database connection
   */
  static async testConnection(connection: any) {
    try {
      // First check for id, then fallback to connectionId
      const connectionId = connection.id || connection.connectionId;
      
      // If a valid connection ID is provided, use the test by ID endpoint
      if (connectionId && typeof connectionId === 'number' && connectionId > 0) {
        const response = await api.post(`/datatransfer/connections/test/${connectionId}`);
        return {
          success: response.data.isSuccess,
          message: response.data.message,
          serverInfo: response.data.serverInfo,
          version: response.data.version,
          isConnectionValid: response.data.isConnectionValid
        };
      } else {
        // For ad-hoc testing, use the regular test endpoint with full connection details
        const response = await api.post('/datatransfer/connections/test', connection);
        return {
          success: response.data.isSuccess,
          message: response.data.message,
          serverInfo: response.data.serverInfo,
          version: response.data.version,
          isConnectionValid: response.data.isConnectionValid
        };
      }
    } catch (error: any) {
      if (error.response && error.response.data) {
        return {
          success: false,
          message: error.response.data.message || error.response.data.error || 'Connection test failed',
          isConnectionValid: false
        };
      }
      return {
        success: false,
        message: error.message || 'Connection test failed',
        isConnectionValid: false
      };
    }
  }
}

export default ConnectionService;