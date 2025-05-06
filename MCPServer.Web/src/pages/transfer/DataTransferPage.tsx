import React, { useState, useEffect } from 'react';
import {
  Box,
  Button,
  Card,
  CircularProgress,
  Container,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Divider,
  Grid,
  IconButton,
  Paper,
  Tab,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Tabs,
  TextField,
  Typography,
  Switch,
  FormControlLabel,
  Select,
  MenuItem,
  FormControl,
  InputLabel,
  Snackbar,
  Alert,
  List,
  ListItem,
  ListItemIcon,
  ListItemText,
  Chip,
} from '@mui/material';
import {
  Add as AddIcon,
  Delete as DeleteIcon,
  Edit as EditIcon,
  PlayArrow as PlayArrowIcon,
  Refresh as RefreshIcon,
  Visibility as VisibilityIcon,
  Storage as StorageIcon,
  Dns as DnsIcon,
  Schedule as ScheduleIcon,
  History as HistoryIcon,
} from '@mui/icons-material';
import { DataGrid } from '@mui/x-data-grid';
import { PageHeader } from '@/components';
import axios from '@/lib/axios';
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
    description?: string;
    isSource?: boolean;
    isDestination?: boolean;
    isActive?: boolean;
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
      // Check if the response has a $values property (API response format)
      if (response && response.$values) {
        setConfigurations(response.$values);
        return response.$values;
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
      // Check if the response has a $values property (API response format)
      if (response && response.$values) {
        setConnections(response.$values);
        return response.$values;
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
      // Check if the response has a $values property (API response format)
      if (response && response.$values) {
        setRunHistory(response.$values);
        return response.$values;
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
      await DataTransferService.saveConnection(connectionData);
      setIsConnectionDialogOpen(false);
      await loadConnections();
      showSnackbar('Connection saved successfully', 'success');
    } catch (error: any) {
      console.error('Error saving connection:', error);

      // Check if this is a conflict error (409) with an existing connection
      if (error.response && error.response.status === 409) {
        const { message, existingConnectionId } = error.response.data;

        if (existingConnectionId) {
          // If we have the ID of the existing connection, ask if the user wants to edit it instead
          if (window.confirm(`${message} Would you like to edit the existing connection instead?`)) {
            // Find the existing connection and open it for editing
            const existingConnection = connections.find(c => c.connectionId === existingConnectionId);
            if (existingConnection) {
              handleOpenConnectionDialog(existingConnection);
              return;
            }
          }
        } else {
          // Just show the conflict message
          showSnackbar(message, 'warning');
        }
      } else {
        // Show a generic error message for other errors
        showSnackbar('Error saving connection', 'error');
      }
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

  return (
    <Container maxWidth="xl">
      <PageHeader
        title="Data Transfer Management"
        subtitle="Configure and manage incremental data transfers between databases"
      />

      <Box sx={{ width: '100%' }}>
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
    </Container>
  );
}