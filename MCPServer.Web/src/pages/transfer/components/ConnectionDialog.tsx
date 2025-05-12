import React, { useState, useEffect } from 'react';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  Grid,
  CircularProgress,
  Tabs,
  Tab
} from '@mui/material';
import ConnectionService from '@/services/connection.service';
import SchemaService from '@/services/schema.service';

// Import the new component modules
import ConnectionBasicInfo from './ConnectionBasicInfo';
import ConnectionStringForm from './ConnectionStringForm';
import ConnectionDetailsForm from './ConnectionDetailsForm';
import TestConnectionSection from './TestConnectionSection';
import ConnectionSettings from './ConnectionSettings';
import DatabaseSchemaDialog from './DatabaseSchemaDialog';

// Import the shared Connection interface
import { Connection } from '../types/Connection';

export default function ConnectionDialog({ open, connection, onClose, onSave }: ConnectionDialogProps) {
  const [connectionDetails, setConnectionDetails] = useState<Connection>({
    connectionId: 0,
    connectionName: '',
    connectionString: '',
    connectionAccessLevel: 'ReadOnly',
    description: '',
    server: '',
    port: null,
    database: '',
    username: '',
    password: '',
    additionalParameters: '',
    isActive: true,
    isConnectionValid: null,
    minPoolSize: 5,
    maxPoolSize: 100,
    timeout: 30,
    trustServerCertificate: true,
    encrypt: true,
    createdBy: "System",
    createdOn: new Date().toISOString(),
    lastModifiedBy: "System",
    lastModifiedOn: new Date().toISOString(),
    lastTestedOn: null,
    isSource: true,
    isDestination: false
  });

  const [connectionMode, setConnectionMode] = useState<'string' | 'details'>('string');
  const [isTesting, setIsTesting] = useState(false);
  const [testResult, setTestResult] = useState<null | {
    success: boolean;
    message: string;
    detailedError?: string;
    server?: string;
    database?: string;
    errorCode?: number;
    errorType?: string;
    innerException?: string;
  }>(null);
  const [connectionTested, setConnectionTested] = useState(false);
  const [isLoadingSchema, setIsLoadingSchema] = useState(false);
  const [dbSchema, setDbSchema] = useState<any[]>([]);
  const [schemaDialogOpen, setSchemaDialogOpen] = useState(false);

  useEffect(() => {
    if (connection) {
      console.log('Loading connection for edit:', connection);
      
      let accessLevel = connection.connectionAccessLevel || 'ReadOnly';
      if (!connection.connectionAccessLevel) {
        if (connection.isSource && connection.isDestination) {
          accessLevel = 'ReadWrite';
        } else if (connection.isSource) {
          accessLevel = 'ReadOnly';
        } else if (connection.isDestination) {
          accessLevel = 'WriteOnly';
        }
      }

      // Set connection details using the flattened model
      setConnectionDetails({
        connectionId: connection.connectionId || 0,
        connectionName: connection.connectionName || '',
        connectionString: connection.connectionString || '',
        connectionAccessLevel: accessLevel,
        description: connection.description || '',
        server: connection.server || '',
        database: connection.database || '',
        username: connection.username || '',
        password: connection.password || '',
        port: connection.port !== undefined ? connection.port : null,
        additionalParameters: connection.additionalParameters || '',
        isActive: connection.isActive !== undefined ? connection.isActive : true,
        isConnectionValid: connection.isConnectionValid || null,
        minPoolSize: connection.minPoolSize || 5,
        maxPoolSize: connection.maxPoolSize || 100,
        timeout: connection.timeout || 30,
        trustServerCertificate: connection.trustServerCertificate !== undefined ? connection.trustServerCertificate : true,
        encrypt: connection.encrypt !== undefined ? connection.encrypt : true,
        createdBy: connection.createdBy || "System",
        createdOn: connection.createdOn || new Date().toISOString(),
        lastModifiedBy: connection.lastModifiedBy || "System",
        lastModifiedOn: connection.lastModifiedOn || new Date().toISOString(),
        lastTestedOn: connection.lastTestedOn ? new Date(connection.lastTestedOn).toISOString() : null,
        // Computed properties based on connectionAccessLevel
        isSource: accessLevel === 'ReadOnly' || accessLevel === 'ReadWrite',
        isDestination: accessLevel === 'WriteOnly' || accessLevel === 'ReadWrite'
      });

      // Determine connection mode based on available information
      if (connection.server) {
        setConnectionMode('details');
      } else if (connection.connectionString) {
        parseConnectionString(connection.connectionString);
      }

      // Set connection test status
      if (connection.lastTestedOn && connection.isActive) {
        setConnectionTested(true);
      } else {
        setTestResult(null);
        setConnectionTested(false);
      }
    } else {
      // Reset connection details for new connections
      setConnectionDetails({
        connectionId: 0,
        connectionName: '',
        connectionString: '',
        connectionAccessLevel: 'ReadOnly',
        description: '',
        server: '',
        port: null,
        database: '',
        username: '',
        password: '',
        additionalParameters: '',
        isActive: true,
        isConnectionValid: null,
        minPoolSize: 5,
        maxPoolSize: 100,
        timeout: 30,
        trustServerCertificate: true,
        encrypt: true,
        createdBy: "System",
        createdOn: new Date().toISOString(),
        lastModifiedBy: "System",
        lastModifiedOn: new Date().toISOString(),
        lastTestedOn: null,
        isSource: true,
        isDestination: false
      });

      setTestResult(null);
      setConnectionTested(false);
    }
  }, [connection, open]);

  // Helper functions
  const parseConnectionString = (connectionString: string) => {
    try {
      const details = { ...connectionDetails };

      const params = connectionString.split(';');
      params.forEach(param => {
        const [key, value] = param.split('=');
        if (!key || !value) return;

        const keyLower = key.trim().toLowerCase();
        const valueClean = value.trim();

        if (keyLower === 'server' || keyLower === 'data source') {
          details.server = valueClean;
        } else if (keyLower === 'database' || keyLower === 'initial catalog') {
          details.database = valueClean;
        } else if (keyLower === 'user id' || keyLower === 'uid') {
          details.username = valueClean;
        } else if (keyLower === 'password' || keyLower === 'pwd') {
          details.password = valueClean;
        } else if (keyLower === 'port') {
          details.port = Number(valueClean);
        }
      });

      setConnectionDetails(details);
      setConnectionMode('details');
    } catch (error) {
      console.error('Error parsing connection string:', error);
      setConnectionMode('string');
    }
  };

  const buildConnectionString = () => {
    if (connectionMode === 'string') {
      // Just return the connection string as is
      return connectionDetails.connectionString;
    }

    // For details mode, build the connection string with all parameters
    const { server, database, username, password, port, additionalParameters } = connectionDetails;
    let connectionString = '';

    const connectionType = 'sqlServer'; // Default to SQL Server for now

    if (connectionType === 'sqlServer') {
      // Always include username and password
      connectionString = `Server=${server}${port ? ',' + port : ''};Database=${database};User ID=${username};Password=${password};`;

      if (connectionDetails.encrypt) {
        connectionString += 'Encrypt=True;';
      }

      if (connectionDetails.trustServerCertificate) {
        connectionString += 'TrustServerCertificate=True;';
      }

      connectionString += `Connection Timeout=${connectionDetails.timeout};`;

      if (additionalParameters) {
        connectionString += additionalParameters;
      }
    } else if (connectionType === 'mysql') {
      // Always include username and password
      connectionString = `Server=${server};Database=${database};User ID=${username};Password=${password};`;

      if (port) {
        connectionString += `Port=${port};`;
      }

      connectionString += `Connection Timeout=${connectionDetails.timeout};`;

      if (additionalParameters) {
        connectionString += additionalParameters;
      }
    }

    console.log('Built connection string:', connectionString);
    return connectionString;
  };

  // Event handlers
  const handleConnectionDetailsChange = (e: React.ChangeEvent<HTMLInputElement | { name?: string; value: unknown }>) => {
    const { name, value, checked, type } = e.target as any;

    if (!name) return;

    setConnectionDetails(prev => ({
      ...prev,
      [name]: type === 'checkbox' ? checked : value
    }));

    if (name === 'connectionString') {
      setTestResult(null);
      setConnectionTested(false);
    }
  };

  const handleConnectionModeChange = (_event: React.SyntheticEvent, newValue: 'string' | 'details') => {
    setConnectionMode(newValue);

    if (newValue === 'details' && connectionDetails.connectionString) {
      parseConnectionString(connectionDetails.connectionString);
    } else if (newValue === 'string' && connectionDetails.server) {
      const connectionString = buildConnectionString();
      setConnectionDetails(prev => ({
        ...prev,
        connectionString
      }));
    }

    setTestResult(null);
    setConnectionTested(false);
  };

  const handleConnectionPurposeChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const value = e.target.value;

    if (value === 'source') {
      setConnectionDetails(prev => ({
        ...prev,
        isSource: true,
        isDestination: false,
        connectionAccessLevel: 'ReadOnly'
      }));
    } else if (value === 'destination') {
      setConnectionDetails(prev => ({
        ...prev,
        isSource: false,
        isDestination: true,
        connectionAccessLevel: 'WriteOnly'
      }));
    } else if (value === 'both') {
      setConnectionDetails(prev => ({
        ...prev,
        isSource: true,
        isDestination: true,
        connectionAccessLevel: 'ReadWrite'
      }));
    }
  };

  const handleTestConnection = async () => {
    let connectionString = '';

    if (connectionMode === 'string') {
      // For connection string mode, ensure username and password are included
      connectionString = connectionDetails.connectionString;

      // Make sure username and password are in the connection string
      if (connectionDetails.username && connectionDetails.password) {
        // Check if they're already in the string
        const hasUserId = /User ID=/i.test(connectionString);
        const hasPassword = /Password=/i.test(connectionString);

        // If not, add them
        if (!hasUserId) {
          connectionString += `;User ID=${connectionDetails.username}`;
        }
        if (!hasPassword) {
          connectionString += `;Password=${connectionDetails.password}`;
        }
      }
    } else {
      connectionString = buildConnectionString();
    }

    if (!connectionString) {
      setTestResult({
        success: false,
        message: connectionMode === 'string'
          ? 'Connection string is required'
          : 'Server, database, username, and password are required'
      });
      return;
    }

    setIsTesting(true);
    setTestResult(null);

    try {
      let connectionAccessLevelValue: number;

      switch (connectionDetails.connectionAccessLevel) {
        case 'ReadOnly':
          connectionAccessLevelValue = 0;
          break;
        case 'WriteOnly':
          connectionAccessLevelValue = 1;
          break;
        case 'ReadWrite':
          connectionAccessLevelValue = 2;
          break;
        default:
          connectionAccessLevelValue = 0;
      }

      // Log the username and password for debugging
      console.log('Testing connection with username:', connectionDetails.username);
      console.log('Testing connection with password:', connectionDetails.password);
      console.log('Testing connection string:', connectionString);

      const connectionData = {
        connectionId: connectionDetails.connectionId,
        connectionName: connectionDetails.connectionName,
        connectionString: connectionString,
        description: connectionDetails.description,
        connectionAccessLevel: connectionAccessLevelValue,
        isActive: connectionDetails.isActive,
        maxPoolSize: connectionDetails.maxPoolSize,
        minPoolSize: connectionDetails.minPoolSize,
        timeout: connectionDetails.timeout,
        encrypt: connectionDetails.encrypt,
        trustServerCertificate: connectionDetails.trustServerCertificate,
        // Include username and password directly
        username: connectionDetails.username,
        password: connectionDetails.password,
        createdBy: connectionDetails.createdBy,
        lastModifiedBy: connectionDetails.lastModifiedBy,
        createdOn: connectionDetails.createdOn,
        lastModifiedOn: connectionDetails.lastModifiedOn
      };

      const response = await ConnectionService.testConnection(connectionData);

      if (response && response.success) {
        setTestResult({
          success: true,
          message: response.message || 'Connection successful',
          server: response.server || '',
          database: response.database || ''
        });

        if (connectionMode === 'details') {
          setConnectionDetails(prev => ({
            ...prev,
            connectionString
          }));
        }

        const now = new Date();
        setConnectionDetails(prev => ({
          ...prev,
          lastTestedOn: now.toISOString(),
          isActive: true
        }));

        setConnectionTested(true);
      } else {
        setTestResult({
          success: false,
          message: response?.message || 'Connection test failed with unknown error',
          detailedError: response?.detailedError || response?.error,
          server: response?.server || '',
          database: response?.database || '',
          errorCode: response?.errorCode,
          errorType: response?.errorType,
          innerException: response?.innerException
        });

        setConnectionTested(false);
      }
    } catch (error: any) {
      console.error('Connection test error:', error);

      let errorMessage = 'Connection test failed';
      let detailedError = '';

      if (error.response && error.response.data) {
        const errorData = error.response.data;
        errorMessage = errorData.message || errorData.error || 'Connection test failed';
        detailedError = errorData.detailedError || errorData.error || error.message;

        setTestResult({
          success: false,
          message: errorMessage,
          detailedError: detailedError,
          server: errorData.connectionDetails?.server || errorData.server || '',
          database: errorData.connectionDetails?.database || '',
          errorCode: errorData.errorCode,
          errorType: errorData.exceptionType,
          innerException: errorData.innerException
        });
      } else {
        errorMessage = error.message || 'Connection test failed';
        detailedError = error.stack || '';

        setTestResult({
          success: false,
          message: errorMessage,
          detailedError: detailedError,
          errorType: error.name
        });
      }

      setConnectionTested(false);
    } finally {
      setIsTesting(false);
    }
  };

  const fetchDatabaseSchema = async () => {
    try {
      setIsLoadingSchema(true);
      console.log('Fetching database schema...');

      // Use the same connection string that was used for the successful test
      let connectionStr = '';
      if (connectionMode === 'string') {
        connectionStr = connectionDetails.connectionString;
      } else {
        connectionStr = buildConnectionString();
      }

      // Log the username and password for debugging
      console.log('Fetching schema with username:', connectionDetails.username);
      console.log('Fetching schema with password:', connectionDetails.password);
      console.log('Fetching schema with connection string:', connectionStr);

      const connectionData = {
        connectionId: connectionDetails.connectionId,
        connectionName: connectionDetails.connectionName,
        connectionString: connectionStr,
        connectionAccessLevel: connectionDetails.connectionAccessLevel,
        // Include username and password directly
        username: connectionDetails.username,
        password: connectionDetails.password,
        // Include these additional parameters that are used during successful test connection
        encrypt: connectionDetails.encrypt,
        trustServerCertificate: connectionDetails.trustServerCertificate,
        timeout: connectionDetails.timeout,
        maxPoolSize: connectionDetails.maxPoolSize,
        minPoolSize: connectionDetails.minPoolSize
      };

      console.log('Sending schema request for database:', connectionDetails.database || 'Unknown');
      const response = await SchemaService.fetchDatabaseSchema(connectionData);
      console.log('Schema response received:', response);

      if (response && response.success) {
        console.log('Schema fetch successful, found items in schema response');

        // Handle case where schema is returned directly or nested in a data property
        const schemaData = response.schema || response.data?.schema || [];

        // If it's array-like, use it directly; otherwise, try to extract from object structure
        const formattedSchema = Array.isArray(schemaData)
          ? schemaData
          : typeof schemaData === 'object' && schemaData !== null
            ? Object.values(schemaData)
            : [];

        console.log('Formatted schema data:', formattedSchema);
        setDbSchema(formattedSchema);
        setSchemaDialogOpen(true);
      } else {
        console.error('Failed to fetch database schema:', response?.message || 'Unknown error');
        setTestResult({
          success: false,
          message: 'Failed to fetch schema: ' + (response?.message || 'Unknown error'),
          detailedError: response?.error
        });
      }
    } catch (error: any) {
      console.error('Error fetching database schema:', error);
      setTestResult({
        success: false,
        message: 'Error fetching schema: ' + (error?.message || 'Unknown error'),
        detailedError: error?.toString()
      });
    } finally {
      setIsLoadingSchema(false);
    }
  };

  const handleSubmit = () => {
    if (connectionTested) {
      // Update connectionAccessLevel based on isSource and isDestination
      let connectionAccessLevelValue: string = connectionDetails.connectionAccessLevel;

      if (connectionDetails.isSource && connectionDetails.isDestination) {
        connectionAccessLevelValue = 'ReadWrite';
      } else if (connectionDetails.isSource) {
        connectionAccessLevelValue = 'ReadOnly';
      } else if (connectionDetails.isDestination) {
        connectionAccessLevelValue = 'WriteOnly';
      } else {
        connectionAccessLevelValue = 'ReadOnly'; // Default
      }

      // Always build a connection string that includes username and password
      let connectionString = '';

      if (connectionMode === 'string') {
        // For connection string mode, ensure username and password are included
        connectionString = connectionDetails.connectionString;

        // Make sure username and password are in the connection string
        if (connectionDetails.username && connectionDetails.password) {
          // Check if they're already in the string
          const hasUserId = /User ID=/i.test(connectionString);
          const hasPassword = /Password=/i.test(connectionString);

          // If not, add them
          if (!hasUserId) {
            connectionString += `;User ID=${connectionDetails.username}`;
          }
          if (!hasPassword) {
            connectionString += `;Password=${connectionDetails.password}`;
          }
        }
      } else {
        // For details mode, build the connection string with all parameters
        connectionString = buildConnectionString();
      }

      console.log('Final connection string for saving:', connectionString);

      // Create a standardized Connection object that matches our database model
      const updatedConnection: Connection = {
        ...connectionDetails,
        connectionString: connectionString,
        connectionAccessLevel: connectionAccessLevelValue,
        isConnectionValid: testResult?.success || false,
        lastModifiedBy: "System",
        lastModifiedOn: new Date().toISOString(),
        // Ensure computed properties match the connectionAccessLevel
        isSource: connectionAccessLevelValue === 'ReadOnly' || connectionAccessLevelValue === 'ReadWrite',
        isDestination: connectionAccessLevelValue === 'WriteOnly' || connectionAccessLevelValue === 'ReadWrite'
      };

      console.log('Saving connection with data:', updatedConnection);
      onSave(updatedConnection);
    } else {
      setTestResult({
        success: false,
        message: 'Please test the connection before saving'
      });
    }
  };

  return (
    <>
      <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
        <DialogTitle>
          {connectionDetails.connectionId ? 'Edit Connection' : 'New Connection'}
        </DialogTitle>
        <DialogContent>
          <Grid container spacing={2} sx={{ mt: 1 }}>
            {/* Basic connection info */}
            <ConnectionBasicInfo
              connectionName={connectionDetails.connectionName}
              connectionType="sqlServer" // Default to SQL Server for now
              onNameChange={handleConnectionDetailsChange}
              onTypeChange={handleConnectionDetailsChange}
            />

            {/* Connection mode tabs */}
            <Grid item xs={12}>
              <Tabs
                value={connectionMode}
                onChange={handleConnectionModeChange}
                aria-label="connection mode tabs"
                sx={{ mb: 2 }}
              >
                <Tab value="string" label="Connection String" />
                <Tab value="details" label="Connection Details" />
              </Tabs>

              {/* Connection String Form */}
              {connectionMode === 'string' && (
                <ConnectionStringForm
                  connectionString={connectionDetails.connectionString}
                  connectionType="sqlServer" // Default to SQL Server for now
                  onChange={handleConnectionDetailsChange}
                />
              )}

              {/* Connection Details Form */}
              {connectionMode === 'details' && (
                <ConnectionDetailsForm
                  connectionDetails={connectionDetails}
                  formSettings={{
                    timeout: connectionDetails.timeout,
                    encrypt: connectionDetails.encrypt,
                    trustServerCertificate: connectionDetails.trustServerCertificate
                  }}
                  onDetailsChange={handleConnectionDetailsChange}
                  onSettingChange={handleConnectionDetailsChange}
                />
              )}
            </Grid>

            {/* Test Connection Section */}
            <TestConnectionSection
              isTesting={isTesting}
              testResult={testResult}
              onTestConnection={handleTestConnection}
            />

            {/* Connection Settings (after successful test) */}
            <ConnectionSettings
              formData={{
                description: connectionDetails.description,
                connectionAccessLevel: connectionDetails.connectionAccessLevel,
                isSource: connectionDetails.isSource,
                isDestination: connectionDetails.isDestination,
                isActive: connectionDetails.isActive,
                lastTestedOn: connectionDetails.lastTestedOn ? new Date(connectionDetails.lastTestedOn) : null,
                maxPoolSize: connectionDetails.maxPoolSize,
                minPoolSize: connectionDetails.minPoolSize
              }}
              connectionTested={connectionTested}
              onFormChange={handleConnectionDetailsChange}
              onConnectionPurposeChange={handleConnectionPurposeChange}
            />
          </Grid>
        </DialogContent>
        <DialogActions>
          <Button onClick={onClose}>Cancel</Button>
          {connectionTested && (
            <Button
              onClick={fetchDatabaseSchema}
              variant="outlined"
              color="primary"
              disabled={isLoadingSchema}
              sx={{ mr: 1 }}
            >
              {isLoadingSchema ? <CircularProgress size={24} /> : 'View Schema'}
            </Button>
          )}
          <Button
            onClick={handleSubmit}
            variant="contained"
            color="primary"
            disabled={!connectionTested}
          >
            Save
          </Button>
        </DialogActions>
      </Dialog>

      {/* Database Schema Dialog */}
      <DatabaseSchemaDialog
        open={schemaDialogOpen}
        isLoading={isLoadingSchema}
        databaseName={connectionDetails.database || testResult?.database || 'Database'}
        schema={dbSchema}
        onClose={() => setSchemaDialogOpen(false)}
      />
    </>
  );
}