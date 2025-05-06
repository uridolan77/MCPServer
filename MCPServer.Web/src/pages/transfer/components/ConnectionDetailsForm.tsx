import React from 'react';
import {
  Grid,
  TextField,
  FormControlLabel,
  Switch
} from '@mui/material';

interface ConnectionDetailsFormProps {
  connectionDetails: {
    server: string;
    database: string;
    username: string;
    password: string;
    port?: string;
    additionalParams?: string;
  };
  formSettings: {
    timeout: number;
    encrypt: boolean;
    trustServerCertificate: boolean;
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
  return (
    <Grid container spacing={2}>
      <Grid item xs={12} md={6}>
        <TextField
          label="Server"
          name="server"
          value={connectionDetails.server}
          onChange={onDetailsChange}
          fullWidth
          required
        />
      </Grid>
      <Grid item xs={12} md={6}>
        <TextField
          label="Database"
          name="database"
          value={connectionDetails.database}
          onChange={onDetailsChange}
          fullWidth
          required
        />
      </Grid>
      <Grid item xs={12} md={6}>
        <div style={{ width: '100%', marginBottom: '8px' }}>
          <label style={{ display: 'block', marginBottom: '4px' }}>Username *</label>
          <div
            contentEditable
            suppressContentEditableWarning
            onInput={(e) => {
              const value = e.currentTarget.textContent || '';
              onDetailsChange({
                target: { name: 'username', value }
              } as React.ChangeEvent<HTMLInputElement>);
            }}
            style={{
              width: '100%',
              padding: '10px',
              border: '1px solid #ccc',
              borderRadius: '4px',
              fontFamily: 'monospace',
              minHeight: '20px'
            }}
          >
            {connectionDetails.username}
          </div>
        </div>
      </Grid>
      <Grid item xs={12} md={6}>
        <div style={{ width: '100%', marginBottom: '8px' }}>
          <label style={{ display: 'block', marginBottom: '4px' }}>Password *</label>
          <div
            contentEditable
            suppressContentEditableWarning
            onInput={(e) => {
              const value = e.currentTarget.textContent || '';
              onDetailsChange({
                target: { name: 'password', value }
              } as React.ChangeEvent<HTMLInputElement>);
            }}
            style={{
              width: '100%',
              padding: '10px',
              border: '1px solid #ccc',
              borderRadius: '4px',
              fontFamily: 'monospace',
              minHeight: '20px'
            }}
          >
            {connectionDetails.password}
          </div>
        </div>
      </Grid>
      <Grid item xs={12} md={6}>
        <TextField
          label="Port (Optional)"
          name="port"
          value={connectionDetails.port}
          onChange={onDetailsChange}
          fullWidth
        />
      </Grid>
      <Grid item xs={12} md={6}>
        <TextField
          label="Connection Timeout (seconds)"
          name="timeout"
          type="number"
          value={formSettings.timeout}
          onChange={onSettingChange}
          fullWidth
        />
      </Grid>
      <Grid item xs={12} md={6}>
        <FormControlLabel
          control={
            <Switch
              name="encrypt"
              checked={formSettings.encrypt}
              onChange={onSettingChange}
            />
          }
          label="Encrypt Connection"
        />
      </Grid>
      <Grid item xs={12} md={6}>
        <FormControlLabel
          control={
            <Switch
              name="trustServerCertificate"
              checked={formSettings.trustServerCertificate}
              onChange={onSettingChange}
            />
          }
          label="Trust Server Certificate"
        />
      </Grid>
      <Grid item xs={12}>
        <TextField
          label="Additional Parameters (Optional)"
          name="additionalParams"
          value={connectionDetails.additionalParams}
          onChange={onDetailsChange}
          fullWidth
          multiline
          rows={2}
          helperText="Additional connection string parameters (e.g. MultipleActiveResultSets=true;)"
        />
      </Grid>
    </Grid>
  );
};

export default ConnectionDetailsForm;