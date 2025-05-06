import axios from '@/lib/axios';

// Mock data for development and testing when API is unavailable
const MOCK_DATA = {
  connections: {
    $values: [
      {
        connectionId: 1,
        connectionName: "DailyActionsDB",
        connectionString: "Server=localhost;Database=DailyActionsDB;User ID=sa;Password=********;",
        description: "Daily actions database",
        isSource: true,
        isDestination: false,
        isActive: true
      },
      {
        connectionId: 13,
        connectionName: "Test Connection",
        connectionString: "Server=localhost;Database=TestDB;User ID=testuser;Password=********;",
        description: "Test connection for development",
        isSource: true,
        isDestination: true,
        isActive: true
      }
    ]
  },
  configurations: {
    $values: [
      {
        configurationId: 1,
        configurationName: "Daily Actions Transfer",
        description: "Transfer daily actions to reporting database",
        sourceConnection: {
          connectionId: 1,
          connectionName: "DailyActionsDB",
          connectionString: "Server=localhost;Database=DailyActionsDB;User ID=sa;Password=********;",
          description: "Daily actions database",
          isSource: true,
          isDestination: false,
          isActive: true
        },
        destinationConnection: {
          connectionId: 13,
          connectionName: "Test Connection",
          connectionString: "Server=localhost;Database=TestDB;User ID=testuser;Password=********;",
          description: "Test connection for development",
          isSource: true,
          isDestination: true,
          isActive: true
        },
        tableMappings: [
          {
            sourceTable: "Actions",
            destinationTable: "DailyActions",
            isActive: true
          }
        ],
        isActive: true,
        schedule: "0 0 * * *",
        lastRunTime: "2023-06-15T12:00:00Z"
      }
    ]
  },
  runHistory: {
    $values: [
      {
        runId: 1,
        configurationId: 1,
        configurationName: "Daily Actions Transfer",
        startTime: "2023-06-15T12:00:00Z",
        endTime: "2023-06-15T12:05:30Z",
        status: "Completed",
        rowsTransferred: 1250,
        errorMessage: null
      }
    ]
  },
  runDetails: {
    runId: 1,
    configurationId: 1,
    configurationName: "Daily Actions Transfer",
    startTime: "2023-06-15T12:00:00Z",
    endTime: "2023-06-15T12:05:30Z",
    status: "Completed",
    rowsTransferred: 1250,
    errorMessage: null,
    tableMappings: [
      {
        sourceTable: "Actions",
        destinationTable: "DailyActions",
        rowsTransferred: 1250,
        errorMessage: null
      }
    ]
  }
};

// Flag to determine if we should use mock data
const USE_MOCK_DATA = false;

class DataTransferService {
  // Connection management
  async getConnections() {
    try {
      const response = await axios.get('data-transfer/connections');
      console.log('Raw connections response:', response.data);

      // If the response doesn't have a $values property, wrap it in one
      if (response.data && !response.data.$values) {
        return { $values: [] };
      }

      // If $values is null or undefined, return an empty array
      if (!response.data.$values) {
        return { $values: [] };
      }

      // Handle nested $values structure
      if (response.data.$values && response.data.$values.$values && Array.isArray(response.data.$values.$values)) {
        console.log('Found nested $values structure, extracting inner array:', response.data.$values.$values);
        return { $values: response.data.$values.$values };
      }

      // Ensure $values is an array
      if (!Array.isArray(response.data.$values)) {
        console.warn('$values is not an array, converting to array:', response.data.$values);
        return { $values: [] };
      }

      // Process each connection to handle hashed connection strings
      if (Array.isArray(response.data.$values)) {
        response.data.$values = response.data.$values.map((connection: any) => {
          // If the connection string starts with "HASHED:", set connectionStringForDisplay
          if (connection.connectionString && connection.connectionString.startsWith('HASHED:')) {
            // Extract server and database info from the hashed string
            const serverMatch = connection.connectionString.match(/Server=([^;]+)/i);
            const databaseMatch = connection.connectionString.match(/Database=([^;]+)/i);

            const server = serverMatch ? serverMatch[1] : 'unknown';
            const database = databaseMatch ? databaseMatch[1] : 'unknown';

            // Create a dummy connection string for display
            connection.connectionStringForDisplay = `Server=${server};Database=${database};User ID=********;Password=********;`;

            // Create connection details object
            connection.connectionDetails = {
              server,
              database,
              username: '********', // Masked for security
              password: '********', // Masked for security
            };
          }

          return connection;
        });
      }

      return response.data;
    } catch (error) {
      console.error('Error fetching connections:', error);
      // Return empty array in case of error
      return { $values: [] };
    }
  }

