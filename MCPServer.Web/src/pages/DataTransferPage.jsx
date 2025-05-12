/*
 * DataTransferConnections table fields:
 * - ConnectionId (PK, int, not null)
 * - ConnectionName (nvarchar(100), not null)
 * - ConnectionString (nvarchar(1000), not null)
 * - Description (nvarchar(500), null)
 * - IsSource (bit, not null)
 * - IsDestination (bit, not null)
 * - IsActive (bit, not null)
 * - CreatedBy (nvarchar(100), not null)
 * - CreatedOn (datetime2(7), not null)
 * - LastModifiedBy (nvarchar(100), null)
 * - LastModifiedOn (datetime2(7), null)
 * - ConnectionAccessLevel (nvarchar(20), not null)
 * - LastTestedOn (datetime2(7), null)
 * - MaxPoolSize (int, not null)
 * - MinPoolSize (int, not null)
 * - Timeout (int, not null)
 * - Encrypt (bit, not null)
 * - TrustServerCertificate (bit, not null)
 */
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
  CloudUpload as CloudUploadIcon,
  CloudDownload as CloudDownloadIcon,
  Database as DatabaseIcon,
  Schedule as ScheduleIcon,
  History as HistoryIcon,
} from '@mui/icons-material';
import { DataGrid } from '@mui/x-data-grid';
import axios from 'axios';

