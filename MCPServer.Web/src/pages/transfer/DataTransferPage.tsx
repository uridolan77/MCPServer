import React, { useState, useEffect } from 'react';
import {
  Box,
  Button,
  Tab,
  Tabs,
  Typography,
  Card,
  CardContent,
  Grid,
} from '@mui/material';
import {
  Add as AddIcon,
  Refresh as RefreshIcon,
  Storage as StorageIcon,
  Dns as DnsIcon,
  History as HistoryIcon,
  Schema as SchemaIcon,
  AutoAwesomeMosaic as AutoAwesomeMosaicIcon, // Or Layers as LayersIcon
} from '@mui/icons-material';
import { PageHeader } from '@/components';
import { useSnackbar } from '@/hooks/useSnackbar';
import { Link as RouterLink } from 'react-router-dom';
import { Connection, ConnectionAccessLevel, ConnectionTestResult } from './types/Connection';
import ConnectionService from '@/services/connection.service';
import ConfigurationService from '@/services/configuration.service';
import RunHistoryService from '@/services/runHistory.service';
import ConnectionsTable from './components/ConnectionsTable';
import ConfigurationsTable from './components/ConfigurationsTable';
import ConnectionDialog from './components/ConnectionDialog';
import ConfigurationDialog from './components/ConfigurationDialog';
import RunHistoryTable from './components/RunHistoryTable';
import RunDetailsDialog from './components/RunDetailsDialog';
import { SemanticLayerAlignmentWizard } from '@/components/SemanticLayerAlignmentWizard'; // Added import

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

