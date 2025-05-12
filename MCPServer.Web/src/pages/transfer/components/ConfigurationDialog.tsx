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
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  IconButton,
  Typography,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  Chip,
  Box,
  Tabs,
  Tab,
  SelectChangeEvent
} from '@mui/material';
import { Add as AddIcon, Delete as DeleteIcon } from '@mui/icons-material';

// Define TypeScript interfaces for our data structures
interface Connection {
  connectionId: number;
  connectionName: string;
  connectionString: string;
  connectionAccessLevel: string;
  description: string;
  server: string;
  port: number | null;
  database: string;
  username: string;
  password: string;
  additionalParameters: string;
  isActive: boolean;
  isConnectionValid: boolean | null;
  minPoolSize: number | null;
  maxPoolSize: number | null;
  timeout: number | null;
  trustServerCertificate: boolean | null;
  encrypt: boolean | null;
  createdBy: string;
  createdOn: string;
  lastModifiedBy: string;
  lastModifiedOn: string | null;
  lastTestedOn: string | null;
  
  // Computed properties that exist in the C# model but not in DB
  isSource?: boolean;
  isDestination?: boolean;
}

interface TableMapping {
  mappingId: number;
  schemaName: string;
  tableName: string;
  timestampColumnName: string;
  orderByColumn: string;
  customWhereClause: string;
  isActive: boolean;
  priority: number;
}

interface Schedule {
  scheduleId: number;
  scheduleType: string;
  startTime: string;
  frequency: number;
  frequencyUnit: string;
  weekDays: string;
  monthDays: string;
  isActive: boolean;
}

interface ConfigurationData {
  configurationId: number;
  configurationName: string;
  description: string;
  sourceConnection: { connectionId: string | number };
  destinationConnection: { connectionId: string | number };
  batchSize: number;
  reportingFrequency: number;
  isActive: boolean;
  tableMappings: TableMapping[];
  schedules: Schedule[];
}

interface ConfigurationDialogProps {
  open: boolean;
  configuration: ConfigurationData | null;
  connections: Connection[];
  onClose: () => void;
  onSave: (configuration: ConfigurationData) => void;
}

interface TabPanelProps {
  children?: React.ReactNode;
  value: number;
  index: number;
}

function TabPanel(props: TabPanelProps) {
  const { children, value, index, ...other } = props;

  return (
    <div
      role="tabpanel"
      hidden={value !== index}
      id={`config-tabpanel-${index}`}
      aria-labelledby={`config-tab-${index}`}
      {...other}
    >
      {value === index && (
        <Box sx={{ py: 2 }}>
          {children}
        </Box>
      )}
    </div>
  );
}

