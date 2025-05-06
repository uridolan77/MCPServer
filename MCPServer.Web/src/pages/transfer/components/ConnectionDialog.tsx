import React, { useState, useEffect } from 'react';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  TextField,
  Grid,
  FormControlLabel,
  Switch,
  Box,
  CircularProgress,
  Alert,
  Divider,
  Typography,
  Tabs,
  Tab,
  Radio,
  RadioGroup,
  FormControl,
  FormLabel,
  InputLabel,
  Select,
  MenuItem,
  SelectChangeEvent,
} from '@mui/material';
import DataTransferService from '@/services/dataTransfer.service';

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
    isActive: false, // Default to inactive until connection is tested successfully
    connectionAccessLevel: 'ReadOnly', // ReadOnly, WriteOnly, ReadWrite
    lastTestedOn: null as Date | null,
    connectionType: 'sqlServer', // sqlServer, mysql, postgresql, oracle
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

  useEffect(() => {
    if (connection) {
      // Determine connection access level from isSource and isDestination if connectionAccessLevel is not provided
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

      // Check if we have a connection string for display (for hashed connection strings)
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

      // If we have connection details from the server, use them to populate the connection details form
      if (connection.connectionDetails) {
        setConnectionDetails({
          server: connection.connectionDetails.server || '',
          database: connection.connectionDetails.database || '',
          username: connection.connectionDetails.username || '',
          password: connection.connectionDetails.password || '',
          port: connection.connectionDetails.port || '',
          additionalParams: '',
        });

        // Switch to details mode if we have connection details
        setConnectionMode('details');
      }
      // Otherwise try to parse connection string into details if it exists
      else if (displayConnectionString) {
        parseConnectionString(displayConnectionString);
      }

      // For existing connections with a lastTestedOn date, consider them already tested
      if (connection.lastTestedOn && connection.isActive) {
        setConnectionTested(true);
      } else {
        // Reset test result when opening an existing connection that hasn't been tested
        setTestResult(null);
        setConnectionTested(false);
      }
    } else {
      setFormData({
        connectionId: 0,
        connectionName: '',
        connectionString: '',
        description: '',
        isSource: true,
        isDestination: false,
        isActive: false, // Default to inactive until connection is tested successfully
        connectionAccessLevel: 'ReadOnly',
        lastTestedOn: null,
        connectionType: 'sqlServer',
        timeout: 30,
        maxPoolSize: 100,
        minPoolSize: 5,
        encrypt: true,
        trustServerCertificate: true,
      });

      // Reset connection details
      setConnectionDetails({
        server: '',
        database: '',
        username: '',
        password: '',
        port: '',
        additionalParams: '',
      });

      // Reset test result when opening a new connection
      setTestResult(null);
      setConnectionTested(false);
    }
  }, [connection, open]);

  // Parse connection string into details
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

      // Parse connection string parameters
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
      // If parsing fails, just keep the connection string as is
      setConnectionMode('string');
    }
  };

  // Build connection string from details
  const buildConnectionString = () => {
    if (connectionMode === 'string') {
      return formData.connectionString;
    }

    const { server, database, username, password, port, additionalParams } = connectionDetails;
    let connectionString = '';

    if (formData.connectionType === 'sqlServer') {
      connectionString = `Server=${server}${port ? ',' + port : ''};Database=${database};User ID=${username};Password=${password};`;

      // Add optional parameters
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
      connectionString = `Server=${server};Database=${database};User ID=${username};Password=${password};`;

      if (port) {
        connectionString += `Port=${port};`;
      }

      connectionString += `Connection Timeout=${formData.timeout};`;

      if (additionalParams) {
        connectionString += additionalParams;
      }
    }

    return connectionString;
  };

  // Remove unused handleChange function

  const handleConnectionDetailsChange = (e: React.ChangeEvent<HTMLInputElement | { name?: string; value: unknown }>) => {
    const { name, value } = e.target as any;

    if (!name) return;

    setConnectionDetails(prev => ({
      ...prev,
      [name]: value
    }));

    // Reset test result when connection details change
    setTestResult(null);
    setConnectionTested(false);
  };

  const handleSelectChange = (e: SelectChangeEvent) => {
    const { name, value } = e.target;

    if (!name) return;

    setFormData(prev => ({
      ...prev,
      [name]: value
    }));

    // Reset test result when connection type changes
    setTestResult(null);
    setConnectionTested(false);
  };

  const handleConnectionModeChange = (_event: React.SyntheticEvent, newValue: 'string' | 'details') => {
    setConnectionMode(newValue);

    if (newValue === 'details' && formData.connectionString) {
      // Parse connection string into details
      parseConnectionString(formData.connectionString);
    } else if (newValue === 'string' && connectionDetails.server) {
      // Build connection string from details
      const connectionString = buildConnectionString();
      setFormData(prev => ({
        ...prev,
        connectionString
      }));
    }

    // Reset test result when switching modes
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
    // For existing connections with hashed connection strings, we can just send the connection ID
    // and the backend will retrieve the original connection string
    if (formData.connectionId > 0 &&
        (formData.connectionString.includes("********") ||
         formData.connectionString.startsWith("HASHED:"))) {

      console.log("Testing existing connection with ID:", formData.connectionId);

      setIsTesting(true);
      setTestResult(null);

      try {
        // Get the numeric value for connectionAccessLevel
        let connectionAccessLevelValue: number;

        if (formData.connectionAccessLevel) {
          // Convert string enum to numeric value
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
              connectionAccessLevelValue = 0; // Default to ReadOnly
          }
        } else {
          // Derive from isSource and isDestination
          connectionAccessLevelValue =
            (formData.isSource && formData.isDestination) ? 2 : // ReadWrite
            formData.isSource ? 0 : // ReadOnly
            formData.isDestination ? 1 : 0; // WriteOnly, default to ReadOnly
        }

        // Check if we're in details mode, and if so, include the updated values as overrides in connection string
        let modifiedConnectionString = formData.connectionString;
        if (connectionMode === 'details') {
          // Extract the base part of the connection string without any existing overrides
          const basePart = formData.connectionString.split(';')
            .filter(part => !part.toLowerCase().startsWith('overrideserver=') && 
                        !part.toLowerCase().startsWith('overridedatabase=') &&
                        !part.toLowerCase().startsWith('overrideusername=') &&
                        !part.toLowerCase().startsWith('overridepassword='))
            .join(';');
            
          // Add override parameters for all fields
          modifiedConnectionString = `${basePart};OverrideServer=${connectionDetails.server};OverrideDatabase=${connectionDetails.database};OverrideUsername=${connectionDetails.username};OverridePassword=${connectionDetails.password}`;
          console.log('Original connection string:', formData.connectionString);
          console.log('Adding override parameters to connection string - using username:', connectionDetails.username);
          console.log('Current server value:', connectionDetails.server);
          console.log('Current database value:', connectionDetails.database);
        }

        // Create a connection object with just the ID and other necessary fields
        const connectionData = {
          connectionId: formData.connectionId,
          connectionName: formData.connectionName,
          connectionString: modifiedConnectionString, // Include any override parameters
          description: formData.description,
          connectionAccessLevel: connectionAccessLevelValue,
          isActive: formData.isActive,
          maxPoolSize: formData.maxPoolSize,
          minPoolSize: formData.minPoolSize,
          timeout: formData.timeout,
          encrypt: formData.encrypt,
          trustServerCertificate: formData.trustServerCertificate,
          // Add required fields
          createdBy: "System",
          lastModifiedBy: "System",
          createdOn: new Date().toISOString(),
          lastModifiedOn: new Date().toISOString()
        };

        console.log('Sending test connection data for existing connection:', connectionData);
        const response = await DataTransferService.testConnection(connectionData);

        if (response && response.success) {
          setTestResult({
            success: true,
            message: response.message || 'Connection successful',
            server: response.server || '',
            database: response.database || ''
          });

          // If test is successful and we're in details mode, update the connection string in formData
          if (connectionMode === 'details') {
            setFormData(prev => ({
              ...prev,
              connectionString: modifiedConnectionString
            }));
          }

          // Update lastTestedOn field and set IsActive to true
          const now = new Date();
          setFormData(prev => ({
            ...prev,
            lastTestedOn: now,
            isActive: true // Automatically set to active after successful test
          }));

          setConnectionTested(true);
        } else {
          // Enhanced error handling with more detailed information
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

          // Log detailed error information to console for debugging
          console.error('Connection test failed:', {
            message: response?.message,
            detailedError: response?.detailedError,
            errorCode: response?.errorCode,
            errorType: response?.errorType,
            innerException: response?.innerException
          });

          setConnectionTested(false);
        }
      } catch (error: any) {
        console.error('Connection test error:', error);

        // Enhanced error handling for exceptions
        let errorMessage = 'Connection test failed';
        let detailedError = '';

        if (error.response && error.response.data) {
          // Extract error details from API response
          const errorData = error.response.data;
          errorMessage = errorData.message || errorData.error || 'Connection test failed';
          detailedError = errorData.detailedError || errorData.error || error.message;

          // Log detailed error information
          console.error('API error response:', errorData);

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
          // Handle non-API errors
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

      return;
    }

    // For new connections or when editing the connection string, use the normal flow
    // Get the connection string based on the current mode
    let connectionString = '';

    if (connectionMode === 'string') {
      connectionString = formData.connectionString;
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
      // Get the numeric value for connectionAccessLevel
      let connectionAccessLevelValue: number;

      if (formData.connectionAccessLevel) {
        // Convert string enum to numeric value
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
            connectionAccessLevelValue = 0; // Default to ReadOnly
        }
      } else {
        // Derive from isSource and isDestination
        connectionAccessLevelValue =
          (formData.isSource && formData.isDestination) ? 2 : // ReadWrite
          formData.isSource ? 0 : // ReadOnly
          formData.isDestination ? 1 : 0; // WriteOnly, default to ReadOnly
      }

      // Create a properly formatted connection object for testing
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
        // Add required fields that were missing
        createdBy: "System",
        lastModifiedBy: "System", // This was the missing required field
        createdOn: new Date().toISOString(),
        lastModifiedOn: new Date().toISOString()
      };

      console.log('Sending test connection data:', connectionData);
      const response = await DataTransferService.testConnection(connectionData);

      if (response && response.success) {
        setTestResult({
          success: true,
          message: response.message || 'Connection successful',
          server: response.server || '',
          database: response.database || ''
        });

        // If test is successful and we're in details mode, update the connection string in formData
        if (connectionMode === 'details') {
          setFormData(prev => ({
            ...prev,
            connectionString
          }));
        }

        // Update lastTestedOn field and set IsActive to true
        const now = new Date();
        setFormData(prev => ({
          ...prev,
          lastTestedOn: now,
          isActive: true // Automatically set to active after successful test
        }));

        setConnectionTested(true);
      } else {
        // Enhanced error handling with more detailed information
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

        // Log detailed error information to console for debugging
        console.error('Connection test failed:', {
          message: response?.message,
          detailedError: response?.detailedError,
          errorCode: response?.errorCode,
          errorType: response?.errorType,
          innerException: response?.innerException
        });

        setConnectionTested(false);
      }
    } catch (error: any) {
      console.error('Connection test error:', error);

      // Enhanced error handling for exceptions
      let errorMessage = 'Connection test failed';
      let detailedError = '';

      if (error.response && error.response.data) {
        // Extract error details from API response
        const errorData = error.response.data;
        errorMessage = errorData.message || errorData.error || 'Connection test failed';
        detailedError = errorData.detailedError || errorData.error || error.message;

        // Log detailed error information
        console.error('API error response:', errorData);

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
        // Handle non-API errors
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

  const handleSubmit = () => {
    // Only allow saving if the connection has been successfully tested
    if (connectionTested) {
      // Get the numeric value for connectionAccessLevel
      let connectionAccessLevelValue: string;

      // Derive from connection purpose
      if (formData.isSource && formData.isDestination) {
        connectionAccessLevelValue = 'ReadWrite';
      } else if (formData.isSource) {
        connectionAccessLevelValue = 'ReadOnly';
      } else if (formData.isDestination) {
        connectionAccessLevelValue = 'WriteOnly';
      } else {
        connectionAccessLevelValue = 'ReadOnly'; // Default
      }

      // If we're in details mode, make sure the connection string is updated
      let updatedFormData;
      if (connectionMode === 'details') {
        const connectionString = buildConnectionString();
        updatedFormData = {
          ...formData,
          connectionString,
          connectionAccessLevel: connectionAccessLevelValue,
          // Add required fields
          createdBy: "System",
          lastModifiedBy: "System",
          createdOn: new Date().toISOString(),
          lastModifiedOn: new Date().toISOString()
        };
      } else {
        updatedFormData = {
          ...formData,
          connectionAccessLevel: connectionAccessLevelValue,
          // Add required fields
          createdBy: "System",
          lastModifiedBy: "System",
          createdOn: new Date().toISOString(),
          lastModifiedOn: new Date().toISOString()
        };
      }

      console.log('Saving connection with data:', updatedFormData);
      onSave(updatedFormData);
    } else {
      setTestResult({
        success: false,
        message: 'Please test the connection before saving'
      });
    }
  };

  // Fix the type error in handleChange
  const handleChangeFixed = (e: React.ChangeEvent<HTMLInputElement | { name?: string; value: unknown }>) => {
    const { name, value, checked } = e.target as any;

    if (!name) return;

    setFormData(prev => ({
      ...prev,
      [name]: (e.target as any).type === 'checkbox' ? checked : value
    }));

    // Reset test result when connection string changes
    if (name === 'connectionString') {
      setTestResult(null);
      setConnectionTested(false);
    }
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
      <DialogTitle>
        {formData.connectionId ? 'Edit Connection' : 'New Connection'}
      </DialogTitle>
      <DialogContent>
        <Grid container spacing={2} sx={{ mt: 1 }}>
          <Grid item xs={12}>
            <TextField
              label="Connection Name"
              name="connectionName"
              value={formData.connectionName}
              onChange={handleChangeFixed}
              fullWidth
              required
            />
          </Grid>

          {/* Connection Type Selection */}
          <Grid item xs={12}>
            <FormControl fullWidth>
              <InputLabel id="connection-type-label">Database Type</InputLabel>
              <Select
                labelId="connection-type-label"
                id="connection-type"
                name="connectionType"
                value={formData.connectionType}
                onChange={handleSelectChange}
                label="Database Type"
              >
                <MenuItem value="sqlServer">SQL Server</MenuItem>
                <MenuItem value="mysql">MySQL</MenuItem>
                <MenuItem value="postgresql">PostgreSQL</MenuItem>
                <MenuItem value="oracle">Oracle</MenuItem>
              </Select>
            </FormControl>
          </Grid>

          {/* Connection Mode Tabs */}
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

            {/* Connection String Mode */}
            {connectionMode === 'string' && (
              <TextField
                label="Connection String"
                name="connectionString"
                value={formData.connectionString}
                onChange={handleChangeFixed}
                fullWidth
                required
                multiline
                rows={3}
                helperText={
                  formData.connectionType === 'sqlServer'
                    ? "Example: Server=myserver;Database=mydatabase;User ID=myuser;Password=mypassword;"
                    : formData.connectionType === 'mysql'
                    ? "Example: Server=myserver;Database=mydatabase;User ID=myuser;Password=mypassword;Port=3306;"
                    : "Enter connection string for your database"
                }
              />
            )}

            {/* Connection Details Mode */}
            {connectionMode === 'details' && (
              <Grid container spacing={2}>
                <Grid item xs={12} md={6}>
                  <TextField
                    label="Server"
                    name="server"
                    value={connectionDetails.server}
                    onChange={handleConnectionDetailsChange}
                    fullWidth
                    required
                  />
                </Grid>
                <Grid item xs={12} md={6}>
                  <TextField
                    label="Database"
                    name="database"
                    value={connectionDetails.database}
                    onChange={handleConnectionDetailsChange}
                    fullWidth
                    required
                  />
                </Grid>
                <Grid item xs={12} md={6}>
                  <TextField
                    label="Username"
                    name="username"
                    value={connectionDetails.username}
                    onChange={handleConnectionDetailsChange}
                    fullWidth
                    required
                  />
                </Grid>
                <Grid item xs={12} md={6}>
                  <TextField
                    label="Password"
                    name="password"
                    type="password"
                    value={connectionDetails.password}
                    onChange={handleConnectionDetailsChange}
                    fullWidth
                    required
                  />
                </Grid>
                <Grid item xs={12} md={6}>
                  <TextField
                    label="Port (Optional)"
                    name="port"
                    value={connectionDetails.port}
                    onChange={handleConnectionDetailsChange}
                    fullWidth
                  />
                </Grid>
                <Grid item xs={12} md={6}>
                  <TextField
                    label="Connection Timeout (seconds)"
                    name="timeout"
                    type="number"
                    value={formData.timeout}
                    onChange={handleChangeFixed}
                    fullWidth
                  />
                </Grid>
                <Grid item xs={12} md={6}>
                  <FormControlLabel
                    control={
                      <Switch
                        name="encrypt"
                        checked={formData.encrypt}
                        onChange={handleChangeFixed}
                      />
                    }
                    label="Encrypt Connection"
                  />
                </Grid>
                <Grid item xs={12} md={6}>
                  <FormControlLabel
                    control={
                      <Switch
                        name="trustServerCertificate"
                        checked={formData.trustServerCertificate}
                        onChange={handleChangeFixed}
                      />
                    }
                    label="Trust Server Certificate"
                  />
                </Grid>
                <Grid item xs={12}>
                  <TextField
                    label="Additional Parameters (Optional)"
                    name="additionalParams"
                    value={connectionDetails.additionalParams}
                    onChange={handleConnectionDetailsChange}
                    fullWidth
                    multiline
                    rows={2}
                    helperText="Additional connection string parameters (e.g. MultipleActiveResultSets=true;)"
                  />
                </Grid>
              </Grid>
            )}
          </Grid>

          {/* Test Connection Section */}
          <Grid item xs={12}>
            <Box sx={{ display: 'flex', justifyContent: 'flex-start', mt: 1, mb: 2 }}>
              <Button
                variant="outlined"
                color="primary"
                onClick={handleTestConnection}
                disabled={isTesting}
                sx={{ mr: 2 }}
              >
                {isTesting ? <CircularProgress size={24} /> : 'Test Connection'}
              </Button>

              {testResult && (
                <Alert
                  severity={testResult.success ? 'success' : 'error'}
                  sx={{ flexGrow: 1 }}
                >
                  {testResult.success ? (
                    // Success message
                    `Connection successful to ${testResult.database} on ${testResult.server}`
                  ) : (
                    // Enhanced error message with details
                    <div>
                      <div><strong>Connection failed:</strong> {testResult.message}</div>
                      {testResult.detailedError && testResult.detailedError !== testResult.message && (
                        <div style={{ marginTop: '8px', fontSize: '0.9em' }}>
                          <strong>Details:</strong> {testResult.detailedError}
                        </div>
                      )}
                      {testResult.errorCode && (
                        <div style={{ marginTop: '4px', fontSize: '0.9em' }}>
                          <strong>Error code:</strong> {testResult.errorCode}
                        </div>
                      )}
                      {testResult.innerException && (
                        <div style={{ marginTop: '4px', fontSize: '0.9em' }}>
                          <strong>Inner exception:</strong> {testResult.innerException}
                        </div>
                      )}
                    </div>
                  )}
                </Alert>
              )}
            </Box>
          </Grid>

          <Grid item xs={12}>
            <Divider sx={{ my: 2 }} />
            <Typography variant="subtitle1" sx={{ mb: 2 }}>
              Connection Settings
            </Typography>
            {!connectionTested && (
              <Typography variant="body2" color="error" sx={{ mb: 2 }}>
                Please test the connection successfully before editing these settings
              </Typography>
            )}
          </Grid>

          <Grid item xs={12}>
            <TextField
              label="Description"
              name="description"
              value={formData.description}
              onChange={handleChangeFixed}
              fullWidth
              disabled={!connectionTested}
            />
          </Grid>

          {/* Connection Purpose */}
          <Grid item xs={12}>
            <FormControl component="fieldset" disabled={!connectionTested}>
              <FormLabel component="legend">Connection Access Level</FormLabel>
              <RadioGroup
                row
                name="connectionPurpose"
                value={
                  formData.connectionAccessLevel === 'ReadWrite'
                    ? 'both'
                    : formData.connectionAccessLevel === 'ReadOnly'
                    ? 'source'
                    : 'destination'
                }
                onChange={handleConnectionPurposeChange}
              >
                <FormControlLabel value="source" control={<Radio />} label="Read Only (Source)" />
                <FormControlLabel value="destination" control={<Radio />} label="Write Only (Destination)" />
                <FormControlLabel value="both" control={<Radio />} label="Read/Write (Both)" />
              </RadioGroup>
            </FormControl>
          </Grid>

          {/* Last Tested Information */}
          {formData.lastTestedOn && (
            <Grid item xs={12}>
              <Typography variant="body2" color="textSecondary">
                Last tested successfully on: {formData.lastTestedOn.toLocaleString()}
              </Typography>
            </Grid>
          )}

          <Grid item xs={12}>
            <FormControlLabel
              control={
                <Switch
                  name="isActive"
                  checked={formData.isActive}
                  onChange={handleChangeFixed}
                  disabled={!connectionTested}
                />
              }
              label="Active"
            />
          </Grid>

          {/* Pool Size Settings */}
          <Grid item xs={12} md={6}>
            <Typography variant="body2" gutterBottom>
              Max Pool Size: {formData.maxPoolSize}
            </Typography>
            <Box sx={{ px: 1 }}>
              <input
                type="range"
                min="10"
                max="1000"
                step="10"
                name="maxPoolSize"
                value={formData.maxPoolSize}
                onChange={handleChangeFixed}
                disabled={!connectionTested}
                style={{ width: '100%' }}
              />
            </Box>
          </Grid>

          <Grid item xs={12} md={6}>
            <Typography variant="body2" gutterBottom>
              Min Pool Size: {formData.minPoolSize}
            </Typography>
            <Box sx={{ px: 1 }}>
              <input
                type="range"
                min="1"
                max="100"
                step="1"
                name="minPoolSize"
                value={formData.minPoolSize}
                onChange={handleChangeFixed}
                disabled={!connectionTested}
                style={{ width: '100%' }}
              />
            </Box>
          </Grid>
        </Grid>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
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
  );
}