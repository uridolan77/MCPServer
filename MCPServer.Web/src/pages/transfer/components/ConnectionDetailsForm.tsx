import React, { useState } from 'react';
import {
  TextField,
  Grid,
  Typography,
  MenuItem,
  FormControl,
  InputLabel,
  Select,
  FormControlLabel,
  Switch,
  InputAdornment,
  IconButton
} from '@mui/material';
import VisibilityIcon from '@mui/icons-material/Visibility';
import VisibilityOffIcon from '@mui/icons-material/VisibilityOff';
import { Connection } from '../types/Connection';

interface ConnectionDetailsFormProps {
  connectionDetails: Connection;
  formSettings: {
    timeout: number | null;
    encrypt: boolean | null;
    trustServerCertificate: boolean | null;
  };
  onDetailsChange: (e: React.ChangeEvent<HTMLInputElement | { name?: string; value: unknown }>) => void;
  onSettingChange: (e: React.ChangeEvent<HTMLInputElement | { name?: string; value: unknown }>) => void;
}

const ConnectionDetailsForm: React.FC<ConnectionDetailsFormProps> = ({
  connectionDetails,
  formSettings,
  onDetailsChange,
  onSettingChange
}) => {
  // State for showing/hiding the password
  const [showPassword, setShowPassword] = useState(false);

  // Toggle visibility of password
  const togglePasswordVisibility = () => {
    setShowPassword(!showPassword);
  };

  return (
    <Grid container spacing={3}>
      <Grid item xs={12}>
        <Typography variant="h6">Connection Details</Typography>
      </Grid>
      
      <Grid item xs={8} md={4}>
        <TextField
          fullWidth
          required
          label="Server"
          name="server"
          value={connectionDetails.server || ''}
          onChange={onDetailsChange}
          variant="outlined"
          placeholder="localhost or server.domain.com"
        />
      </Grid>
      
      <Grid item xs={4} md={2}>
        <TextField
          fullWidth
          label="Port"
          name="port"
          value={connectionDetails.port || ''}
          onChange={onDetailsChange}
          variant="outlined"
          type="number"
          placeholder="1433"
        />
      </Grid>
      
      <Grid item xs={12} md={6}>
        <TextField
          fullWidth
          required
          label="Database"
          name="database"
          value={connectionDetails.database || ''}
          onChange={onDetailsChange}
          variant="outlined"
        />
      </Grid>
      
      <Grid item xs={12} md={6}>
        <TextField
          fullWidth
          required
          label="Username"
          name="username"
          value={connectionDetails.username || ''}
          onChange={onDetailsChange}
          variant="outlined"
        />
      </Grid>
      
      <Grid item xs={12} md={6}>
        <TextField
          fullWidth
          required
          label="Password"
          name="password"
          type={showPassword ? 'text' : 'password'}
          value={connectionDetails.password || ''}
          onChange={onDetailsChange}
          variant="outlined"
          InputProps={{
            endAdornment: (
              <InputAdornment position="end">
                <IconButton
                  onClick={togglePasswordVisibility}
                  edge="end"
                >
                  {showPassword ? <VisibilityOffIcon /> : <VisibilityIcon />}
                </IconButton>
              </InputAdornment>
            ),
          }}
        />
      </Grid>
      
      <Grid item xs={12}>
        <TextField
          fullWidth
          label="Additional Parameters"
          name="additionalParameters"
          value={connectionDetails.additionalParameters || ''}
          onChange={onDetailsChange}
          variant="outlined"
          placeholder="Param1=Value1;Param2=Value2"
        />
      </Grid>
      
      <Grid item xs={12} md={4}>
        <TextField
          fullWidth
          label="Connection Timeout"
          name="timeout"
          type="number"
          value={formSettings.timeout || 30}
          onChange={onSettingChange}
          variant="outlined"
          InputProps={{
            endAdornment: <InputAdornment position="end">seconds</InputAdornment>,
          }}
        />
      </Grid>
      
      <Grid item xs={12} md={4}>
        <FormControlLabel
          control={
            <Switch
              checked={!!formSettings.encrypt}
              onChange={onSettingChange}
              name="encrypt"
              color="primary"
            />
          }
          label="Encrypt Connection"
        />
      </Grid>
      
      <Grid item xs={12} md={4}>
        <FormControlLabel
          control={
            <Switch
              checked={!!formSettings.trustServerCertificate}
              onChange={onSettingChange}
              name="trustServerCertificate"
              color="primary"
            />
          }
          label="Trust Server Certificate"
        />
      </Grid>
    </Grid>
  );
};

export default ConnectionDetailsForm;