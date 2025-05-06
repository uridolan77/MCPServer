import React, { useState, useEffect } from 'react';
import {
  Box,
  Button,
  Tab,
  Tabs,
  Typography,
} from '@mui/material';
import {
  Add as AddIcon,
  Refresh as RefreshIcon,
  Storage as StorageIcon,
  Dns as DnsIcon,
  History as HistoryIcon,
} from '@mui/icons-material';
import { PageHeader } from '@/components';
import { useSnackbar } from '@/hooks/useSnackbar';
import DataTransferService from '@/services/dataTransfer.service';
import ConnectionsTable from './components/ConnectionsTable';
import ConfigurationsTable from './components/ConfigurationsTable';
import ConnectionDialog from './components/ConnectionDialog';
import ConfigurationDialog from './components/ConfigurationDialog';
import RunHistoryTable from './components/RunHistoryTable';
import RunDetailsDialog from './components/RunDetailsDialog';

interface TabPanelProps {
  children?: React.ReactNode;
  value: number;
  index: number;
  [key: string]: any;
}

function TabPanel(props: TabPanelProps) {
  const { children, value, index, ...other } = props;

  return (
    <div
      role="tabpanel"
      hidden={value !== index}
      id={`simple-tabpanel-${index}`}
      aria-labelledby={`simple-tab-${index}`}
      {...other}
    >
      {value === index && (
        <Box sx={{ p: 3 }}>
          {children}
        </Box>
      )}
    </div>
  );
}