const ConnectionModel: Connection = {
  connectionId: 0,
  connectionName: '',
  connectionString: '',
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
  connectionAccessLevel: ConnectionAccessLevel.ReadOnly,
  createdBy: 'System',
  createdOn: new Date().toISOString(),
  lastModifiedBy: 'System',
  lastModifiedOn: null,
  lastTestedOn: null,
  isSource: true,
  isDestination: false
};

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
  const [selectedConnection, setSelectedConnection] = useState<Connection | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [isConfigDialogOpen, setIsConfigDialogOpen] = useState(false);
  const [isConnectionDialogOpen, setIsConnectionDialogOpen] = useState(false);
  const [isRunDetailsDialogOpen, setIsRunDetailsDialogOpen] = useState(false);
  const [runDetails, setRunDetails] = useState<any>(null);
  const [isSemanticWizardOpen, setIsSemanticWizardOpen] = useState(false); // Added state for new wizard
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
      const configurationsArray = await ConfigurationService.getConfigurations();
      console.log('Configurations array from service:', configurationsArray);
      console.log('Configurations array length:', configurationsArray.length);

      // Ensure we're setting a valid array to the state
      if (!Array.isArray(configurationsArray)) {
        console.error('configurationsArray is not an array!', configurationsArray);
        setConfigurations([]);
        return [];
      }

      setConfigurations(configurationsArray);
      return configurationsArray;
    } catch (error) {
      console.error('Error loading configurations:', error);
      showSnackbar('Error loading configurations', 'error');
      throw error;
    }
  };

  const loadConnections = async () => {
    try {
      const connectionsArray = await ConnectionService.getConnections();
      console.log('Connections array from service:', connectionsArray);
      console.log('Connections array length:', connectionsArray.length);

      // Ensure we're setting a valid array to the state
      if (!Array.isArray(connectionsArray)) {
        console.error('connectionsArray is not an array!', connectionsArray);
        setConnections([]);
        return [];
      }

      setConnections(connectionsArray);
      return connectionsArray;
    } catch (error) {
      console.error('Error loading connections:', error);
      showSnackbar('Error loading connections', 'error');
      throw error;
    }
  };

  const loadRunHistory = async (configId: number = 0) => {
    try {
      const historyArray = await RunHistoryService.getRunHistory(configId);
      console.log('Run history array from service:', historyArray);
      console.log('Run history array length:', historyArray.length);

      // Ensure we're setting a valid array to the state
      if (!Array.isArray(historyArray)) {
        console.error('historyArray is not an array!', historyArray);
        setRunHistory([]);
        return [];
      }

      setRunHistory(historyArray);
      return historyArray;
    } catch (error) {
      console.error('Error loading run history:', error);
      showSnackbar('Error loading run history', 'error');
      throw error;
    }
  };

  const loadRunDetails = async (runId: number) => {
    try {
      const response = await RunHistoryService.getRunDetails(runId);
      setRunDetails(response);
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
    console.log('Opening connection for edit:', connection);
    setSelectedConnection(connection);
    setIsConnectionDialogOpen(true);
  };

  const handleSaveConnection = async (connection: Connection) => {
    try {
      console.log('Saving connection with data:', connection);
      
      // Save connection using the ConnectionService
      const response = await ConnectionService.saveConnection(connection);
      
      showSnackbar('Connection saved successfully', 'success');
      await loadConnections();
      setIsConnectionDialogOpen(false);
    } catch (error) {
      console.error('Error saving connection:', error);
      showSnackbar('Error saving connection', 'error');
    }
  };

  const handleSaveConfig = async (configData: Configuration) => {
    try {
      await ConfigurationService.saveConfiguration(configData);
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
      await ConfigurationService.executeTransfer(configId);
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
      const response = await ConfigurationService.testConfiguration(configId);

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

  const handleOpenSemanticWizard = () => { // Added handler
    setIsSemanticWizardOpen(true);
  };

  const handleCloseSemanticWizard = () => { // Added handler
    setIsSemanticWizardOpen(false);
  };

  const handleTestConnection = async (connection: Connection) => {
    try {
      setIsLoading(true);
      
      console.log('Testing connection with data:', connection);
      
      const response = await ConnectionService.testConnection(connection);
      
      if (response.success) {
        showSnackbar('Connection test successful', 'success');
      } else {
        showSnackbar(`Connection test failed: ${response.message}`, 'error');
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

        {/* Tools Cards - Added before tab panels */}
        {activeTab === 0 && (
          <Box sx={{ p: 2 }}>
            <Grid container spacing={2}>
              <Grid item xs={12} md={6} lg={4}>
                <Card variant="outlined">
                  <CardContent>
                    <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
                      <SchemaIcon color="primary" sx={{ mr: 1 }} />
                      <Typography variant="h6" component="h3">
                        Database Schema Mapper
                      </Typography>
                    </Box>
                    <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                      Map database schemas, select tables and columns, and generate schema metadata JSON for data transfer.
                    </Typography>
                    <Button
                      variant="outlined"
                      component={RouterLink}
                      to="/transfer/schema-mapper"
                      fullWidth
                    >
                      Open Schema Mapper
                    </Button>
                  </CardContent>
                </Card>
              </Grid>

              <Grid item xs={12} md={6} lg={4}>
                <Card variant="outlined">
                  <CardContent>
                    <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
                      <AddIcon color="primary" sx={{ mr: 1 }} />
                      <Typography variant="h6" component="h3">
                        New Configuration
                      </Typography>
                    </Box>
                    <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                      Create a new data transfer configuration between source and destination databases.
                    </Typography>
                    <Button
                      variant="contained"
                      startIcon={<AddIcon />}
                      onClick={() => handleOpenConfigDialog()}
                      fullWidth
                    >
                      New Configuration
                    </Button>
                  </CardContent>
                </Card>
              </Grid>

              {/* New Card for Semantic Layer Alignment Wizard */}
              <Grid item xs={12} md={6} lg={4}>
                <Card variant="outlined">
                  <CardContent>
                    <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
                      <AutoAwesomeMosaicIcon color="primary" sx={{ mr: 1 }} />
                      <Typography variant="h6" component="h3">
                        Semantic Layer Alignment
                      </Typography>
                    </Box>
                    <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                      Align database schemas with semantic layer ontology definitions (SLOD).
                    </Typography>
                    <Button
                      variant="outlined"
                      onClick={handleOpenSemanticWizard}
                      fullWidth
                    >
                      Open Alignment Wizard
                    </Button>
                  </CardContent>
                </Card>
              </Grid>
            </Grid>
          </Box>
        )}

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
        connection={selectedConnection}
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

      {/* Semantic Layer Alignment Wizard Dialog */}
      <SemanticLayerAlignmentWizard
        open={isSemanticWizardOpen}
        onClose={handleCloseSemanticWizard}
        onComplete={(data) => {
          console.log('Semantic Layer Alignment Wizard completed', data);
          // Potentially handle completion, e.g., show a snackbar
          showSnackbar('Semantic Layer Alignment Wizard completed successfully!', 'success');
          handleCloseSemanticWizard();
        }}
      />
    </Box>
  );
}