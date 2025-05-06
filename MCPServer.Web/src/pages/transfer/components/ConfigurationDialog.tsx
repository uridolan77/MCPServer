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
  Tab
} from '@mui/material';
import { Add as AddIcon, Delete as DeleteIcon } from '@mui/icons-material';

function TabPanel(props) {
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

export default function ConfigurationDialog({ open, configuration, connections, onClose, onSave }) {
  const [tabValue, setTabValue] = useState(0);
  const [formData, setFormData] = useState({
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

  const handleChange = (e) => {
    const { name, value, checked } = e.target;

    // Handle nested properties
    if (name.includes('.')) {
      const [parent, child] = name.split('.');
      setFormData(prev => ({
        ...prev,
        [parent]: {
          ...prev[parent],
          [child]: value
        }
      }));
    } else {
      setFormData(prev => ({
        ...prev,
        [name]: e.target.type === 'checkbox' ? checked : value
      }));
    }
  };

  const handleTableMappingChange = (e) => {
    const { name, value, checked } = e.target;
    setNewTableMapping(prev => ({
      ...prev,
      [name]: e.target.type === 'checkbox' ? checked : value
    }));
  };

  const handleScheduleChange = (e) => {
    const { name, value, checked } = e.target;
    setNewSchedule(prev => ({
      ...prev,
      [name]: e.target.type === 'checkbox' ? checked : value
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
    setFormData(prev => ({
      ...prev,
      schedules: prev.schedules.filter((_, i) => i !== index)
    }));
  };

  const handleTabChange = (event, newValue) => {
    setTabValue(newValue);
  };

  const handleSubmit = () => {
    // Prepare data for save
    const configToSave = {
      ...formData,
      // Ensure the connection IDs are passed correctly for backend
      sourceConnectionId: formData.sourceConnection.connectionId,
      destinationConnectionId: formData.destinationConnection.connectionId
    };

    onSave(configToSave);
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
                  onChange={(e) => {
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
                    <MenuItem value="" disabled>No source connections available</MenuItem>
                  }
                </Select>
              </FormControl>
            </Grid>
            <Grid item xs={12} md={6}>
              <FormControl fullWidth margin="normal">
                <InputLabel>Destination Connection</InputLabel>
                <Select
                  value={formData.destinationConnection.connectionId}
                  onChange={(e) => {
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
                    <MenuItem value="" disabled>No destination connections available</MenuItem>
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