  async getConnection(id: number) {
    try {
      const response = await axios.get(`data-transfer/connections/${id}`);

      // Process the connection to handle hashed connection string
      if (response.data && response.data.connectionString && response.data.connectionString.startsWith('HASHED:')) {
        // Extract server and database info from the hashed string
        const serverMatch = response.data.connectionString.match(/Server=([^;]+)/i);
        const databaseMatch = response.data.connectionString.match(/Database=([^;]+)/i);

        const server = serverMatch ? serverMatch[1] : 'unknown';
        const database = databaseMatch ? databaseMatch[1] : 'unknown';

        // Create a dummy connection string for display
        response.data.connectionStringForDisplay = `Server=${server};Database=${database};User ID=********;Password=********;`;

        // Create connection details object
        response.data.connectionDetails = {
          server,
          database,
          username: '********', // Masked for security
          password: '********', // Masked for security
        };
      }

      return response.data;
    } catch (error) {
      console.warn(`Using mock data for connection ${id} due to API error:`, error);
      if (USE_MOCK_DATA) {
        const connection = MOCK_DATA.connections.$values.find(c => c.connectionId === id);
        return connection || null;
      }
      return null;
    }
  }

  async saveConnection(connection: any) {
    try {
      // Ensure we have a valid connection object
      if (!connection || !connection.connectionName || !connection.connectionString) {
        throw new Error('Invalid connection data: Connection name and connection string are required');
      }

      // Make sure connection purpose is set
      if (connection.isSource === undefined && connection.isDestination === undefined) {
        connection.isSource = true;
        connection.isDestination = true;
      }

      // Set default values if not provided
      connection.isActive = connection.isActive !== undefined ? connection.isActive : true;
      connection.maxPoolSize = connection.maxPoolSize || 100;
      connection.minPoolSize = connection.minPoolSize || 5;

      // Hash the connection string for security
      // Note: The actual hashing is done on the server side, but we'll mark it here
      console.log('Connection string will be hashed on the server side for security');

      // Format the connection data correctly for the API
      const formattedConnection = { ...connection };

      // Convert string enum values to numeric enum values
      if (typeof formattedConnection.connectionAccessLevel === 'string') {
        switch (formattedConnection.connectionAccessLevel) {
          case 'ReadOnly':
            formattedConnection.connectionAccessLevel = 0;
            break;
          case 'WriteOnly':
            formattedConnection.connectionAccessLevel = 1;
            break;
          case 'ReadWrite':
            formattedConnection.connectionAccessLevel = 2;
            break;
          default:
            // Default to ReadWrite if invalid value
            formattedConnection.connectionAccessLevel = 2;
        }
      }

      // Ensure required fields are present
      if (!formattedConnection.lastModifiedBy) {
        formattedConnection.lastModifiedBy = "System";
      }

      if (!formattedConnection.createdBy) {
        formattedConnection.createdBy = "System";
      }

      if (!formattedConnection.createdOn) {
        formattedConnection.createdOn = new Date().toISOString();
      }

      if (!formattedConnection.lastModifiedOn) {
        formattedConnection.lastModifiedOn = new Date().toISOString();
      }

      console.log('Saving connection:', formattedConnection);

      // Always use POST for both creating and updating connections
      // The API uses the connectionId to determine if it's an update or create
      const response = await axios.post('data-transfer/connections', formattedConnection);
      return response.data;
    } catch (error: any) {
      console.error('Error saving connection:', error);

      // Check if we have a structured error response from the API
      if (error.response) {
        // If it's a conflict (409) with an existing connection, pass through the response
        if (error.response.status === 409) {
          throw error;
        }

        // For other error responses, extract the error message
        const errorMessage = error.response.data?.message ||
                            error.response.data?.error ||
                            'Failed to save connection';

        const enhancedError = new Error(errorMessage);
        enhancedError.name = 'SaveConnectionError';
        throw enhancedError;
      }

      // If using mock data is enabled, simulate saving
      if (USE_MOCK_DATA) {
        console.warn('Using mock data for saving connection');
        if (connection.connectionId) {
          // Update existing
          const index = MOCK_DATA.connections.$values.findIndex(c => c.connectionId === connection.connectionId);
          if (index >= 0) {
            MOCK_DATA.connections.$values[index] = { ...connection };
          }
        } else {
          // Add new with generated ID
          const newId = Math.max(...MOCK_DATA.connections.$values.map(c => c.connectionId), 0) + 1;
          const newConnection = { ...connection, connectionId: newId };
          MOCK_DATA.connections.$values.push(newConnection);
          return newConnection;
        }
        return connection;
      }

      // For network or other errors
      throw new Error(`Failed to save connection: ${error.message}`);
    }
  }

