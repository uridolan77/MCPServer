import React, { useState, useEffect, useCallback } from 'react';
import { Box, FormControl, InputLabel, Select, MenuItem, Button, CircularProgress, Typography, Alert } from '@mui/material';
import { SelectChangeEvent } from '@mui/material/Select';
import ConnectionService from '@/services/connection.service';
import { Connection } from '@/pages/transfer/types/Connection';
import { ConnectionSelectionStepProps } from './types';
import { useSnackbar } from '@/hooks/useSnackbar';

const ConnectionSelectionStep: React.FC<ConnectionSelectionStepProps> = ({ formData, onFormDataChange, setStepValidated }) => {
  const [connections, setConnections] = useState<Connection[]>([]);
  const [selectedConnectionId, setSelectedConnectionId] = useState<number | ''>('');
  const [isLoadingConnections, setIsLoadingConnections] = useState<boolean>(false);
  const [isTestingConnection, setIsTestingConnection] = useState<boolean>(false);
  const [testConnectionResult, setTestConnectionResult] = useState<{ success: boolean; message: string; isConnectionValid?: boolean } | null>(null);
  const { showSnackbar } = useSnackbar();

  // Load connections only once when the component mounts
  useEffect(() => {
    setStepValidated(false); // Initially, step is not validated
    loadConnections();
  }, [setStepValidated]);

  // Handle pre-selection and syncing with formData
  useEffect(() => {
    // Only update from formData when we have a valid connectionId and it doesn't match the current selection
    if (formData.selectedConnection?.connectionId && 
        formData.selectedConnection.connectionId !== selectedConnectionId) {
      setSelectedConnectionId(formData.selectedConnection.connectionId);
    }
    
    // Only update test result from formData when it exists and differs from current result
    if (formData.connectionTestResult && 
        JSON.stringify(formData.connectionTestResult) !== JSON.stringify(testConnectionResult)) {
      setTestConnectionResult(formData.connectionTestResult);
    }

    // Set step validation based on test result success
    const hasSuccessfulTest = testConnectionResult?.success === true;
    
    // Directly call setStepValidated with the test result success
    setStepValidated(hasSuccessfulTest);
    
    console.log('Step validation state:', { 
      hasSelectedConnection: selectedConnectionId !== '',
      currentConnection: connections.find(c => c.connectionId === selectedConnectionId),
      formDataConnection: formData.selectedConnection,
      hasSuccessfulTest,
      testResult: testConnectionResult,
      isStepValid: hasSuccessfulTest
    });
  }, [formData.selectedConnection, formData.connectionTestResult, selectedConnectionId, testConnectionResult, setStepValidated, connections]);

  const loadConnections = async () => {
    setIsLoadingConnections(true);
    try {
      const fetchedConnections = await ConnectionService.getConnections();
      
      console.log('Raw fetched connections:', fetchedConnections);
      
      // Map the connections to ensure consistent property naming
      // The API returns connections with both "id" and "name" fields (not connectionId/connectionName)
      const mappedConnections = fetchedConnections.map(conn => {
        console.log('Mapping connection:', conn);
        return {
          ...conn,
          // Ensure correct ID mapping - API returns "id", not "connectionId"
          connectionId: conn.id || conn.Id || conn.connectionId || conn.ConnectionId,
          // Ensure correct name mapping - API returns "name", not "connectionName"
          connectionName: conn.name || conn.Name || conn.connectionName || conn.ConnectionName || 'Unnamed Connection'
        };
      });
      
      // Only filter for active connections
      const activeConnections = mappedConnections.filter(conn => conn.isActive === true);
      
      console.log('Active connections after filtering:', activeConnections);
      
      setConnections(activeConnections);
      
      // If we have connections and nothing is selected yet, select the first one
      if (activeConnections.length > 0 && !formData.selectedConnection) {
        const firstConnection = activeConnections[0];
        console.log('Selecting first connection:', firstConnection);
        setSelectedConnectionId(firstConnection.connectionId);
        onFormDataChange({ 
          selectedConnection: firstConnection,
          connectionTestResult: undefined 
        });
      }
    } catch (error) {
      console.error('Error loading connections:', error);
      showSnackbar('Error loading connections', 'error');
    } finally {
      setIsLoadingConnections(false);
    }
  };

  const handleConnectionChange = (event: SelectChangeEvent<number | ''>) => {
    const connId = event.target.value as number | '';
    setSelectedConnectionId(connId);
    setTestConnectionResult(null); // Reset test result on new selection
    
    const selectedConn = connections.find(c => c.connectionId === connId);
    if (selectedConn) {
      // Create a complete connection object with all necessary properties
      const completeConnection = {
        ...selectedConn,
        id: selectedConn.connectionId,  // Ensure both id and connectionId are set 
        connectionId: selectedConn.connectionId,
        name: selectedConn.connectionName,
        connectionName: selectedConn.connectionName
      };
      
      // Update the form data in the parent component
      console.log('Setting selected connection:', completeConnection);
      onFormDataChange({ 
        selectedConnection: completeConnection, 
        connectionTestResult: undefined 
      });
    } else {
      onFormDataChange({ 
        selectedConnection: undefined, 
        connectionTestResult: undefined 
      });
    }
  };

  const handleTestConnection = useCallback(async () => {
    if (!selectedConnectionId) {
      showSnackbar('Please select a connection to test.', 'warning');
      return;
    }
    const connectionToTest = connections.find(c => c.connectionId === selectedConnectionId);
    if (!connectionToTest) {
      showSnackbar('Selected connection not found.', 'error');
      return;
    }

    // Check if the connection has a valid ID
    if (typeof connectionToTest.connectionId !== 'number' || connectionToTest.connectionId <= 0) {
      showSnackbar(`Cannot test connection "${connectionToTest.connectionName}". The connection ID is invalid or temporary.`, 'error');
      return;
    }

    setIsTestingConnection(true);
    setTestConnectionResult(null);
    try {
      console.log('Testing connection with ID:', connectionToTest.connectionId);
      
      // The API expects an "id" parameter, not "connectionId"
      const response = await ConnectionService.testConnection({
        id: connectionToTest.connectionId
      });
      
      const result = { 
        success: response.success, 
        message: response.message,
        isConnectionValid: response.isConnectionValid 
      };
      
      // Create a complete connection object with all required properties
      const updatedConnection = {
        ...connectionToTest,
        id: connectionToTest.connectionId,
        connectionId: connectionToTest.connectionId,
        name: connectionToTest.connectionName,
        connectionName: connectionToTest.connectionName,
        isConnectionValid: response.isConnectionValid,
        lastTestedOn: new Date().toISOString()
      };
      
      // First set the local state
      setTestConnectionResult(result);
      
      // Then update the parent component's state with BOTH the connection and test result
      // This ensures the next step has access to the connection data
      onFormDataChange({
        selectedConnection: updatedConnection,
        connectionTestResult: result
      });
      
      // Directly set the step validation based on test success
      if (result.success) {
        setStepValidated(true);
      }

      console.log('Connection test complete. Updated form data with connection:', updatedConnection);
      console.log('Step validation set to:', result.success);

      if (result.success) {
        showSnackbar(result.message || 'Connection test successful.', 'success');
      } else {
        showSnackbar(result.message || 'Connection test failed.', 'error');
      }
    } catch (error: any) {
      console.error('Error testing connection:', error);
      const errorMessage = error.response?.data?.message || error.message || 'An unknown error occurred during connection test.';
      const result = { 
        success: false, 
        message: errorMessage,
        isConnectionValid: false 
      };
      
      // Update the connection with failed status
      const updatedConnection = {
        ...connectionToTest,
        id: connectionToTest.connectionId,
        connectionId: connectionToTest.connectionId,
        name: connectionToTest.connectionName,
        connectionName: connectionToTest.connectionName,
        isConnectionValid: false,
        lastTestedOn: new Date().toISOString()
      };
      
      // Set state and update formData
      setTestConnectionResult(result);
      setStepValidated(false);
      onFormDataChange({ 
        selectedConnection: updatedConnection,
        connectionTestResult: result
      });
      
      showSnackbar(errorMessage, 'error');
    } finally {
      setIsTestingConnection(false);
    }
  }, [selectedConnectionId, connections, onFormDataChange, showSnackbar, setStepValidated]);

  return (
    <Box sx={{ p: 2 }}>
      <Typography variant="h6" gutterBottom>
        Select Database Connection
      </Typography>
      <Typography variant="body2" color="text.secondary" gutterBottom>
        Choose an active connection from the list below. Test the connection to proceed.
      </Typography>

      {isLoadingConnections ? (
        <CircularProgress />
      ) : connections.length === 0 ? (
        <Alert severity="info" sx={{ mt: 2, mb: 1 }}>
          No active connections found. Please ensure connections are configured and marked as active in the system.
        </Alert>
      ) : (
        <FormControl fullWidth margin="normal">
          <InputLabel id="connection-select-label">Connection</InputLabel>
          <Select
            labelId="connection-select-label"
            id="connection-select"
            value={selectedConnectionId}
            label="Connection"
            onChange={handleConnectionChange}
            disabled={isTestingConnection}
          >
            {connections.map((conn) => (
              <MenuItem key={conn.connectionId} value={conn.connectionId}>
                {conn.connectionName}
              </MenuItem>
            ))}
          </Select>
        </FormControl>
      )}

      <Button
        variant="contained"
        onClick={handleTestConnection}
        disabled={connections.length === 0 || selectedConnectionId === '' || isLoadingConnections || isTestingConnection}
        sx={{ mt: 2, mr: 1 }}
      >
        {isTestingConnection ? <CircularProgress size={24} /> : 'Test Connection'}
      </Button>

      {testConnectionResult && (
        <Alert severity={testConnectionResult.success ? 'success' : 'error'} sx={{ mt: 2 }}>
          {testConnectionResult.message}
        </Alert>
      )}
      
      {/* Only show the info alert if no connection is selected yet */}
      {!formData.selectedConnection && connections.length > 0 && !selectedConnectionId && (
        <Alert severity="info" sx={{ mt: 2 }}>
          Please select a connection and test it to proceed.
        </Alert>
      )}
      
      {/* Only show the warning if a connection is selected but hasn't been successfully tested */}
      {formData.selectedConnection && !testConnectionResult?.success && (
        <Alert severity="warning" sx={{ mt: 2 }}>
          Please ensure the connection test is successful to proceed.
        </Alert>
      )}
    </Box>
  );
};

export default ConnectionSelectionStep;
