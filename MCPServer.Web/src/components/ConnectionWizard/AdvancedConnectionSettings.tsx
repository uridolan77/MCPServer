import React, { useState, useEffect } from 'react';
import {
  Box,
  Typography,
  Grid,
  TextField,
  Slider,
  FormControl,
  FormControlLabel,
  Switch,
  InputLabel,
  Select,
  MenuItem,
  Divider,
  Tooltip,
  IconButton,
  Paper
} from '@mui/material';
import { Info as InfoIcon } from '@mui/icons-material';
import { DatabaseConnection } from '@/types/connections';

interface AdvancedConnectionSettingsProps {
  formData?: any;
  updateFormData?: (data: any) => void;
}

const AdvancedConnectionSettings: React.FC<AdvancedConnectionSettingsProps> = ({ 
  formData = {}, 
  updateFormData = () => {} 
}) => {
  const [connection, setConnection] = useState<Partial<DatabaseConnection>>({
    description: '',
    maxPoolSize: 100,
    minPoolSize: 5,
    timeout: 30,
    encrypt: true,
    trustServerCertificate: true,
    isActive: true,
    connectionAccessLevel: 'ReadOnly',
    ...formData.connection
  });

  // Update local state when formData changes
  useEffect(() => {
    if (formData.connection) {
      setConnection(prev => ({
        ...prev,
        ...formData.connection
      }));
    }
  }, [formData.connection]);

  // Handle form field changes
  const handleChange = (field: keyof DatabaseConnection, value: any) => {
    const updatedConnection = {
      ...connection,
      [field]: value
    };
    
    setConnection(updatedConnection);
    
    // Update parent form data
    updateFormData({
      connection: updatedConnection
    });
  };

  return (
    <Box>
      <Typography variant="h6" gutterBottom>
        Advanced Connection Settings
      </Typography>
      <Typography variant="body2" color="text.secondary" paragraph>
        Configure additional options for your database connection.
      </Typography>

      <Paper variant="outlined" sx={{ p: 2, mb: 3 }}>
        <Grid container spacing={3}>
          <Grid item xs={12}>
            <TextField
              fullWidth
              label="Description"
              name="description"
              value={connection.description || ''}
              onChange={(e) => handleChange('description', e.target.value)}
              multiline
              rows={2}
              helperText="Optional: Enter a description for this connection"
            />
          </Grid>

          <Grid item xs={12}>
            <FormControl fullWidth>
              <InputLabel id="access-level-label">Connection Access Level</InputLabel>
              <Select
                labelId="access-level-label"
                id="access-level"
                value={connection.connectionAccessLevel || 'ReadOnly'}
                label="Connection Access Level"
                onChange={(e) => handleChange('connectionAccessLevel', e.target.value)}
              >
                <MenuItem value="ReadOnly">Read Only</MenuItem>
                <MenuItem value="WriteOnly">Write Only</MenuItem>
                <MenuItem value="ReadWrite">Read & Write</MenuItem>
              </Select>
            </FormControl>
          </Grid>
        </Grid>
      </Paper>

      {connection.connectionType === 'SQL Server' && (
        <Paper variant="outlined" sx={{ p: 2, mb: 3 }}>
          <Typography variant="subtitle1" gutterBottom>
            SQL Server Options
            <Tooltip title="These settings control how your application connects to SQL Server">
              <IconButton size="small" sx={{ ml: 1 }}>
                <InfoIcon fontSize="small" />
              </IconButton>
            </Tooltip>
          </Typography>
          <Divider sx={{ mb: 2 }} />
          
          <Grid container spacing={3}>
            <Grid item xs={12} md={6}>
              <Typography gutterBottom>Max Pool Size: {connection.maxPoolSize}</Typography>
              <Slider
                value={connection.maxPoolSize || 100}
                onChange={(_, value) => handleChange('maxPoolSize', value)}
                valueLabelDisplay="auto"
                step={10}
                min={10}
                max={500}
              />
              <Typography variant="caption" color="text.secondary">
                Maximum number of connections allowed in the pool
              </Typography>
            </Grid>

            <Grid item xs={12} md={6}>
              <Typography gutterBottom>Min Pool Size: {connection.minPoolSize}</Typography>
              <Slider
                value={connection.minPoolSize || 5}
                onChange={(_, value) => handleChange('minPoolSize', value)}
                valueLabelDisplay="auto"
                step={1}
                min={0}
                max={50}
              />
              <Typography variant="caption" color="text.secondary">
                Minimum number of connections maintained in the pool
              </Typography>
            </Grid>

            <Grid item xs={12} md={6}>
              <Typography gutterBottom>Connection Timeout: {connection.timeout} seconds</Typography>
              <Slider
                value={connection.timeout || 30}
                onChange={(_, value) => handleChange('timeout', value)}
                valueLabelDisplay="auto"
                step={5}
                min={5}
                max={120}
              />
              <Typography variant="caption" color="text.secondary">
                Time in seconds to wait for a connection to the server
              </Typography>
            </Grid>

            <Grid item xs={12}>
              <FormControlLabel
                control={
                  <Switch
                    checked={connection.encrypt ?? true}
                    onChange={(e) => handleChange('encrypt', e.target.checked)}
                    color="primary"
                  />
                }
                label="Encrypt Connection"
              />
              <Typography variant="caption" display="block" color="text.secondary">
                Encrypts network traffic between the application and SQL Server
              </Typography>
            </Grid>

            <Grid item xs={12}>
              <FormControlLabel
                control={
                  <Switch
                    checked={connection.trustServerCertificate ?? true}
                    onChange={(e) => handleChange('trustServerCertificate', e.target.checked)}
                    color="primary"
                  />
                }
                label="Trust Server Certificate"
              />
              <Typography variant="caption" display="block" color="text.secondary">
                When true, SQL Server's SSL certificate will not be validated
              </Typography>
            </Grid>
          </Grid>
        </Paper>
      )}

      <Paper variant="outlined" sx={{ p: 2 }}>
        <Typography variant="subtitle1" gutterBottom>
          Connection Status
        </Typography>
        <Divider sx={{ mb: 2 }} />
        
        <Grid container spacing={3}>
          <Grid item xs={12}>
            <FormControlLabel
              control={
                <Switch
                  checked={connection.isActive ?? true}
                  onChange={(e) => handleChange('isActive', e.target.checked)}
                  color="primary"
                />
              }
              label="Active Connection"
            />
            <Typography variant="caption" display="block" color="text.secondary">
              Inactive connections won't be available for data operations
            </Typography>
          </Grid>
        </Grid>
      </Paper>
    </Box>
  );
};

export default AdvancedConnectionSettings;