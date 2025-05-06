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
import DataTransferService from '@/services/dataTransfer.service';

// Import the new component modules
import ConnectionBasicInfo from './ConnectionBasicInfo';
import ConnectionStringForm from './ConnectionStringForm';
import ConnectionDetailsForm from './ConnectionDetailsForm';
import TestConnectionSection from './TestConnectionSection';
import ConnectionSettings from './ConnectionSettings';
import DatabaseSchemaDialog from './DatabaseSchemaDialog';

interface ConnectionDialogProps {
  open: boolean;
  connection: any;
  onClose: () => void;
  onSave: (connection: any) => void;
}

interface ConnectionDetails {
  server: string;
  database: string;
  username: string;
  password: string;
  port?: string;
  additionalParams?: string;
}

export default function ConnectionDialog({ open, connection, onClose, onSave }: ConnectionDialogProps) {
  const [formData, setFormData] = useState({
    connectionId: 0,
    connectionName: '',
    connectionString: '',
    description: '',
    isSource: true,
    isDestination: false,
    isActive: false,
    connectionAccessLevel: 'ReadOnly',
    lastTestedOn: null as Date | null,
    connectionType: 'sqlServer',
    timeout: 30,
    maxPoolSize: 100,
    minPoolSize: 5,
    encrypt: true,
    trustServerCertificate: true,
  });

  const [connectionMode, setConnectionMode] = useState<'string' | 'details'>('string');
  const [connectionDetails, setConnectionDetails] = useState<ConnectionDetails>({
    server: '',
    database: '',
    username: '',
    password: '',
    port: '',
    additionalParams: '',
  });

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

      const displayConnectionString = connection.connectionStringForDisplay || connection.connectionString || '';

      setFormData({
        connectionId: connection.connectionId || 0,
        connectionName: connection.connectionName || '',
        connectionString: displayConnectionString,
        description: connection.description || '',
        isSource: connection.isSource !== undefined ? connection.isSource : true,
        isDestination: connection.isDestination !== undefined ? connection.isDestination : false,
        isActive: connection.isActive !== undefined ? connection.isActive : true,
        connectionAccessLevel: accessLevel,
        lastTestedOn: connection.lastTestedOn ? new Date(connection.lastTestedOn) : null,
        connectionType: connection.connectionType || 'sqlServer',
        timeout: connection.timeout || 30,
        maxPoolSize: connection.maxPoolSize || 100,
        minPoolSize: connection.minPoolSize || 5,
        encrypt: connection.encrypt !== undefined ? connection.encrypt : true,
        trustServerCertificate: connection.trustServerCertificate !== undefined ? connection.trustServerCertificate : true,
      });

      if (connection.connectionDetails) {
        setConnectionDetails({
          server: connection.connectionDetails.server || '',
          database: connection.connectionDetails.database || '',
          username: connection.connectionDetails.username || '',
          password: connection.connectionDetails.password || '',
          port: connection.connectionDetails.port || '',
          additionalParams: '',
        });

        setConnectionMode('details');
      } else if (displayConnectionString) {
        parseConnectionString(displayConnectionString);
      }

      if (connection.lastTestedOn && connection.isActive) {
        setConnectionTested(true);
      } else {
        setTestResult(null);
        setConnectionTested(false);
      }
    } else {
      // Reset form for new connections
      setFormData({
        connectionId: 0,
        connectionName: '',
        connectionString: '',
        description: '',
        isSource: true,
        isDestination: false,
        isActive: false,
        connectionAccessLevel: 'ReadOnly',
        lastTestedOn: null,
        connectionType: 'sqlServer',
        timeout: 30,
        maxPoolSize: 100,
        minPoolSize: 5,
        encrypt: true,
        trustServerCertificate: true,
      });

      setConnectionDetails({
        server: '',
        database: '',
        username: '',
        password: '',
        port: '',
        additionalParams: '',
      });

      setTestResult(null);
      setConnectionTested(false);
    }
  }, [connection, open]);

  // Helper functions
  const parseConnectionString = (connectionString: string) => {
    try {
      const details: ConnectionDetails = {
        server: '',
        database: '',
        username: '',
        password: '',
        port: '',
        additionalParams: '',
      };

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
          details.port = valueClean;
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
      return formData.connectionString;
    }

    // For details mode, build the connection string with all parameters
    const { server, database, username, password, port, additionalParams } = connectionDetails;
    let connectionString = '';

    if (formData.connectionType === 'sqlServer') {
      // Always include username and password
      connectionString = `Server=${server}${port ? ',' + port : ''};Database=${database};User ID=${username};Password=${password};`;

      if (formData.encrypt) {
        connectionString += 'Encrypt=True;';
      }

      if (formData.trustServerCertificate) {
        connectionString += 'TrustServerCertificate=True;';
      }

      connectionString += `Connection Timeout=${formData.timeout};`;

      if (additionalParams) {
        connectionString += additionalParams;
      }
    } else if (formData.connectionType === 'mysql') {
      // Always include username and password
      connectionString = `Server=${server};Database=${database};User ID=${username};Password=${password};`;

      if (port) {
        connectionString += `Port=${port};`;
      }

      connectionString += `Connection Timeout=${formData.timeout};`;

      if (additionalParams) {
        connectionString += additionalParams;
      }
    }

    console.log('Built connection string:', connectionString);
    return connectionString;
  };

  // Event handlers
  const handleChangeFixed = (e: React.ChangeEvent<HTMLInputElement | { name?: string; value: unknown }>) => {
    const { name, value, checked } = e.target as any;

    if (!name) return;

    setFormData(prev => ({
      ...prev,
      [name]: (e.target as any).type === 'checkbox' ? checked : value
    }));

    if (name === 'connectionString') {
      setTestResult(null);
      setConnectionTested(false);
    }
  };

  const handleConnectionDetailsChange = (e: React.ChangeEvent<HTMLInputElement | { name?: string; value: unknown }>) => {
    const { name, value } = e.target as any;

    if (!name) return;

    setConnectionDetails(prev => ({
      ...prev,
      [name]: value
    }));

    setTestResult(null);
    setConnectionTested(false);
  };

  const handleSelectChange = (e: any) => {
    const { name, value } = e.target;

    if (!name) return;

    setFormData(prev => ({
      ...prev,
      [name]: value
    }));

    setTestResult(null);
    setConnectionTested(false);
  };

  const handleConnectionModeChange = (_event: React.SyntheticEvent, newValue: 'string' | 'details') => {
    setConnectionMode(newValue);

    if (newValue === 'details' && formData.connectionString) {
      parseConnectionString(formData.connectionString);
    } else if (newValue === 'string' && connectionDetails.server) {
      const connectionString = buildConnectionString();
      setFormData(prev => ({
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
      setFormData(prev => ({
        ...prev,
        isSource: true,
        isDestination: false,
        connectionAccessLevel: 'ReadOnly'
      }));
    } else if (value === 'destination') {
      setFormData(prev => ({
        ...prev,
        isSource: false,
        isDestination: true,
        connectionAccessLevel: 'WriteOnly'
      }));
    } else if (value === 'both') {
      setFormData(prev => ({
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
      connectionString = formData.connectionString;

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

      if (formData.connectionAccessLevel) {
        switch (formData.connectionAccessLevel) {
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
      } else {
        connectionAccessLevelValue =
          (formData.isSource && formData.isDestination) ? 2 :
          formData.isSource ? 0 :
          formData.isDestination ? 1 : 0;
      }

      // Log the username and password for debugging
      console.log('Testing connection with username:', connectionDetails.username);
      console.log('Testing connection with password:', connectionDetails.password);
      console.log('Testing connection string:', connectionString);

      const connectionData = {
        connectionId: formData.connectionId,
        connectionName: formData.connectionName,
        connectionString: connectionString,
        description: formData.description,
        connectionAccessLevel: connectionAccessLevelValue,
        isActive: formData.isActive,
        maxPoolSize: formData.maxPoolSize,
        minPoolSize: formData.minPoolSize,
        timeout: formData.timeout,
        encrypt: formData.encrypt,
        trustServerCertificate: formData.trustServerCertificate,
        // Include username and password directly
        username: connectionDetails.username,
        password: connectionDetails.password,
        createdBy: "System",
        lastModifiedBy: "System",
        createdOn: new Date().toISOString(),
        lastModifiedOn: new Date().toISOString()
      };

      const response = await DataTransferService.testConnection(connectionData);

      if (response && response.success) {
        setTestResult({
          success: true,
          message: response.message || 'Connection successful',
          server: response.server || '',
          database: response.database || ''
        });

        if (connectionMode === 'details') {
          setFormData(prev => ({
            ...prev,
            connectionString
          }));
        }

        const now = new Date();
        setFormData(prev => ({
          ...prev,
          lastTestedOn: now,
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
        connectionStr = formData.connectionString;
      } else {
        connectionStr = buildConnectionString();
      }

      // Log the username and password for debugging
      console.log('Fetching schema with username:', connectionDetails.username);
      console.log('Fetching schema with password:', connectionDetails.password);
      console.log('Fetching schema with connection string:', connectionStr);

      const connectionData = {
        connectionId: formData.connectionId,
        connectionName: formData.connectionName,
        connectionString: connectionStr,
        connectionAccessLevel: formData.connectionAccessLevel,
        // Include username and password directly
        username: connectionDetails.username,
        password: connectionDetails.password,
        // Include these additional parameters that are used during successful test connection
        encrypt: formData.encrypt,
        trustServerCertificate: formData.trustServerCertificate,
        timeout: formData.timeout,
        maxPoolSize: formData.maxPoolSize,
        minPoolSize: formData.minPoolSize
      };

      console.log('Sending schema request for database:', connectionDetails.database || 'Unknown');
      const response = await DataTransferService.fetchDatabaseSchema(connectionData);
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
      let connectionAccessLevelValue: string;

      if (formData.isSource && formData.isDestination) {
        connectionAccessLevelValue = 'ReadWrite';
      } else if (formData.isSource) {
        connectionAccessLevelValue = 'ReadOnly';
      } else if (formData.isDestination) {
        connectionAccessLevelValue = 'WriteOnly';
      } else {
        connectionAccessLevelValue = 'ReadOnly';
      }

      // Always build a connection string that includes username and password
      let connectionString = '';

      if (connectionMode === 'string') {
        // For connection string mode, ensure username and password are included
        connectionString = formData.connectionString;

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

      // Create the form data with all necessary fields
      const updatedFormData = {
        ...formData,
        connectionString,
        connectionAccessLevel: connectionAccessLevelValue,
        // Include connection details for debugging
        connectionDetails: {
          server: connectionDetails.server,
          database: connectionDetails.database,
          username: connectionDetails.username,
          password: connectionDetails.password,
          port: connectionDetails.port,
          additionalParams: connectionDetails.additionalParams
        },
        // Include username and password directly in the connection object
        username: connectionDetails.username,
        password: connectionDetails.password,
        createdBy: "System",
        lastModifiedBy: "System",
        createdOn: new Date().toISOString(),
        lastModifiedOn: new Date().toISOString()
      };

      console.log('Saving connection with username:', connectionDetails.username);
      console.log('Saving connection with password:', connectionDetails.password);
      console.log('Saving connection with data:', updatedFormData);
      onSave(updatedFormData);
    } else {
      setTestResult({
        success: false,
        message: 'Please test the connection before saving'
      });
    }
  };

  // This function is used when extracting connection details from a connection string
  // It's kept here for future use
  /*
  const parseConnectionStringToObject = (connectionString: string) => {
    const details: ConnectionDetails = {
      server: '',
      database: '',
      username: '',
      password: '',
      port: '',
      additionalParams: '',
    };

    try {
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
          details.port = valueClean;
        }
      });
    } catch (error) {
      console.error('Error parsing connection string:', error);
    }

    return details;
  };
  */

  return (
    <>
      <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
        <DialogTitle>
          {formData.connectionId ? 'Edit Connection' : 'New Connection'}
        </DialogTitle>
        <DialogContent>
          <Grid container spacing={2} sx={{ mt: 1 }}>
            {/* Basic connection info */}
            <ConnectionBasicInfo
              connectionName={formData.connectionName}
              connectionType={formData.connectionType}
              onNameChange={handleChangeFixed}
              onTypeChange={handleSelectChange}
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
                  connectionString={formData.connectionString}
                  connectionType={formData.connectionType}
                  onChange={handleChangeFixed}
                />
              )}

              {/* Connection Details Form */}
              {connectionMode === 'details' && (
                <ConnectionDetailsForm
                  connectionDetails={connectionDetails}
                  formSettings={{
                    timeout: formData.timeout,
                    encrypt: formData.encrypt,
                    trustServerCertificate: formData.trustServerCertificate
                  }}
                  onDetailsChange={handleConnectionDetailsChange}
                  onSettingChange={handleChangeFixed}
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
                description: formData.description,
                connectionAccessLevel: formData.connectionAccessLevel,
                isSource: formData.isSource,
                isDestination: formData.isDestination,
                isActive: formData.isActive,
                lastTestedOn: formData.lastTestedOn,
                maxPoolSize: formData.maxPoolSize,
                minPoolSize: formData.minPoolSize
              }}
              connectionTested={connectionTested}
              onFormChange={handleChangeFixed}
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