function TabPanel(props) {
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
  const [configurations, setConfigurations] = useState([]);
  const [connections, setConnections] = useState([]);
  const [runHistory, setRunHistory] = useState([]);
  const [selectedConfig, setSelectedConfig] = useState(null);
  const [selectedRun, setSelectedRun] = useState(null);
  const [isLoading, setIsLoading] = useState(false);
  const [isConfigDialogOpen, setIsConfigDialogOpen] = useState(false);
  const [isConnectionDialogOpen, setIsConnectionDialogOpen] = useState(false);
  const [isRunDetailsDialogOpen, setIsRunDetailsDialogOpen] = useState(false);
  const [runDetails, setRunDetails] = useState(null);
  const [newConnection, setNewConnection] = useState({
    connectionName: '',
    connectionString: '',
    description: '',
    isSource: true,
    isDestination: true,
    isActive: true,
  });
  const [newConfig, setNewConfig] = useState({
    configurationName: '',
    description: '',
    sourceConnectionId: '',
    destinationConnectionId: '',
    batchSize: 5000,
    reportingFrequency: 10,
    isActive: true,
    tableMappings: [],
    schedules: [],
  });
  const [newTableMapping, setNewTableMapping] = useState({
    schemaName: '',
    tableName: '',
    timestampColumnName: '',
    orderByColumn: '',
    customWhereClause: '',
    isActive: true,
    priority: 100,
  });
  const [newSchedule, setNewSchedule] = useState({
    scheduleType: 'Daily',
    startTime: '00:00',
    frequency: 1,
    frequencyUnit: 'Day',
    weekDays: 'Mon,Tue,Wed,Thu,Fri',
    monthDays: '1',
    isActive: true,
  });
  const [snackbar, setSnackbar] = useState({
    open: false,
    message: '',
    severity: 'success',
  });

  useEffect(() => {
    loadConfigurations();
    loadConnections();
    loadRunHistory();
  }, []);

  const useMockData = async () => {
    try {
      const response = await axios.post(`/api/data-transfer/use-mock-data`);
      setConfigurations(response.data);
      showSnackbar('Using mock data for demonstration', 'info');
    } catch (error) {
      console.error('Error loading mock data:', error);
      showSnackbar('Error loading mock data', 'error');
    }
  };

  const loadConfigurations = async () => {
    setIsLoading(true);
    try {
      const response = await axios.get(`/api/data-transfer/configurations`);
      setConfigurations(response.data);
    } catch (error) {
      console.error('Error loading configurations:', error);
      showSnackbar('Error loading configurations', 'error');
      // Try to use mock data if real data fails
      try {
        const mockResponse = await axios.post(`/api/data-transfer/use-mock-data`);
        setConfigurations(mockResponse.data);
        showSnackbar('Using mock data for demonstration', 'info');
      } catch (mockError) {
        console.error('Error loading mock data:', mockError);
      }
    } finally {
      setIsLoading(false);
    }
  };

  const loadConnections = async () => {
    try {
      const response = await axios.get(`/api/data-transfer/connections`);
      setConnections(response.data);
    } catch (error) {
      console.error('Error loading connections:', error);
      showSnackbar('Error loading connections', 'error');
    }
  };

  const loadRunHistory = async (configId = 0) => {
    try {
      const response = await axios.get(`/api/data-transfer/history`, {
        params: { configurationId: configId, limit: 50 }
      });
      setRunHistory(response.data);
    } catch (error) {
      console.error('Error loading run history:', error);
      showSnackbar('Error loading run history', 'error');
    }
  };

  const loadRunDetails = async (runId) => {
    try {
      const response = await axios.get(`/api/data-transfer/runs/${runId}`);
      setRunDetails(response.data);
      setIsRunDetailsDialogOpen(true);
    } catch (error) {
      console.error('Error loading run details:', error);
      showSnackbar('Error loading run details', 'error');
    }
  };

  const handleTabChange = (event, newValue) => {
    setActiveTab(newValue);
  };

  const handleOpenConfigDialog = (config = null) => {
    if (config) {
      setNewConfig({
        ...config,
        sourceConnectionId: config.sourceConnection.connectionId,
        destinationConnectionId: config.destinationConnection.connectionId,
      });
      setSelectedConfig(config);
    } else {
      setNewConfig({
        configurationName: '',
        description: '',
        sourceConnectionId: '',
        destinationConnectionId: '',
        batchSize: 5000,
        reportingFrequency: 10,
        isActive: true,
        tableMappings: [],
        schedules: [],
      });
      setSelectedConfig(null);
    }
    setIsConfigDialogOpen(true);
  };

  const handleOpenConnectionDialog = (connection = null) => {
    if (connection) {
      console.log('Opening connection for edit:', connection);
      
      // Use exact field names from the database schema
      setNewConnection({
        connectionId: connection.connectionId,
        connectionName: connection.connectionName || '',
        connectionString: connection.connectionString || '',
        description: connection.description || '',
        isSource: connection.isSource === true,
        isDestination: connection.isDestination === true,
        isActive: connection.isActive === true,
        connectionAccessLevel: connection.connectionAccessLevel,
        maxPoolSize: connection.maxPoolSize || 100,
        minPoolSize: connection.minPoolSize || 5,
        timeout: connection.timeout || 30,
        encrypt: connection.encrypt === true,
        trustServerCertificate: connection.trustServerCertificate === true
      });
    } else {
      setNewConnection({
        connectionName: '',
        connectionString: '',
        description: '',
        isSource: true,
        isDestination: true,
        isActive: true,
        maxPoolSize: 100,
        minPoolSize: 5,
        timeout: 30,
        encrypt: true,
        trustServerCertificate: true
      });
    }
    setIsConnectionDialogOpen(true);
  };

  const handleSaveConnection = async () => {
    try {
      console.log('Saving connection:', newConnection);
      let response;
      
      if (newConnection.connectionId) {
        // Update existing connection
        response = await axios.put(`/api/data-transfer/connections/${newConnection.connectionId}`, newConnection);
        showSnackbar(`Connection "${newConnection.connectionName}" updated successfully`, 'success');
      } else {
        // Create new connection
        response = await axios.post(`/api/data-transfer/connections`, newConnection);
        showSnackbar(`Connection "${newConnection.connectionName}" created successfully`, 'success');
      }
      
      setIsConnectionDialogOpen(false);
      await loadConnections();
    } catch (error) {
      console.error('Error saving connection:', error);
      showSnackbar('Error saving connection', 'error');
    }
  };

  const handleSaveConfig = async () => {
    try {
      // Prepare the configuration DTO
      const configToSave = {
        ...newConfig,
        sourceConnection: { connectionId: newConfig.sourceConnectionId },
        destinationConnection: { connectionId: newConfig.destinationConnectionId }
      };

      const response = await axios.post(`/api/data-transfer/configurations`, configToSave);
      setIsConfigDialogOpen(false);
      loadConfigurations();
      showSnackbar('Configuration saved successfully', 'success');
    } catch (error) {
      console.error('Error saving configuration:', error);
      showSnackbar('Error saving configuration', 'error');
    }
  };

  const handleAddTableMapping = () => {
    setNewConfig({
      ...newConfig,
      tableMappings: [...newConfig.tableMappings, { ...newTableMapping, mappingId: 0 }]
    });
    setNewTableMapping({
      schemaName: '',
      tableName: '',
      timestampColumnName: '',
      orderByColumn: '',
      customWhereClause: '',
      isActive: true,
      priority: 100,
    });
  };

  const handleRemoveTableMapping = (index) => {
    const updatedMappings = [...newConfig.tableMappings];
    updatedMappings.splice(index, 1);
    setNewConfig({ ...newConfig, tableMappings: updatedMappings });
  };

  const handleAddSchedule = () => {
    // Convert startTime string to TimeSpan
    const startTimeParts = newSchedule.startTime.split(':');
    const hours = parseInt(startTimeParts[0], 10);
    const minutes = parseInt(startTimeParts[1], 10);

    setNewConfig({
      ...newConfig,
      schedules: [...newConfig.schedules, {
        ...newSchedule,
        scheduleId: 0,
        startTime: `${hours}:${minutes}:00`
      }]
    });
    setNewSchedule({
      scheduleType: 'Daily',
      startTime: '00:00',
      frequency: 1,
      frequencyUnit: 'Day',
      weekDays: 'Mon,Tue,Wed,Thu,Fri',
      monthDays: '1',
      isActive: true,
    });
  };

  const handleRemoveSchedule = (index) => {
    const updatedSchedules = [...newConfig.schedules];
    updatedSchedules.splice(index, 1);
    setNewConfig({ ...newConfig, schedules: updatedSchedules });
  };

  const handleExecuteTransfer = async (configId) => {
    try {
      const response = await axios.post(`/api/data-transfer/configurations/${configId}/execute`);
      showSnackbar('Data transfer started successfully', 'success');
      // Reload run history after a short delay to show the new run
      setTimeout(() => loadRunHistory(), 1000);
    } catch (error) {
      console.error('Error executing data transfer:', error);
      showSnackbar('Error executing data transfer', 'error');
    }
  };

  const showSnackbar = (message, severity) => {
    setSnackbar({
      open: true,
      message,
      severity,
    });
  };

  const handleCloseSnackbar = () => {
    setSnackbar({
      ...snackbar,
      open: false,
    });
  };

  // Configuration columns for DataGrid
  const configColumns = [
    { field: 'configurationId', headerName: 'ID', width: 70 },
    { field: 'configurationName', headerName: 'Name', width: 200 },
    { field: 'description', headerName: 'Description', width: 250 },
    {
      field: 'sourceConnection',
      headerName: 'Source',
      width: 150,
      valueGetter: (params) => params.row.sourceConnection?.connectionName || '',
    },
    {
      field: 'destinationConnection',
      headerName: 'Destination',
      width: 150,
      valueGetter: (params) => params.row.destinationConnection?.connectionName || '',
    },
    {
      field: 'tableMappings',
      headerName: 'Tables',
      width: 100,
      valueGetter: (params) => params.row.tableMappings?.length || 0,
    },
    {
      field: 'isActive',
      headerName: 'Active',
      width: 100,
      renderCell: (params) => (
        <Chip
          label={params.row.isActive ? 'Active' : 'Inactive'}
          color={params.row.isActive ? 'success' : 'default'}
          size="small"
        />
      ),
    },
    {
      field: 'actions',
      headerName: 'Actions',
      width: 200,
      renderCell: (params) => (
        <Box>
          <IconButton
            color="primary"
            onClick={() => handleOpenConfigDialog(params.row)}
            title="Edit"
          >
            <EditIcon />
          </IconButton>
          <IconButton
            color="success"
            onClick={() => handleExecuteTransfer(params.row.configurationId)}
            title="Execute"
          >
            <PlayArrowIcon />
          </IconButton>
          <IconButton
            color="info"
            onClick={() => {
              setSelectedConfig(params.row);
              loadRunHistory(params.row.configurationId);
              setActiveTab(2);
            }}
            title="History"
          >
            <HistoryIcon />
          </IconButton>
        </Box>
      ),
    },
  ];

  // Connection columns for DataGrid
  const connectionColumns = [
    { field: 'connectionId', headerName: 'ID', width: 70 },
    { field: 'connectionName', headerName: 'Name', width: 200 },
    { field: 'description', headerName: 'Description', width: 250 },
    {
      field: 'isSource',
      headerName: 'Source',
      width: 100,
      renderCell: (params) => (
        <Chip
          label={params.row.isSource ? 'Yes' : 'No'}
          color={params.row.isSource ? 'success' : 'default'}
          size="small"
        />
      ),
    },
    {
      field: 'isDestination',
      headerName: 'Destination',
      width: 100,
      renderCell: (params) => (
        <Chip
          label={params.row.isDestination ? 'Yes' : 'No'}
          color={params.row.isDestination ? 'success' : 'default'}
          size="small"
        />
      ),
    },
    {
      field: 'isActive',
      headerName: 'Active',
      width: 100,
      renderCell: (params) => (
        <Chip
          label={params.row.isActive ? 'Active' : 'Inactive'}
          color={params.row.isActive ? 'success' : 'default'}
          size="small"
        />
      ),
    },
    {
      field: 'actions',
      headerName: 'Actions',
      width: 150,
      renderCell: (params) => (
        <Box>
          <IconButton
            color="primary"
            onClick={() => handleOpenConnectionDialog(params.row)}
            title="Edit"
          >
            <EditIcon />
          </IconButton>
        </Box>
      ),
    },
  ];

  // Run history columns for DataGrid
  const runHistoryColumns = [
    { field: 'runId', headerName: 'ID', width: 70 },
    { field: 'configurationName', headerName: 'Configuration', width: 200 },
    {
      field: 'startTime',
      headerName: 'Start Time',
      width: 180,
      valueFormatter: (params) => new Date(params.value).toLocaleString(),
    },
    {
      field: 'endTime',
      headerName: 'End Time',
      width: 180,
      valueFormatter: (params) => params.value ? new Date(params.value).toLocaleString() : 'Running...',
    },
    {
      field: 'status',
      headerName: 'Status',
      width: 150,
      renderCell: (params) => {
        let color = 'default';
        if (params.value === 'Completed') color = 'success';
        else if (params.value === 'Failed') color = 'error';
        else if (params.value === 'Running') color = 'info';
        else if (params.value === 'CompletedWithErrors') color = 'warning';

        return (
          <Chip
            label={params.value}
            color={color}
            size="small"
          />
        );
      },
    },
    {
      field: 'totalTablesProcessed',
      headerName: 'Tables',
      width: 80,
      valueFormatter: (params) => params.value.toLocaleString(),
    },
    {
      field: 'totalRowsProcessed',
      headerName: 'Rows',
      width: 100,
      valueFormatter: (params) => params.value.toLocaleString(),
    },
    {
      field: 'elapsedMs',
      headerName: 'Duration',
      width: 120,
      valueFormatter: (params) => {
        if (!params.value) return 'Running...';
        const seconds = params.value / 1000;
        if (seconds < 60) return `${seconds.toFixed(2)}s`;
        const minutes = Math.floor(seconds / 60);
        const remainingSeconds = seconds % 60;
        return `${minutes}m ${remainingSeconds.toFixed(0)}s`;
      },
    },
    {
      field: 'actions',
      headerName: 'Actions',
      width: 100,
      renderCell: (params) => (
        <Box>
          <IconButton
            color="primary"
            onClick={() => loadRunDetails(params.row.runId)}
            title="View Details"
          >
            <VisibilityIcon />
          </IconButton>
        </Box>
      ),
    },
  ];

  return (
    <Container maxWidth="xl">
      <Typography variant="h4" component="h1" gutterBottom>
        Data Transfer Management
      </Typography>

      <Box sx={{ width: '100%' }}>
        <Box sx={{ borderBottom: 1, borderColor: 'divider' }}>
          <Tabs value={activeTab} onChange={handleTabChange} aria-label="data transfer tabs">
            <Tab icon={<DatabaseIcon />} iconPosition="start" label="Configurations" />
            <Tab icon={<CloudUploadIcon />} iconPosition="start" label="Connections" />
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
                onClick={loadConfigurations}
                sx={{ mr: 1 }}
              >
                Refresh
              </Button>
              <Button
                variant="outlined"
                color="secondary"
                startIcon={<CloudDownloadIcon />}
                onClick={useMockData}
                sx={{ mr: 1 }}
              >
                Use Mock Data
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
          <Paper sx={{ height: 500, width: '100%' }}>
            <DataGrid
              rows={configurations}
              columns={configColumns}
              pageSize={10}
              rowsPerPageOptions={[10, 25, 50]}
              getRowId={(row) => row.configurationId}
              loading={isLoading}
              disableSelectionOnClick
            />
          </Paper>
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
                onClick={loadConnections}
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
          <Paper sx={{ height: 500, width: '100%' }}>
            <DataGrid
              rows={connections}
              columns={connectionColumns}
              pageSize={10}
              rowsPerPageOptions={[10, 25, 50]}
              getRowId={(row) => row.connectionId}
              disableSelectionOnClick
            />
          </Paper>
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
          <Paper sx={{ height: 500, width: '100%' }}>
            <DataGrid
              rows={runHistory}
              columns={runHistoryColumns}
              pageSize={10}
              rowsPerPageOptions={[10, 25, 50]}
              getRowId={(row) => row.runId}
              disableSelectionOnClick
              sortModel={[{ field: 'startTime', sort: 'desc' }]}
            />
          </Paper>
        </TabPanel>
      </Box>

      {/* Connection Dialog */}
      <Dialog open={isConnectionDialogOpen} onClose={() => setIsConnectionDialogOpen(false)} maxWidth="md" fullWidth>
        <DialogTitle>
          {newConnection.connectionId ? 'Edit Connection' : 'New Connection'}
        </DialogTitle>
        <DialogContent>
          <Grid container spacing={2} sx={{ mt: 1 }}>
            <Grid item xs={12}>
              <TextField
                label="Connection Name"
                value={newConnection.connectionName || ''}
                onChange={(e) => setNewConnection({ ...newConnection, connectionName: e.target.value })}
                fullWidth
                required
              />
            </Grid>
            <Grid item xs={12}>
              <TextField
                label="Connection String"
                value={newConnection.connectionString || ''}
                onChange={(e) => setNewConnection({ ...newConnection, connectionString: e.target.value })}
                fullWidth
                required
                multiline
                rows={3}
              />
            </Grid>
            <Grid item xs={12}>
              <TextField
                label="Description"
                value={newConnection.description || ''}
                onChange={(e) => setNewConnection({ ...newConnection, description: e.target.value })}
                fullWidth
              />
            </Grid>
            <Grid item xs={4}>
              <FormControlLabel
                control={
                  <Switch
                    checked={newConnection.isSource}
                    onChange={(e) => setNewConnection({ ...newConnection, isSource: e.target.checked })}
                  />
                }
                label="Use as Source"
              />
            </Grid>
            <Grid item xs={4}>
              <FormControlLabel
                control={
                  <Switch
                    checked={newConnection.isDestination}
                    onChange={(e) => setNewConnection({ ...newConnection, isDestination: e.target.checked })}
                  />
                }
                label="Use as Destination"
              />
            </Grid>
            <Grid item xs={4}>
              <FormControlLabel
                control={
                  <Switch
                    checked={newConnection.isActive}
                    onChange={(e) => setNewConnection({ ...newConnection, isActive: e.target.checked })}
                  />
                }
                label="Active"
              />
            </Grid>
          </Grid>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setIsConnectionDialogOpen(false)}>Cancel</Button>
          <Button onClick={handleSaveConnection} variant="contained" color="primary">
            Save
          </Button>
        </DialogActions>
      </Dialog>

      {/* Configuration Dialog */}
      <Dialog open={isConfigDialogOpen} onClose={() => setIsConfigDialogOpen(false)} maxWidth="lg" fullWidth>
        <DialogTitle>
          {newConfig.configurationId ? 'Edit Configuration' : 'New Configuration'}
        </DialogTitle>
        <DialogContent>
          <Box sx={{ borderBottom: 1, borderColor: 'divider', mb: 2 }}>
            <Tabs value={0}>
              <Tab label="General" />
            </Tabs>
          </Box>

          <Grid container spacing={2}>
            <Grid item xs={12} md={6}>
              <TextField
                label="Configuration Name"
                value={newConfig.configurationName || ''}
                onChange={(e) => setNewConfig({ ...newConfig, configurationName: e.target.value })}
                fullWidth
                required
                margin="normal"
              />
            </Grid>
            <Grid item xs={12} md={6}>
              <TextField
                label="Description"
                value={newConfig.description || ''}
                onChange={(e) => setNewConfig({ ...newConfig, description: e.target.value })}
                fullWidth
                margin="normal"
              />
            </Grid>
            <Grid item xs={12} md={6}>
              <FormControl fullWidth margin="normal">
                <InputLabel>Source Connection</InputLabel>
                <Select
                  value={newConfig.sourceConnectionId || ''}
                  onChange={(e) => setNewConfig({ ...newConfig, sourceConnectionId: e.target.value })}
                  label="Source Connection"
                  required
                >
                  {connections
                    .filter(conn => conn.isSource)
                    .map(conn => (
                      <MenuItem key={conn.connectionId} value={conn.connectionId}>
                        {conn.connectionName}
                      </MenuItem>
                    ))
                  }
                </Select>
              </FormControl>
            </Grid>
            <Grid item xs={12} md={6}>
              <FormControl fullWidth margin="normal">
                <InputLabel>Destination Connection</InputLabel>
                <Select
                  value={newConfig.destinationConnectionId || ''}
                  onChange={(e) => setNewConfig({ ...newConfig, destinationConnectionId: e.target.value })}
                  label="Destination Connection"
                  required
                >
                  {connections
                    .filter(conn => conn.isDestination)
                    .map(conn => (
                      <MenuItem key={conn.connectionId} value={conn.connectionId}>
                        {conn.connectionName}
                      </MenuItem>
                    ))
                  }
                </Select>
              </FormControl>
            </Grid>
            <Grid item xs={12} md={4}>
              <TextField
                label="Batch Size"
                type="number"
                value={newConfig.batchSize || 5000}
                onChange={(e) => setNewConfig({ ...newConfig, batchSize: parseInt(e.target.value) })}
                fullWidth
                margin="normal"
              />
            </Grid>
            <Grid item xs={12} md={4}>
              <TextField
                label="Reporting Frequency"
                type="number"
                value={newConfig.reportingFrequency || 10}
                onChange={(e) => setNewConfig({ ...newConfig, reportingFrequency: parseInt(e.target.value) })}
                fullWidth
                margin="normal"
              />
            </Grid>
            <Grid item xs={12} md={4}>
              <FormControlLabel
                control={
                  <Switch
                    checked={newConfig.isActive}
                    onChange={(e) => setNewConfig({ ...newConfig, isActive: e.target.checked })}
                  />
                }
                label="Active"
                sx={{ mt: 3 }}
              />
            </Grid>
          </Grid>

          <Typography variant="h6" sx={{ mt: 4, mb: 2 }}>
            Table Mappings
          </Typography>

          <Grid container spacing={2}>
            <Grid item xs={12} md={3}>
              <TextField
                label="Schema Name"
                value={newTableMapping.schemaName || ''}
                onChange={(e) => setNewTableMapping({ ...newTableMapping, schemaName: e.target.value })}
                fullWidth
              />
            </Grid>
            <Grid item xs={12} md={3}>
              <TextField
                label="Table Name"
                value={newTableMapping.tableName || ''}
                onChange={(e) => setNewTableMapping({ ...newTableMapping, tableName: e.target.value })}
                fullWidth
              />
            </Grid>
            <Grid item xs={12} md={3}>
              <TextField
                label="Timestamp Column"
                value={newTableMapping.timestampColumnName || ''}
                onChange={(e) => setNewTableMapping({ ...newTableMapping, timestampColumnName: e.target.value })}
                fullWidth
              />
            </Grid>
            <Grid item xs={12} md={3}>
              <TextField
                label="Order By Column"
                value={newTableMapping.orderByColumn || ''}
                onChange={(e) => setNewTableMapping({ ...newTableMapping, orderByColumn: e.target.value })}
                fullWidth
              />
            </Grid>
            <Grid item xs={12} md={9}>
              <TextField
                label="Custom Where Clause"
                value={newTableMapping.customWhereClause || ''}
                onChange={(e) => setNewTableMapping({ ...newTableMapping, customWhereClause: e.target.value })}
                fullWidth
              />
            </Grid>
            <Grid item xs={12} md={2}>
              <FormControlLabel
                control={
                  <Switch
                    checked={newTableMapping.isActive}
                    onChange={(e) => setNewTableMapping({ ...newTableMapping, isActive: e.target.checked })}
                  />
                }
                label="Active"
              />
            </Grid>
            <Grid item xs={12} md={1}>
              <Button
                variant="contained"
                color="primary"
                onClick={handleAddTableMapping}
                fullWidth
              >
                Add
              </Button>
            </Grid>
          </Grid>

          {newConfig.tableMappings.length > 0 && (
            <TableContainer component={Paper} sx={{ mt: 2 }}>
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>Schema</TableCell>
                    <TableCell>Table</TableCell>
                    <TableCell>Timestamp Column</TableCell>
                    <TableCell>Order By</TableCell>
                    <TableCell>Where</TableCell>
                    <TableCell>Active</TableCell>
                    <TableCell>Actions</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {newConfig.tableMappings.map((mapping, index) => (
                    <TableRow key={index}>
                      <TableCell>{mapping.schemaName}</TableCell>
                      <TableCell>{mapping.tableName}</TableCell>
                      <TableCell>{mapping.timestampColumnName}</TableCell>
                      <TableCell>{mapping.orderByColumn}</TableCell>
                      <TableCell>{mapping.customWhereClause}</TableCell>
                      <TableCell>
                        <Chip
                          label={mapping.isActive ? 'Active' : 'Inactive'}
                          color={mapping.isActive ? 'success' : 'default'}
                          size="small"
                        />
                      </TableCell>
                      <TableCell>
                        <IconButton
                          size="small"
                          color="error"
                          onClick={() => handleRemoveTableMapping(index)}
                        >
                          <DeleteIcon />
                        </IconButton>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
          )}

          <Typography variant="h6" sx={{ mt: 4, mb: 2 }}>
            Schedules
          </Typography>

          <Grid container spacing={2}>
            <Grid item xs={12} md={2}>
              <FormControl fullWidth>
                <InputLabel>Type</InputLabel>
                <Select
                  value={newSchedule.scheduleType || 'Daily'}
                  onChange={(e) => setNewSchedule({ ...newSchedule, scheduleType: e.target.value })}
                  label="Type"
                >
                  <MenuItem value="Once">Once</MenuItem>
                  <MenuItem value="Daily">Daily</MenuItem>
                  <MenuItem value="Weekly">Weekly</MenuItem>
                  <MenuItem value="Monthly">Monthly</MenuItem>
                  <MenuItem value="Custom">Custom</MenuItem>
                </Select>
              </FormControl>
            </Grid>
            <Grid item xs={12} md={2}>
              <TextField
                label="Start Time"
                type="time"
                value={newSchedule.startTime || '00:00'}
                onChange={(e) => setNewSchedule({ ...newSchedule, startTime: e.target.value })}
                fullWidth
                InputLabelProps={{
                  shrink: true,
                }}
                inputProps={{
                  step: 300, // 5 min
                }}
              />
            </Grid>
            <Grid item xs={12} md={2}>
              <TextField
                label="Frequency"
                type="number"
                value={newSchedule.frequency || 1}
                onChange={(e) => setNewSchedule({ ...newSchedule, frequency: parseInt(e.target.value) })}
                fullWidth
                disabled={newSchedule.scheduleType === 'Once'}
              />
            </Grid>
            <Grid item xs={12} md={2}>
              <FormControl fullWidth disabled={newSchedule.scheduleType === 'Once'}>
                <InputLabel>Unit</InputLabel>
                <Select
                  value={newSchedule.frequencyUnit || 'Day'}
                  onChange={(e) => setNewSchedule({ ...newSchedule, frequencyUnit: e.target.value })}
                  label="Unit"
                >
                  <MenuItem value="Minute">Minute</MenuItem>
                  <MenuItem value="Hour">Hour</MenuItem>
                  <MenuItem value="Day">Day</MenuItem>
                  <MenuItem value="Week">Week</MenuItem>
                  <MenuItem value="Month">Month</MenuItem>
                </Select>
              </FormControl>
            </Grid>
            <Grid item xs={12} md={3}>
              {newSchedule.scheduleType === 'Weekly' && (
                <TextField
                  label="Week Days (Mon,Tue,...)"
                  value={newSchedule.weekDays || 'Mon,Tue,Wed,Thu,Fri'}
                  onChange={(e) => setNewSchedule({ ...newSchedule, weekDays: e.target.value })}
                  fullWidth
                />
              )}
              {newSchedule.scheduleType === 'Monthly' && (
                <TextField
                  label="Month Days (1,15,...)"
                  value={newSchedule.monthDays || '1'}
                  onChange={(e) => setNewSchedule({ ...newSchedule, monthDays: e.target.value })}
                  fullWidth
                />
              )}
            </Grid>
            <Grid item xs={12} md={1}>
              <FormControlLabel
                control={
                  <Switch
                    checked={newSchedule.isActive}
                    onChange={(e) => setNewSchedule({ ...newSchedule, isActive: e.target.checked })}
                  />
                }
                label="Active"
              />
            </Grid>
            <Grid item xs={12} md={1}>
              <Button
                variant="contained"
                color="primary"
                onClick={handleAddSchedule}
                fullWidth
              >
                Add
              </Button>
            </Grid>
          </Grid>

          {newConfig.schedules.length > 0 && (
            <TableContainer component={Paper} sx={{ mt: 2 }}>
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>Type</TableCell>
                    <TableCell>Start Time</TableCell>
                    <TableCell>Frequency</TableCell>
                    <TableCell>Days</TableCell>
                    <TableCell>Active</TableCell>
                    <TableCell>Actions</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {newConfig.schedules.map((schedule, index) => (
                    <TableRow key={index}>
                      <TableCell>{schedule.scheduleType}</TableCell>
                      <TableCell>{schedule.startTime}</TableCell>
                      <TableCell>
                        {schedule.scheduleType === 'Once'
                          ? 'Once'
                          : `Every ${schedule.frequency} ${schedule.frequencyUnit}${schedule.frequency > 1 ? 's' : ''}`}
                      </TableCell>
                      <TableCell>
                        {schedule.scheduleType === 'Weekly'
                          ? schedule.weekDays
                          : schedule.scheduleType === 'Monthly'
                            ? schedule.monthDays
                            : '-'}
                      </TableCell>
                      <TableCell>
                        <Chip
                          label={schedule.isActive ? 'Active' : 'Inactive'}
                          color={schedule.isActive ? 'success' : 'default'}
                          size="small"
                        />
                      </TableCell>
                      <TableCell>
                        <IconButton
                          size="small"
                          color="error"
                          onClick={() => handleRemoveSchedule(index)}
                        >
                          <DeleteIcon />
                        </IconButton>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setIsConfigDialogOpen(false)}>Cancel</Button>
          <Button onClick={handleSaveConfig} variant="contained" color="primary">
            Save
          </Button>
        </DialogActions>
      </Dialog>

      {/* Run Details Dialog */}
      <Dialog open={isRunDetailsDialogOpen} onClose={() => setIsRunDetailsDialogOpen(false)} maxWidth="lg" fullWidth>
        <DialogTitle>
          Run Details #{runDetails?.runId}
        </DialogTitle>
        <DialogContent>
          {runDetails && (
            <Box>
              <Grid container spacing={2} sx={{ mb: 3 }}>
                <Grid item xs={12} md={6}>
                  <Typography variant="subtitle1">Configuration: {runDetails.configurationName}</Typography>
                  <Typography variant="subtitle1">
                    Status: <Chip
                      label={runDetails.status}
                      color={
                        runDetails.status === 'Completed' ? 'success' :
                        runDetails.status === 'Failed' ? 'error' :
                        runDetails.status === 'Running' ? 'info' :
                        runDetails.status === 'CompletedWithErrors' ? 'warning' :
                        'default'
                      }
                      size="small"
                    />
                  </Typography>
                  <Typography variant="subtitle1">
                    Started: {new Date(runDetails.startTime).toLocaleString()}
                  </Typography>
                  <Typography variant="subtitle1">
                    Ended: {runDetails.endTime ? new Date(runDetails.endTime).toLocaleString() : 'Running...'}
                  </Typography>
                </Grid>
                <Grid item xs={12} md={6}>
                  <Typography variant="subtitle1">
                    Duration: {runDetails.elapsedMs ? `${(runDetails.elapsedMs / 1000).toFixed(2)} seconds` : 'Running...'}
                  </Typography>
                  <Typography variant="subtitle1">
                    Tables Processed: {runDetails.totalTablesProcessed}
                    {runDetails.failedTablesCount > 0 ? ` (${runDetails.failedTablesCount} failed)` : ''}
                  </Typography>
                  <Typography variant="subtitle1">
                    Rows Processed: {runDetails.totalRowsProcessed.toLocaleString()}
                  </Typography>
                  <Typography variant="subtitle1">
                    Avg. Speed: {runDetails.averageRowsPerSecond ? `${runDetails.averageRowsPerSecond.toFixed(2)} rows/sec` : 'N/A'}
                  </Typography>
                </Grid>
              </Grid>

              <Divider sx={{ mb: 2 }} />

              <Typography variant="h6" sx={{ mb: 2 }}>Table Metrics</Typography>
              <TableContainer component={Paper}>
                <Table size="small">
                  <TableHead>
                    <TableRow>
                      <TableCell>Schema</TableCell>
                      <TableCell>Table</TableCell>
                      <TableCell>Status</TableCell>
                      <TableCell>Rows</TableCell>
                      <TableCell>Duration</TableCell>
                      <TableCell>Speed</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {runDetails.tableMetrics.map((metric) => (
                      <TableRow key={metric.metricId}>
                        <TableCell>{metric.schemaName}</TableCell>
                        <TableCell>{metric.tableName}</TableCell>
                        <TableCell>
                          <Chip
                            label={metric.status}
                            color={
                              metric.status === 'Completed' ? 'success' :
                              metric.status === 'Failed' ? 'error' :
                              metric.status === 'Running' ? 'info' :
                              'default'
                            }
                            size="small"
                          />
                        </TableCell>
                        <TableCell>{metric.rowsProcessed.toLocaleString()} / {metric.totalRowsToProcess.toLocaleString()}</TableCell>
                        <TableCell>
                          {metric.elapsedMs ? `${(metric.elapsedMs / 1000).toFixed(2)} seconds` : 'Running...'}
                        </TableCell>
                        <TableCell>
                          {metric.rowsPerSecond ? `${metric.rowsPerSecond.toFixed(2)} rows/sec` : 'N/A'}
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>

              {runDetails.logs.length > 0 && (
                <>
                  <Typography variant="h6" sx={{ mt: 3, mb: 2 }}>Logs</Typography>
                  <Paper sx={{ maxHeight: 300, overflow: 'auto' }}>
                    <List dense>
                      {runDetails.logs.map((log) => (
                        <ListItem key={log.logId}>
                          <ListItemIcon>
                            <Chip
                              label={log.logLevel}
                              color={
                                log.logLevel === 'Error' ? 'error' :
                                log.logLevel === 'Warning' ? 'warning' :
                                log.logLevel === 'Information' ? 'info' :
                                'default'
                              }
                              size="small"
                            />
                          </ListItemIcon>
                          <ListItemText
                            primary={log.message}
                            secondary={new Date(log.logTime).toLocaleString()}
                          />
                        </ListItem>
                      ))}
                    </List>
                  </Paper>
                </>
              )}
            </Box>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setIsRunDetailsDialogOpen(false)}>Close</Button>
        </DialogActions>
      </Dialog>

      {/* Snackbar for notifications */}
      <Snackbar
        open={snackbar.open}
        autoHideDuration={6000}
        onClose={handleCloseSnackbar}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
      >
        <Alert onClose={handleCloseSnackbar} severity={snackbar.severity} sx={{ width: '100%' }}>
          {snackbar.message}
        </Alert>
      </Snackbar>
    </Container>
  );
}