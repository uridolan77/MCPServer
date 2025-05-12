import React from 'react';
import { TextField, Grid, Typography, FormControl, InputLabel, Select, MenuItem } from '@mui/material';

interface ConnectionBasicInfoProps {
  connectionName: string;
  connectionType: string;
  onNameChange: (e: React.ChangeEvent<HTMLInputElement | { name?: string; value: unknown }>) => void;
  onTypeChange: (e: React.ChangeEvent<HTMLInputElement | { name?: string; value: unknown }>) => void;
}

const ConnectionBasicInfo: React.FC<ConnectionBasicInfoProps> = ({
  connectionName,
  connectionType,
  onNameChange,
  onTypeChange
}) => {
  return (
    <Grid container spacing={3}>
      <Grid item xs={12}>
        <Typography variant="h6">Basic Information</Typography>
      </Grid>
      
      <Grid item xs={12} md={6}>
        <TextField
          fullWidth
          required
          label="Connection Name"
          name="connectionName"
          value={connectionName}
          onChange={onNameChange}
          variant="outlined"
          placeholder="My Database Connection"
        />
      </Grid>
      
      <Grid item xs={12} md={6}>
        <FormControl fullWidth>
          <InputLabel id="connection-type-label">Database Type</InputLabel>
          <Select
            labelId="connection-type-label"
            id="connectionType"
            name="connectionType"
            value={connectionType}
            label="Database Type"
            onChange={onTypeChange}
          >
            <MenuItem value="sqlServer">SQL Server</MenuItem>
            <MenuItem value="mysql">MySQL</MenuItem>
            <MenuItem value="postgresql">PostgreSQL</MenuItem>
          </Select>
        </FormControl>
      </Grid>
    </Grid>
  );
};

export default ConnectionBasicInfo;