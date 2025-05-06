import React, { useState, useEffect } from 'react';
import {
  Box,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  FormHelperText,
  Grid,
  Typography,
  Divider,
  Alert
} from '@mui/material';
import { DatabaseConnection } from '@/types/connections';

interface BasicConnectionSettingsProps {
  formData?: any;
  updateFormData?: (data: any) => void;
}

const BasicConnectionSettings: React.FC<BasicConnectionSettingsProps> = ({
  formData = {},
  updateFormData = () => {}
}) => {
  const [connection, setConnection] = useState<Partial<DatabaseConnection>>({
    connectionName: '',
    server: '',
    database: '',
    username: '',
    password: '',
    port: '1433',
    connectionType: 'SQL Server',
    ...formData.connection
  });

  const [errors, setErrors] = useState<Record<string, string>>({});

  // Update local state when formData changes
  useEffect(() => {
    if (formData.connection) {
      setConnection(prev => ({
        ...prev,
        ...formData.connection
      }));
    }
  }, [formData.connection]);

  // Validate form fields
  const validate = (): boolean => {
    const newErrors: Record<string, string> = {};
    
    if (!connection.connectionName?.trim()) {
      newErrors.connectionName = 'Connection name is required';
    }
    
    if (!connection.server?.trim()) {
      newErrors.server = 'Server name is required';
    }
    
    if (!connection.database?.trim()) {
      newErrors.database = 'Database name is required';
    }
    
    if (connection.authType === 'SQL' || !connection.authType) {
      if (!connection.username?.trim()) {
        newErrors.username = 'Username is required for SQL authentication';
      }
      
      if (!connection.password?.trim()) {
        newErrors.password = 'Password is required for SQL authentication';
      }
    }
    
    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  // Handle form field changes
  const handleChange = (field: keyof DatabaseConnection, value: any) => {
    // For auth type, we need to clear or set fields accordingly
    if (field === 'authType') {
      const updatedConnection = {
        ...connection,
        [field]: value
      };
      
      // If switching to Windows auth, clear username/password
      if (value === 'Windows') {
        updatedConnection.username = '';
        updatedConnection.password = '';
      }
      
      setConnection(updatedConnection);
      
      // Update parent form data
      updateFormData({
        connection: updatedConnection,
        isBasicSettingsValid: validate()
      });
      return;
    }
    
    // Normal field update
    const updatedConnection = {
      ...connection,
      [field]: value
    };
    
    setConnection(updatedConnection);
    
    // Clear error when field is updated
    if (errors[field]) {
      setErrors((prev) => ({
        ...prev,
        [field]: ''
      }));
    }
    
    // Update parent form data
    updateFormData({
      connection: updatedConnection,
      isBasicSettingsValid: validate()
    });
  };
  
  // Generate connection string
  useEffect(() => {
    // Only generate if we have the minimum required fields
    if (!connection.server || !connection.database) return;
    
    let connectionString = '';
    
    // Different connection string format based on auth type
    if (connection.connectionType === 'SQL Server') {
      if (connection.authType === 'Windows') {
        connectionString = `Server=${connection.server}${connection.port ? ',' + connection.port : ''};Database=${connection.database};Integrated Security=SSPI;`;
      } else {
        // SQL auth is default
        connectionString = `Server=${connection.server}${connection.port ? ',' + connection.port : ''};Database=${connection.database};User Id=${connection.username};Password=${connection.password};`;
      }
    } else if (connection.connectionType === 'MySQL') {
      connectionString = `Server=${connection.server};Port=${connection.port || '3306'};Database=${connection.database};Uid=${connection.username};Pwd=${connection.password};`;
    } else if (connection.connectionType === 'PostgreSQL') {
      connectionString = `Host=${connection.server};Port=${connection.port || '5432'};Database=${connection.database};Username=${connection.username};Password=${connection.password};`;
    }
    
    // Additional common options
    if (connection.connectionType === 'SQL Server') {
      connectionString += 'TrustServerCertificate=True;Encrypt=True;';
    }
    
    // Update connection string in form data
    updateFormData({
      connection: {
        ...connection,
        connectionString
      }
    });
  }, [connection.server, connection.database, connection.username, connection.password, connection.port, connection.authType, connection.connectionType, updateFormData]);

  return (
    <Box>
      <Typography variant="h6" gutterBottom>
        Basic Connection Settings
      </Typography>
      <Typography variant="body2" color="text.secondary" paragraph>
        Enter the basic information needed to connect to your database.
      </Typography>

      <Grid container spacing={3}>
        <Grid item xs={12}>
          <TextField
            fullWidth
            label="Connection Name"
            name="connectionName"
            value={connection.connectionName || ''}
            onChange={(e) => handleChange('connectionName', e.target.value)}
            error={!!errors.connectionName}
            helperText={errors.connectionName || 'Enter a descriptive name for this connection'}
            required
          />
        </Grid>

        <Grid item xs={12} sm={6}>
          <FormControl fullWidth required error={!!errors.connectionType}>
            <InputLabel id="connection-type-label">Database Type</InputLabel>
            <Select
              labelId="connection-type-label"
              id="connection-type"
              value={connection.connectionType || 'SQL Server'}
              label="Database Type"
              onChange={(e) => handleChange('connectionType', e.target.value)}
            >
              <MenuItem value="SQL Server">SQL Server</MenuItem>
              <MenuItem value="MySQL">MySQL</MenuItem>
              <MenuItem value="PostgreSQL">PostgreSQL</MenuItem>
            </Select>
            <FormHelperText>Select your database type</FormHelperText>
          </FormControl>
        </Grid>
        
        <Grid item xs={12}>
          <Divider sx={{ my: 1 }} />
          <Typography variant="subtitle1" gutterBottom>
            Server Information
          </Typography>
        </Grid>

        <Grid item xs={12} sm={8}>
          <TextField
            fullWidth
            label="Server Name/IP"
            name="server"
            value={connection.server || ''}
            onChange={(e) => handleChange('server', e.target.value)}
            error={!!errors.server}
            helperText={errors.server || 'Enter the server name or IP address'}
            required
            placeholder={connection.connectionType === 'SQL Server' ? 'localhost\\SQLEXPRESS' : 'localhost'}
          />
        </Grid>

        <Grid item xs={12} sm={4}>
          <TextField
            fullWidth
            label="Port"
            name="port"
            value={connection.port || (connection.connectionType === 'SQL Server' ? '1433' : connection.connectionType === 'MySQL' ? '3306' : '5432')}
            onChange={(e) => handleChange('port', e.target.value)}
            error={!!errors.port}
            helperText={errors.port || 'Leave blank for default port'}
            placeholder={connection.connectionType === 'SQL Server' ? '1433' : connection.connectionType === 'MySQL' ? '3306' : '5432'}
          />
        </Grid>

        <Grid item xs={12}>
          <TextField
            fullWidth
            label="Database Name"
            name="database"
            value={connection.database || ''}
            onChange={(e) => handleChange('database', e.target.value)}
            error={!!errors.database}
            helperText={errors.database || 'Enter the database name'}
            required
          />
        </Grid>

        <Grid item xs={12}>
          <Divider sx={{ my: 1 }} />
          <Typography variant="subtitle1" gutterBottom>
            Authentication
          </Typography>
        </Grid>

        {connection.connectionType === 'SQL Server' && (
          <Grid item xs={12}>
            <FormControl fullWidth>
              <InputLabel id="auth-type-label">Authentication Type</InputLabel>
              <Select
                labelId="auth-type-label"
                id="auth-type"
                value={connection.authType || 'SQL'}
                label="Authentication Type"
                onChange={(e) => handleChange('authType', e.target.value)}
              >
                <MenuItem value="SQL">SQL Server Authentication</MenuItem>
                <MenuItem value="Windows">Windows Authentication</MenuItem>
              </Select>
              <FormHelperText>Choose how to authenticate with the database</FormHelperText>
            </FormControl>
          </Grid>
        )}

        {(connection.authType !== 'Windows' || connection.connectionType !== 'SQL Server') && (
          <>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                label="Username"
                name="username"
                value={connection.username || ''}
                onChange={(e) => handleChange('username', e.target.value)}
                error={!!errors.username}
                helperText={errors.username || 'Enter the database username'}
                required={connection.authType !== 'Windows'}
              />
            </Grid>

            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                label="Password"
                name="password"
                type="password"
                value={connection.password || ''}
                onChange={(e) => handleChange('password', e.target.value)}
                error={!!errors.password}
                helperText={errors.password || 'Enter the database password'}
                required={connection.authType !== 'Windows'}
              />
            </Grid>
          </>
        )}

        <Grid item xs={12}>
          <Alert severity="info" sx={{ mt: 2 }}>
            Connection string will be automatically generated based on your inputs.
          </Alert>
        </Grid>
      </Grid>
    </Box>
  );
};

export default BasicConnectionSettings;