  async deleteConnection(id: number) {
    try {
      const response = await axios.delete(`data-transfer/connections/${id}`);
      return response.data;
    } catch (error) {
      console.warn(`Using mock data for deleting connection ${id} due to API error:`, error);
      if (USE_MOCK_DATA) {
        // Simulate deletion by removing from mock data
        const index = MOCK_DATA.connections.$values.findIndex(c => c.connectionId === id);
        if (index >= 0) {
          MOCK_DATA.connections.$values.splice(index, 1);
        }
        return { success: true };
      }
      throw error;
    }
  }

  async testConnection(data: any) {
    // Check if we're receiving the new structure with a connection property
    // or the old structure with connection properties directly
    const connectionData = data.connection || data;

    try {
      // Ensure connectionAccessLevel is properly formatted as an enum value (0, 1, 2)
      // instead of a string ('ReadOnly', 'WriteOnly', 'ReadWrite')
      const formattedConnection = { ...connectionData };

      // Convert string enum values to numeric enum values
      if (typeof formattedConnection.connectionAccessLevel === 'string') {
        switch (formattedConnection.connectionAccessLevel) {
          case 'ReadOnly':
            formattedConnection.connectionAccessLevel = 0;
            break;
          case 'WriteOnly':
            formattedConnection.connectionAccessLevel = 1;
            break;
          case 'ReadWrite':
            formattedConnection.connectionAccessLevel = 2;
            break;
          default:
            // Default to ReadWrite if invalid value
            formattedConnection.connectionAccessLevel = 2;
        }
      }

      // Ensure required fields are present
      if (!formattedConnection.lastModifiedBy) {
        formattedConnection.lastModifiedBy = "System";
      }

      if (!formattedConnection.createdBy) {
        formattedConnection.createdBy = "System";
      }

      console.log('Sending connection test request with data:', formattedConnection);
      const response = await axios.post('data-transfer/connections/test', formattedConnection);

      // Ensure we return a properly formatted response
      if (response && response.data) {
        return {
          success: response.data.success || false,
          message: response.data.message || 'Connection test completed',
          server: response.data.server || '',
          database: response.data.database || '',
          testQueryResult: response.data.testQueryResult || 0
        };
      } else {
        return {
          success: false,
          message: 'Invalid response from server',
          server: '',
          database: '',
          testQueryResult: 0
        };
      }
    } catch (error: any) {
      console.error('Connection test error:', error);

      // Log detailed error information to console for debugging
      if (error.response) {
        console.error('Response status:', error.response.status);
        console.error('Response data:', error.response.data);
      }

      // Check if we have a structured error response from the API
      if (error.response && error.response.data) {
        const errorData = error.response.data;

        // Extract connection details if available
        const connectionDetails = errorData.connectionDetails || {};

        return {
          success: false,
          message: errorData.message || errorData.error || 'Connection failed',
          detailedError: errorData.detailedError || errorData.error || error.message,
          server: connectionDetails.server || errorData.server || '',
          database: connectionDetails.database || '',
          errorCode: errorData.errorCode,
          errorType: errorData.exceptionType,
          innerException: errorData.innerException,
          error: errorData.detailedError || errorData.error || error.message
        };
      }

      // If using mock data is enabled, return mock success
      if (USE_MOCK_DATA) {
        console.warn('Using mock data for testing connection');
        return {
          success: true,
          message: "Connection successful (mock)",
          server: connectionData.connectionString?.split(';').find((s: string) => s.startsWith('Server='))?.split('=')[1] || 'localhost',
          database: connectionData.connectionString?.split(';').find((s: string) => s.startsWith('Database='))?.split('=')[1] || 'TestDB',
          testQueryResult: 1
        };
      }

      // Return a formatted error response for other types of errors
      return {
        success: false,
        message: error.message || 'Connection test failed',
        detailedError: error.message,
        server: '',
        database: '',
        errorType: error.name,
        error: error.message
      };
    }
  }

  // Configuration management
  async getConfigurations() {
    try {
      const response = await axios.get('data-transfer/configurations');
      console.log('Raw configurations response:', response.data);

      // If the response doesn't have a $values property, wrap it in one
      if (response.data && !response.data.$values) {
        return { $values: [] };
      }

      // If $values is null or undefined, return an empty array
      if (!response.data.$values) {
        return { $values: [] };
      }

      // Handle nested $values structure
      if (response.data.$values && response.data.$values.$values && Array.isArray(response.data.$values.$values)) {
        console.log('Found nested $values structure, extracting inner array:', response.data.$values.$values);
        return { $values: response.data.$values.$values };
      }

      // Ensure $values is an array
      if (!Array.isArray(response.data.$values)) {
        console.warn('$values is not an array, converting to array:', response.data.$values);
        return { $values: [] };
      }

      return response.data;
    } catch (error) {
      console.error('Error fetching configurations:', error);
      // Return empty array in case of error
      return { $values: [] };
    }
  }