export default function ConfigurationDialog({ open, configuration, connections, onClose, onSave }: ConfigurationDialogProps) {
  const [tabValue, setTabValue] = useState<number>(0);
  const [formData, setFormData] = useState<ConfigurationData>({
    configurationId: 0,
    configurationName: '',
    description: '',
    sourceConnection: { connectionId: '' },
    destinationConnection: { connectionId: '' },
    batchSize: 5000,
    reportingFrequency: 10,
    isActive: true,
    tableMappings: [],
    schedules: []
  });

  const [newTableMapping, setNewTableMapping] = useState<TableMapping>({
    mappingId: 0,
    schemaName: '',
    tableName: '',
    timestampColumnName: '',
    orderByColumn: '',
    customWhereClause: '',
    isActive: true,
    priority: 100,
  });

  const [newSchedule, setNewSchedule] = useState<Schedule>({
    scheduleId: 0,
    scheduleType: 'Daily',
    startTime: '00:00',
    frequency: 1,
    frequencyUnit: 'Day',
    weekDays: 'Mon,Tue,Wed,Thu,Fri',
    monthDays: '1',
    isActive: true,
  });

  useEffect(() => {
    if (configuration) {
      setFormData({
        configurationId: configuration.configurationId || 0,
        configurationName: configuration.configurationName || '',
        description: configuration.description || '',
        sourceConnection: configuration.sourceConnection || { connectionId: '' },
        destinationConnection: configuration.destinationConnection || { connectionId: '' },
        batchSize: configuration.batchSize || 5000,
        reportingFrequency: configuration.reportingFrequency || 10,
        isActive: configuration.isActive !== undefined ? configuration.isActive : true,
        tableMappings: configuration.tableMappings || [],
        schedules: configuration.schedules || []
      });
    } else {
      setFormData({
        configurationId: 0,
        configurationName: '',
        description: '',
        sourceConnection: { connectionId: '' },
        destinationConnection: { connectionId: '' },
        batchSize: 5000,
        reportingFrequency: 10,
        isActive: true,
        tableMappings: [],
        schedules: []
      });
    }
  }, [configuration, open]);

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | { name?: string; value: unknown; checked?: boolean }> | SelectChangeEvent<string>) => {
    const target = e.target as { name: string; value: unknown; checked?: boolean; type?: string };

    // Handle nested properties
    if (target.name.includes('.')) {
      const [parent, child] = target.name.split('.');
      setFormData(prev => ({
        ...prev,
        [parent]: {
          ...(prev[parent as keyof ConfigurationData] as object),
          [child]: target.value
        }
      }));
    } else {
      setFormData(prev => ({
        ...prev,
        [target.name]: target.type === 'checkbox' ? target.checked : target.value
      }));
    }
  };

  const handleTableMappingChange = (e: React.ChangeEvent<HTMLInputElement | { name?: string; value: unknown; checked?: boolean }> | SelectChangeEvent<string>) => {
    const target = e.target as { name: string; value: unknown; checked?: boolean; type?: string };
    setNewTableMapping(prev => ({
      ...prev,
      [target.name]: target.type === 'checkbox' ? target.checked : target.value
    }));
  };

  const handleScheduleChange = (e: React.ChangeEvent<HTMLInputElement | { name?: string; value: unknown; checked?: boolean }> | SelectChangeEvent<string>) => {
    const target = e.target as { name: string; value: unknown; checked?: boolean; type?: string };
    setNewSchedule(prev => ({
      ...prev,
      [target.name]: target.type === 'checkbox' ? target.checked : target.value
    }));
  };

  const handleAddTableMapping = () => {
    if (!newTableMapping.schemaName || !newTableMapping.tableName || !newTableMapping.timestampColumnName) {
      return; // Don't add if required fields are missing
    }

    setFormData(prev => ({
      ...prev,
      tableMappings: [
        ...prev.tableMappings,
        { ...newTableMapping, mappingId: 0 }
      ]
    }));

    // Reset form
    setNewTableMapping({
      mappingId: 0,
      schemaName: '',
      tableName: '',
      timestampColumnName: '',
      orderByColumn: '',
      customWhereClause: '',
      isActive: true,
      priority: 100,
    });
  };

  const handleRemoveTableMapping = (index: number) => {
    setFormData(prev => ({
      ...prev,
      tableMappings: prev.tableMappings.filter((_, i) => i !== index)
    }));
  };

  const handleAddSchedule = () => {
    setFormData(prev => ({
      ...prev,
      schedules: [
        ...prev.schedules,
        { ...newSchedule, scheduleId: 0 }
      ]
    }));

    // Reset form
    setNewSchedule({
      scheduleId: 0,
      scheduleType: 'Daily',
      startTime: '00:00',
      frequency: 1,
      frequencyUnit: 'Day',
      weekDays: 'Mon,Tue,Wed,Thu,Fri',
      monthDays: '1',
      isActive: true,
    });
  };

  const handleRemoveSchedule = (index: number) => {
    setFormData(prev => ({
      ...prev,
      schedules: prev.schedules.filter((_, i) => i !== index)
    }));
  };

  const handleTabChange = (_event: React.SyntheticEvent, newValue: number) => {
    setTabValue(newValue);
  };

  const handleSubmit = () => {
    // Find the full connection objects from the connections array
    const sourceConnection = connections.find(c => c.connectionId.toString() === formData.sourceConnection.connectionId.toString());
    const destinationConnection = connections.find(c => c.connectionId.toString() === formData.destinationConnection.connectionId.toString());

    if (!sourceConnection || !destinationConnection) {
      alert('Please select valid source and destination connections');
      return;
    }

    // Prepare data for save with full connection objects
    const configToSave = {
      ...formData,
      sourceConnection: sourceConnection,
      destinationConnection: destinationConnection
    };

    onSave(configToSave as ConfigurationData);
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="lg" fullWidth>
      <DialogTitle>
        {formData.configurationId ? 'Edit Configuration' : 'New Configuration'}
      </DialogTitle>
      <DialogContent>
        <Box sx={{ borderBottom: 1, borderColor: 'divider', mb: 2 }}>
          <Tabs value={tabValue} onChange={handleTabChange}>
            <Tab label="General" />
            <Tab label="Table Mappings" />
            <Tab label="Schedule" />
          </Tabs>
        </Box>

        {/* General Tab */}
        <TabPanel value={tabValue} index={0}>
          <Grid container spacing={2}>
            <Grid item xs={12} md={6}>
              <TextField
                label="Configuration Name"
                name="configurationName"
                value={formData.configurationName}
                onChange={handleChange}
                fullWidth
                required
                margin="normal"
              />
            </Grid>
            <Grid item xs={12} md={6}>
              <TextField
                label="Description"
                name="description"
                value={formData.description}
                onChange={handleChange}
                fullWidth
                margin="normal"
              />
            </Grid>
            <Grid item xs={12} md={6}>
              <FormControl fullWidth margin="normal">
                <InputLabel>Source Connection</InputLabel>
                <Select
                  value={formData.sourceConnection.connectionId}
                  onChange={(e: SelectChangeEvent) => {
                    setFormData(prev => ({
                      ...prev,
                      sourceConnection: { connectionId: e.target.value }
                    }));
                  }}
                  label="Source Connection"
                  required
                >
                  {connections && connections.length > 0 ?
                    connections
                      .filter(conn => conn.isSource)
                      .map(conn => (
                        <MenuItem key={conn.connectionId} value={conn.connectionId}>
                          {conn.connectionName}
                        </MenuItem>
                      ))
                    :
                    <MenuItem key="no-source" value="" disabled>No source connections available</MenuItem>
                  }
                </Select>
              </FormControl>
            </Grid>
            <Grid item xs={12} md={6}>
              <FormControl fullWidth margin="normal">
                <InputLabel>Destination Connection</InputLabel>
                <Select
                  value={formData.destinationConnection.connectionId}
                  onChange={(e: SelectChangeEvent) => {
                    setFormData(prev => ({
                      ...prev,
                      destinationConnection: { connectionId: e.target.value }
                    }));
                  }}
                  label="Destination Connection"
                  required
                >
                  {connections && connections.length > 0 ?
                    connections
                      .filter(conn => conn.isDestination)
                      .map(conn => (
                        <MenuItem key={conn.connectionId} value={conn.connectionId}>
                          {conn.connectionName}
                        </MenuItem>
                      ))
                    :
                    <MenuItem key="no-dest" value="" disabled>No destination connections available</MenuItem>
                  }
                </Select>
              </FormControl>
            </Grid>
            <Grid item xs={12} md={4}>
              <TextField
                label="Batch Size"
                name="batchSize"
                type="number"
                value={formData.batchSize}
                onChange={handleChange}
                fullWidth
                margin="normal"
              />
            </Grid>
            <Grid item xs={12} md={4}>
              <TextField
                label="Reporting Frequency"
                name="reportingFrequency"
                type="number"
                value={formData.reportingFrequency}
                onChange={handleChange}
                fullWidth
                margin="normal"
              />
            </Grid>
            <Grid item xs={12} md={4}>
              <FormControlLabel
                control={
                  <Switch
                    name="isActive"
                    checked={formData.isActive}
                    onChange={handleChange}
                  />
                }
                label="Active"
                sx={{ mt: 3 }}
              />
            </Grid>
          </Grid>
        </TabPanel>

        {/* Table Mappings Tab */}
        <TabPanel value={tabValue} index={1}>
          <Typography variant="h6" sx={{ mb: 2 }}>
            Table Mappings
          </Typography>

          <Grid container spacing={2}>
            <Grid item xs={12} md={3}>
              <TextField
                label="Schema Name"
                name="schemaName"
                value={newTableMapping.schemaName}
                onChange={handleTableMappingChange}
                fullWidth
                required
              />
            </Grid>
            <Grid item xs={12} md={3}>
              <TextField
                label="Table Name"
                name="tableName"
                value={newTableMapping.tableName}
                onChange={handleTableMappingChange}
                fullWidth
                required
              />
            </Grid>
            <Grid item xs={12} md={3}>
              <TextField
                label="Timestamp Column"
                name="timestampColumnName"
                value={newTableMapping.timestampColumnName}
                onChange={handleTableMappingChange}
                fullWidth
                required
              />
            </Grid>
            <Grid item xs={12} md={3}>
              <TextField
                label="Order By Column"
                name="orderByColumn"
                value={newTableMapping.orderByColumn}
                onChange={handleTableMappingChange}
                fullWidth
              />
            </Grid>
            <Grid item xs={12} md={9}>
              <TextField
                label="Custom Where Clause"
                name="customWhereClause"
                value={newTableMapping.customWhereClause}
                onChange={handleTableMappingChange}
                fullWidth
              />
            </Grid>
            <Grid item xs={12} md={2}>
              <FormControlLabel
                control={
                  <Switch
                    name="isActive"
                    checked={newTableMapping.isActive}
                    onChange={handleTableMappingChange}
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
                startIcon={<AddIcon />}
              >
                Add
              </Button>
            </Grid>
          </Grid>

          {formData.tableMappings.length > 0 && (
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
                  {formData.tableMappings.map((mapping, index) => (
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
        </TabPanel>

        {/* Schedule Tab */}
        <TabPanel value={tabValue} index={2}>
          <Typography variant="h6" sx={{ mb: 2 }}>
            Schedules
          </Typography>

          <Grid container spacing={2}>
            <Grid item xs={12} md={2}>
              <FormControl fullWidth>
                <InputLabel>Type</InputLabel>
                <Select
                  name="scheduleType"
                  value={newSchedule.scheduleType}
                  onChange={handleScheduleChange}
                  label="Type"
                >
                  <MenuItem key="schedule-once" value="Once">Once</MenuItem>
                  <MenuItem key="schedule-daily" value="Daily">Daily</MenuItem>
                  <MenuItem key="schedule-weekly" value="Weekly">Weekly</MenuItem>
                  <MenuItem key="schedule-monthly" value="Monthly">Monthly</MenuItem>
                  <MenuItem key="schedule-custom" value="Custom">Custom</MenuItem>
                </Select>
              </FormControl>
            </Grid>
            <Grid item xs={12} md={2}>
              <TextField
                label="Start Time"
                name="startTime"
                type="time"
                value={newSchedule.startTime}
                onChange={handleScheduleChange}
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
                name="frequency"
                type="number"
                value={newSchedule.frequency}
                onChange={handleScheduleChange}
                fullWidth
                disabled={newSchedule.scheduleType === 'Once'}
              />
            </Grid>
            <Grid item xs={12} md={2}>
              <FormControl fullWidth disabled={newSchedule.scheduleType === 'Once'}>
                <InputLabel>Unit</InputLabel>
                <Select
                  name="frequencyUnit"
                  value={newSchedule.frequencyUnit}
                  onChange={handleScheduleChange}
                  label="Unit"
                >
                  <MenuItem key="unit-minute" value="Minute">Minute</MenuItem>
                  <MenuItem key="unit-hour" value="Hour">Hour</MenuItem>
                  <MenuItem key="unit-day" value="Day">Day</MenuItem>
                  <MenuItem key="unit-week" value="Week">Week</MenuItem>
                  <MenuItem key="unit-month" value="Month">Month</MenuItem>
                </Select>
              </FormControl>
            </Grid>
            <Grid item xs={12} md={3}>
              {newSchedule.scheduleType === 'Weekly' && (
                <TextField
                  label="Week Days (Mon,Tue,...)"
                  name="weekDays"
                  value={newSchedule.weekDays}
                  onChange={handleScheduleChange}
                  fullWidth
                />
              )}
              {newSchedule.scheduleType === 'Monthly' && (
                <TextField
                  label="Month Days (1,15,...)"
                  name="monthDays"
                  value={newSchedule.monthDays}
                  onChange={handleScheduleChange}
                  fullWidth
                />
              )}
            </Grid>
            <Grid item xs={12} md={1}>
              <FormControlLabel
                control={
                  <Switch
                    name="isActive"
                    checked={newSchedule.isActive}
                    onChange={handleScheduleChange}
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
                startIcon={<AddIcon />}
              >
                Add
              </Button>
            </Grid>
          </Grid>

          {formData.schedules.length > 0 && (
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
                  {formData.schedules.map((schedule, index) => (
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
        </TabPanel>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button onClick={handleSubmit} variant="contained" color="primary">
          Save
        </Button>
      </DialogActions>
    </Dialog>
  );
}