export default function DataTransferPage() {
  const [activeTab, setActiveTab] = useState(0);
  interface Configuration {
    configurationId: number;
    configurationName: string;
    description?: string;
    sourceConnection?: any;
    destinationConnection?: any;
    batchSize?: number;
    reportingFrequency?: number;
    isActive?: boolean;
    tableMappings?: any[];
    schedules?: any[];
  }

  interface Connection {
    connectionId: number;
    connectionName: string;
    connectionString: string;
    connectionStringForDisplay?: string;
    connectionDetails?: {
      server?: string;
      database?: string;
      username?: string;
      password?: string;
      port?: string;
    };
    description?: string;
    isSource?: boolean;
    isDestination?: boolean;
    isActive: boolean;
    connectionAccessLevel?: 'ReadOnly' | 'WriteOnly' | 'ReadWrite';
    lastTestedOn?: string | Date | null;
    createdOn?: string | Date;
    lastModifiedOn?: string | Date;
    maxPoolSize?: number;
    minPoolSize?: number;
    timeout?: number;
    encrypt?: boolean;
    trustServerCertificate?: boolean;
  }

  interface RunHistoryItem {
    runId: number;
    configurationId: number;
    configurationName: string;
    startTime: string;
    endTime?: string;
    status: string;
    tablesProcessed: number;
    rowsProcessed: number;
    elapsedTime: string;
  }

  const [configurations, setConfigurations] = useState<Configuration[]>([]);
  const [connections, setConnections] = useState<Connection[]>([]);
  const [runHistory, setRunHistory] = useState<RunHistoryItem[]>([]);
  const [selectedConfig, setSelectedConfig] = useState<Configuration | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [isConfigDialogOpen, setIsConfigDialogOpen] = useState(false);
  const [isConnectionDialogOpen, setIsConnectionDialogOpen] = useState(false);
  const [isRunDetailsDialogOpen, setIsRunDetailsDialogOpen] = useState(false);
  const [runDetails, setRunDetails] = useState<any>(null);
  const { showSnackbar } = useSnackbar();

  useEffect(() => {
    loadInitialData();
  }, []);

  const loadInitialData = async () => {
    setIsLoading(true);
    try {
      await Promise.all([
        loadConfigurations(),
        loadConnections(),
        loadRunHistory()
      ]);
    } catch (error) {
      console.error('Error loading initial data:', error);
      showSnackbar('Error loading data', 'error');
    } finally {
      setIsLoading(false);
    }
  };

  const loadConfigurations = async () => {
    try {
      const response = await DataTransferService.getConfigurations();
      console.log('Configurations response:', response);

      // Check if the response has a $values property (API response format)
      if (response && response.$values) {
        // Ensure $values is an array
        const configurationsArray = Array.isArray(response.$values) ? response.$values : [];
        console.log('Configurations array:', configurationsArray);
        setConfigurations(configurationsArray);
        return configurationsArray;
      } else {
        console.error('Unexpected response format:', response);
        setConfigurations([]);
        return [];
      }
    } catch (error) {
      console.error('Error loading configurations:', error);
      showSnackbar('Error loading configurations', 'error');
      throw error;
    }
  };

  const loadConnections = async () => {
    try {
      const response = await DataTransferService.getConnections();
      console.log('Connections response:', response);

      // Check if the response has a $values property (API response format)
      if (response && response.$values) {
        // Ensure $values is an array
        const connectionsArray = Array.isArray(response.$values) ? response.$values : [];
        console.log('Connections array:', connectionsArray);
        setConnections(connectionsArray);
        return connectionsArray;
      } else {
        console.error('Unexpected response format:', response);
        setConnections([]);
        return [];
      }
    } catch (error) {
      console.error('Error loading connections:', error);
      showSnackbar('Error loading connections', 'error');
      throw error;
    }
  };

  const loadRunHistory = async (configId: number = 0) => {
    try {
      const response = await DataTransferService.getRunHistory(configId);
      console.log('Run history response:', response);

      // Check if the response has a $values property (API response format)
      if (response && response.$values) {
        // Ensure $values is an array
        const historyArray = Array.isArray(response.$values) ? response.$values : [];
        console.log('Run history array:', historyArray);
        setRunHistory(historyArray);
        return historyArray;
      } else {
        console.error('Unexpected response format:', response);
        setRunHistory([]);
        return [];
      }
    } catch (error) {
      console.error('Error loading run history:', error);
      showSnackbar('Error loading run history', 'error');
      throw error;
    }
  };

  const loadRunDetails = async (runId: number) => {
    try {
      const response = await DataTransferService.getRunDetails(runId);
      setRunDetails(response.data);
      setIsRunDetailsDialogOpen(true);
    } catch (error) {
      console.error('Error loading run details:', error);
      showSnackbar('Error loading run details', 'error');
    }
  };

  const handleTabChange = (_event: React.SyntheticEvent, newValue: number) => {
    setActiveTab(newValue);
  };

  const handleOpenConfigDialog = (config: Configuration | null = null) => {
    setSelectedConfig(config);
    setIsConfigDialogOpen(true);
  };

  const handleOpenConnectionDialog = (connection: Connection | null = null) => {
    setSelectedConfig(connection as unknown as Configuration);
    setIsConnectionDialogOpen(true);
  };

  const handleSaveConnection = async (connectionData: Connection) => {
    try {
      // Validate connection data before sending to the service
      if (!connectionData.connectionName || !connectionData.connectionString) {
        showSnackbar('Connection name and connection string are required', 'error');
        return;
      }

      // Get the numeric value for connectionAccessLevel
      let connectionAccessLevelValue: number;

      if (connectionData.connectionAccessLevel) {
        // Convert string enum to numeric value
        switch (connectionData.connectionAccessLevel) {
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
          (connectionData.isSource && connectionData.isDestination) ? 2 : // ReadWrite
          connectionData.isSource ? 0 : // ReadOnly
          connectionData.isDestination ? 1 : 0; // WriteOnly, default to ReadOnly
      }

      // Format the connection data correctly for the API
      const formattedConnection = {
        ...connectionData,
        connectionAccessLevel: connectionAccessLevelValue,
        // Add required fields that were missing
        createdBy: "System",
        lastModifiedBy: "System",
        createdOn: connectionData.createdOn || new Date().toISOString(),
        lastModifiedOn: new Date().toISOString()
      };

      console.log('Saving connection with data:', formattedConnection);

      // Call the service to save the connection
      const result = await DataTransferService.saveConnection(formattedConnection);

      // Close the dialog and refresh the connections list
      setIsConnectionDialogOpen(false);
      await loadConnections();

      // Show success message
      showSnackbar(
        connectionData.connectionId
          ? `Connection "${connectionData.connectionName}" updated successfully`
          : `Connection "${connectionData.connectionName}" created successfully`,
        'success'
      );

      return result;
    } catch (error: any) {
      console.error('Error saving connection:', error);

      // Check if this is a conflict error (409) with an existing connection
      if (error.response && error.response.status === 409) {
        const { message, existingConnectionId } = error.response.data || {};

        if (existingConnectionId) {
          // If we have the ID of the existing connection, ask if the user wants to edit it instead
          if (window.confirm(`${message || 'A connection with this name already exists.'} Would you like to edit the existing connection instead?`)) {
            // Find the existing connection and open it for editing
            const existingConnection = connections.find(c => c.connectionId === existingConnectionId);
            if (existingConnection) {
              handleOpenConnectionDialog(existingConnection);
              return;
            }
          }
        } else {
          // Just show the conflict message
          showSnackbar(message || 'A connection with this name already exists', 'warning');
        }
      } else if (error.name === 'SaveConnectionError') {
        // Show the specific error message from our enhanced error
        showSnackbar(error.message, 'error');
      } else if (error.response && error.response.data) {
        // Show the error message from the API response if available
        const errorMessage = error.response.data.message || error.response.data.error || 'Failed to save connection';
        showSnackbar(errorMessage, 'error');
      } else {
        // Show a generic error message for other errors
        showSnackbar(`Error saving connection: ${error.message || 'Unknown error'}`, 'error');
      }

      // Return null to indicate failure
      return null;
    }
  };

  const handleSaveConfig = async (configData: Configuration) => {
    try {
      await DataTransferService.saveConfiguration(configData);
      setIsConfigDialogOpen(false);
      await loadConfigurations();
      showSnackbar('Configuration saved successfully', 'success');
    } catch (error) {
      console.error('Error saving configuration:', error);
      showSnackbar('Error saving configuration', 'error');
    }
  };

  const handleExecuteTransfer = async (configId: number) => {
    try {
      await DataTransferService.executeTransfer(configId);
      showSnackbar('Data transfer started successfully', 'success');
      // Reload run history after a short delay to show the new run
      setTimeout(() => loadRunHistory(configId), 1000);
    } catch (error) {
      console.error('Error executing data transfer:', error);
      showSnackbar('Error executing data transfer', 'error');
    }
  };

  const handleTestConfiguration = async (configId: number) => {
    try {
      setIsLoading(true);
      const response = await DataTransferService.testConfiguration(configId);

      if (response.overallSuccess) {
        showSnackbar(`All connections successful for "${response.configurationName}"`, 'success');
      } else {
        // Determine which connection failed
        if (!response.source.success && !response.destination.success) {
          showSnackbar(`Both source and destination connections failed for "${response.configurationName}"`, 'error');
        } else if (!response.source.success) {
          showSnackbar(`Source connection failed for "${response.configurationName}": ${response.source.message}`, 'error');
        } else if (!response.destination.success) {
          showSnackbar(`Destination connection failed for "${response.configurationName}": ${response.destination.message}`, 'error');
        }
      }
    } catch (error) {
      console.error('Error testing configuration:', error);
      showSnackbar('Error testing configuration connections', 'error');
    } finally {
      setIsLoading(false);
    }
  };

  const handleViewRunDetails = (run: RunHistoryItem) => {
    loadRunDetails(run.runId);
  };

  const handleFilterRunHistory = (configId: number) => {
    setSelectedConfig(configurations.find(c => c.configurationId === configId) || null);
    loadRunHistory(configId);
    setActiveTab(2); // Switch to history tab
  };

  const handleCloseRunDetails = () => {
    setIsRunDetailsDialogOpen(false);
  };

  const handleTestConnection = async (connection: Connection) => {
    try {
      setIsLoading(true);

      // Get the numeric value for connectionAccessLevel
      let connectionAccessLevelValue: number;

      if (connection.connectionAccessLevel) {
        // Convert string enum to numeric value
        switch (connection.connectionAccessLevel) {
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
          (connection.isSource && connection.isDestination) ? 2 : // ReadWrite
          connection.isSource ? 0 : // ReadOnly
          connection.isDestination ? 1 : 0; // WriteOnly, default to ReadOnly
      }

      // Format the connection data correctly for the API
      const connectionData = {
        connectionId: connection.connectionId,
        connectionName: connection.connectionName,
        connectionString: connection.connectionString,
        description: connection.description || '',
        connectionAccessLevel: connectionAccessLevelValue,
        isActive: connection.isActive,
        maxPoolSize: connection.maxPoolSize || 100,
        minPoolSize: connection.minPoolSize || 5,
        timeout: connection.timeout || 30,
        encrypt: connection.encrypt !== undefined ? connection.encrypt : true,
        trustServerCertificate: connection.trustServerCertificate !== undefined ? connection.trustServerCertificate : true,
        // Add required fields that were missing
        createdBy: "System",
        lastModifiedBy: "System", // This was the missing required field
        createdOn: connection.createdOn || new Date().toISOString(),
        lastModifiedOn: connection.lastModifiedOn || new Date().toISOString()
      };

      console.log('Testing connection with data:', connectionData);
      const response = await DataTransferService.testConnection(connectionData);

      if (response.success) {
        showSnackbar(`Connection successful to ${response.database} on ${response.server}`, 'success');
      } else {
        showSnackbar(`Connection failed: ${response.message}`, 'error');
        console.error('Connection test failed:', response);
      }
    } catch (error) {
      console.error('Error testing connection:', error);
      showSnackbar('Error testing connection', 'error');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <Box sx={{ width: '100%', height: '100%', display: 'flex', flexDirection: 'column' }}>
      <PageHeader
        title="Data Transfer Management"
        subtitle="Configure and manage incremental data transfers between databases"
      />

      <Box sx={{ width: '100%', flexGrow: 1, display: 'flex', flexDirection: 'column' }}>
        <Box sx={{ borderBottom: 1, borderColor: 'divider' }}>
          <Tabs value={activeTab} onChange={handleTabChange} aria-label="data transfer tabs">
            <Tab icon={<DnsIcon />} iconPosition="start" label="Configurations" />
            <Tab icon={<StorageIcon />} iconPosition="start" label="Connections" />
            <Tab icon={<HistoryIcon />} iconPosition="start" label="History" />
          </Tabs>
        </Box>

        {/* Configurations Tab */}
        <TabPanel value={activeTab} index={0}>
          <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 2 }}>
            <Typography variant="h6" component="h2">
              Data Transfer Configurations
            </Typography>
            <Box>
              <Button
                variant="outlined"
                startIcon={<RefreshIcon />}
                onClick={() => loadConfigurations()}
                sx={{ mr: 1 }}
              >
                Refresh
              </Button>
              <Button
                variant="contained"
                startIcon={<AddIcon />}
                onClick={() => handleOpenConfigDialog()}
              >
                New Configuration
              </Button>
            </Box>
          </Box>
          <ConfigurationsTable
            configurations={configurations}
            connections={connections}
            onEdit={handleOpenConfigDialog}
            onExecute={handleExecuteTransfer}
            onTest={handleTestConfiguration}
            onViewHistory={handleFilterRunHistory}
            isLoading={isLoading}
          />
        </TabPanel>

        {/* Connections Tab */}
        <TabPanel value={activeTab} index={1}>
          <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 2 }}>
            <Typography variant="h6" component="h2">
              Database Connections
            </Typography>
            <Box>
              <Button
                variant="outlined"
                startIcon={<RefreshIcon />}
                onClick={() => loadConnections()}
                sx={{ mr: 1 }}
              >
                Refresh
              </Button>
              <Button
                variant="contained"
                startIcon={<AddIcon />}
                onClick={() => handleOpenConnectionDialog()}
              >
                New Connection
              </Button>
            </Box>
          </Box>
          <ConnectionsTable
            connections={connections}
            onEdit={handleOpenConnectionDialog}
            onTest={handleTestConnection}
            isLoading={isLoading}
          />
        </TabPanel>

        {/* History Tab */}
        <TabPanel value={activeTab} index={2}>
          <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 2 }}>
            <Typography variant="h6" component="h2">
              {selectedConfig
                ? `Run History for: ${selectedConfig.configurationName}`
                : 'Run History (All Configurations)'}
            </Typography>
            <Box>
              <Button
                variant="outlined"
                startIcon={<RefreshIcon />}
                onClick={() => loadRunHistory(selectedConfig?.configurationId || 0)}
                sx={{ mr: 1 }}
              >
                Refresh
              </Button>
              {selectedConfig && (
                <Button
                  variant="outlined"
                  color="secondary"
                  onClick={() => {
                    setSelectedConfig(null);
                    loadRunHistory(0);
                  }}
                  sx={{ mr: 1 }}
                >
                  Show All
                </Button>
              )}
            </Box>
          </Box>
          <RunHistoryTable
            runHistory={runHistory}
            onViewDetails={handleViewRunDetails}
            isLoading={isLoading}
          />
        </TabPanel>
      </Box>

      {/* Connection Dialog */}
      <ConnectionDialog
        open={isConnectionDialogOpen}
        connection={selectedConfig}
        onClose={() => setIsConnectionDialogOpen(false)}
        onSave={handleSaveConnection}
      />

      {/* Configuration Dialog */}
      <ConfigurationDialog
        open={isConfigDialogOpen}
        configuration={selectedConfig}
        connections={connections}
        onClose={() => setIsConfigDialogOpen(false)}
        onSave={handleSaveConfig}
      />

      {/* Run Details Dialog */}
      <RunDetailsDialog
        open={isRunDetailsDialogOpen}
        runDetails={runDetails}
        onClose={handleCloseRunDetails}
      />
    </Box>
  );
}