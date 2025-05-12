import api from './api';
import DataTransferUtilsService from './dataTransferUtils.service';

/**
 * Service for managing data transfer configurations
 */
class ConfigurationService {
  /**
   * Get all data transfer configurations
   */
  static async getConfigurations() {
    const response = await api.get('/datatransfer/configurations');
    const configurations = DataTransferUtilsService.extractFromNestedValues(response.data);
    
    if (!Array.isArray(configurations)) return [];
    
    return configurations.map(config => {
      if (!config) return null;

      // Create a configuration object with proper field names
      const processedConfig: any = {
        configurationId: config.ConfigurationId,
        configurationName: config.ConfigurationName,
        description: config.Description,
        sourceConnectionId: config.SourceConnectionId,
        destinationConnectionId: config.DestinationConnectionId,
        batchSize: config.BatchSize,
        reportingFrequency: config.ReportingFrequency,
        isActive: config.IsActive,
        createdBy: config.CreatedBy,
        createdOn: config.CreatedOn,
        lastModifiedBy: config.LastModifiedBy,
        lastModifiedOn: config.LastModifiedOn
      };

      // Handle nested connections if they exist
      if (config.SourceConnection) {
        processedConfig.sourceConnection = {
          connectionId: config.SourceConnection.ConnectionId,
          connectionName: config.SourceConnection.ConnectionName,
          connectionString: config.SourceConnection.ConnectionString,
          description: config.SourceConnection.Description,
          isActive: config.SourceConnection.IsActive
        };
      }

      if (config.DestinationConnection) {
        processedConfig.destinationConnection = {
          connectionId: config.DestinationConnection.ConnectionId,
          connectionName: config.DestinationConnection.ConnectionName,
          connectionString: config.DestinationConnection.ConnectionString,
          description: config.DestinationConnection.Description,
          isActive: config.DestinationConnection.IsActive
        };
      }

      // Process table mappings array
      if (config.TableMappings && Array.isArray(config.TableMappings)) {
        processedConfig.tableMappings = config.TableMappings.map((mapping: any) => ({
          tableMappingId: mapping.TableMappingId,
          sourceTable: mapping.SourceTable,
          destinationTable: mapping.DestinationTable,
          isActive: mapping.IsActive
        }));
      } else {
        processedConfig.tableMappings = [];
      }

      return processedConfig;
    }).filter(Boolean);
  }

  /**
   * Get a specific configuration by ID
   */
  static async getConfiguration(id: number) {
    const response = await api.get(`/datatransfer/configurations/${id}`);
    return DataTransferUtilsService.extractFromNestedValues(response.data);
  }

  /**
   * Save a configuration (create or update)
   */
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

  /**
   * Delete a configuration
   */
  static async deleteConfiguration(id: number) {
    const response = await api.delete(`/datatransfer/configurations/${id}`);
    return response.data;
  }

  /**
   * Test a configuration
   */
  static async testConfiguration(id: number) {
    const response = await api.post(`/datatransfer/configurations/${id}/test`);
    return response.data;
  }

  /**
   * Execute a data transfer operation for a configuration
   */
  static async executeTransfer(id: number) {
    const response = await api.post(`/datatransfer/configurations/${id}/execute`);
    return response.data;
  }
}

export default ConfigurationService;