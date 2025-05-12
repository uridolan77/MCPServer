import api from './api';
import DataTransferUtilsService from './dataTransferUtils.service';

/**
 * Service for working with database schemas in the data transfer system
 */
class SchemaService {
  /**
   * Get database schema for a connection by ID
   */
  static async getDatabaseSchema(connectionId: number) {
    const response = await api.get(`/datatransfer/schema/databases?connectionId=${connectionId}`);
    return DataTransferUtilsService.extractFromNestedValues(response.data);
  }

  /**
   * Get database schema using a connection string
   */
  static async getDatabaseSchemaByConnectionString(connectionString: string) {
    const response = await api.post('/datatransfer/schema', { connectionString });
    return DataTransferUtilsService.extractFromNestedValues(response.data);
  }

  /**
   * Fetch database schema using connection data object
   */
  static async fetchDatabaseSchema(connectionData: any) {
    // Prepare connection data with defaults for missing properties
    const payload = {
      ...connectionData,
      connectionName: connectionData.connectionName || 'Temporary Connection',
      isActive: connectionData.isActive !== false,
      maxPoolSize: connectionData.maxPoolSize || 100,
      minPoolSize: connectionData.minPoolSize || 5,
      timeout: connectionData.timeout || 30,
      encrypt: connectionData.encrypt !== false,
      trustServerCertificate: connectionData.trustServerCertificate !== false,
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
    }
    
    // Send the request to the API with increased timeout
    const response = await api.post('/datatransfer/connections/schema', payload, {
      timeout: 60000 // 60 seconds for schema retrieval
    });
    
    return DataTransferUtilsService.extractFromNestedValues(response.data);
  }

  /**
   * Save a database schema
   */
  static async saveSchema(schema: any) {
    const response = await api.post('/datatransfer/schema/save', schema);
    return response.data;
  }

  /**
   * Get all saved schemas
   */
  static async getSchemas() {
    const response = await api.get('/datatransfer/schemas');
    return DataTransferUtilsService.extractFromNestedValues(response.data);
  }

  /**
   * Get a specific schema by ID
   */
  static async getSchema(id: number) {
    const response = await api.get(`/datatransfer/configurations/${id}`);
    return DataTransferUtilsService.extractFromNestedValues(response.data);
  }
}

export default SchemaService;