  async getConfiguration(id: number) {
    try {
      const response = await axios.get(`data-transfer/configurations/${id}`);
      return response.data;
    } catch (error) {
      console.warn(`Using mock data for configuration ${id} due to API error:`, error);
      if (USE_MOCK_DATA) {
        const config = MOCK_DATA.configurations.$values.find(c => c.configurationId === id);
        return config || null;
      }
      return null;
    }
  }

  async saveConfiguration(configuration: any) {
    try {
      // Always use POST for both creating and updating configurations
      // The API uses the configurationId to determine if it's an update or create
      const response = await axios.post('data-transfer/configurations', configuration);
      return response.data;
    } catch (error) {
      console.warn('Using mock data for saving configuration due to API error:', error);
      if (USE_MOCK_DATA) {
        // Simulate saving by adding to mock data
        if (configuration.configurationId) {
          // Update existing
          const index = MOCK_DATA.configurations.$values.findIndex(c => c.configurationId === configuration.configurationId);
          if (index >= 0) {
            MOCK_DATA.configurations.$values[index] = { ...configuration };
          }
        } else {
          // Add new with generated ID
          const newId = Math.max(...MOCK_DATA.configurations.$values.map(c => c.configurationId), 0) + 1;
          const newConfig = { ...configuration, configurationId: newId };
          MOCK_DATA.configurations.$values.push(newConfig);
          return newConfig;
        }
        return configuration;
      }
      throw error;
    }
  }

  async deleteConfiguration(id: number) {
    try {
      const response = await axios.delete(`data-transfer/configurations/${id}`);
      return response.data;
    } catch (error) {
      console.warn(`Using mock data for deleting configuration ${id} due to API error:`, error);
      if (USE_MOCK_DATA) {
        // Simulate deletion by removing from mock data
        const index = MOCK_DATA.configurations.$values.findIndex(c => c.configurationId === id);
        if (index >= 0) {
          MOCK_DATA.configurations.$values.splice(index, 1);
        }
        return { success: true };
      }
      throw error;
    }
  }

  // Run management
  async executeTransfer(configurationId: number) {
    try {
      const response = await axios.post(`data-transfer/configurations/${configurationId}/execute`);
      return response.data;
    } catch (error) {
      console.error(`Error executing transfer ${configurationId}:`, error);
      throw error;
    }
  }

  async testConfiguration(configurationId: number) {
    try {
      const response = await axios.post(`data-transfer/configurations/${configurationId}/test`);
      return response.data;
    } catch (error) {
      console.error(`Error testing configuration ${configurationId}:`, error);
      throw error;
    }
  }

  async getRunHistory(configurationId = 0, limit = 50) {
    try {
      const response = await axios.get(`data-transfer/history?configurationId=${configurationId}&limit=${limit}`);
      console.log('Raw run history response:', response.data);

      // If the response doesn't have a $values property, wrap it in one
      if (response.data && !response.data.$values) {
        return { $values: [] };
      }

      // If $values is null or undefined, return an empty array
      if (!response.data.$values) {
        return { $values: [] };
      }

      // Handle nested $values structure
      if (response.data.$values && response.data.$values.$values && Array.isArray(response.data.$values.$values)) {
        console.log('Found nested $values structure, extracting inner array:', response.data.$values.$values);
        return { $values: response.data.$values.$values };
      }

      // Ensure $values is an array
      if (!Array.isArray(response.data.$values)) {
        console.warn('$values is not an array, converting to array:', response.data.$values);
        return { $values: [] };
      }

      return response.data;
    } catch (error) {
      console.error(`Error fetching run history (configId: ${configurationId}):`, error);
      // Return empty array in case of error
      return { $values: [] };
    }
  }

  async getRunDetails(runId: number) {
    try {
      const response = await axios.get(`data-transfer/runs/${runId}`);
      return response.data;
    } catch (error) {
      console.warn(`Using mock data for run details ${runId} due to API error:`, error);
      if (USE_MOCK_DATA) {
        if (runId === 1) {
          return MOCK_DATA.runDetails;
        }

        // For other run IDs, generate mock details based on run history
        const run = MOCK_DATA.runHistory.$values.find(r => r.runId === runId);
        if (run) {
          return {
            ...run,
            tableMappings: [
              {
                sourceTable: "Table1",
                destinationTable: "DestTable1",
                rowsTransferred: run.rowsTransferred,
                errorMessage: null
              }
            ]
          };
        }
        return null;
      }
      return null;
    }
  }
}

export default new DataTransferService();