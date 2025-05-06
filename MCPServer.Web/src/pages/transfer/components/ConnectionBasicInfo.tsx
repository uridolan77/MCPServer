import React from 'react';
import {
  TextField,
  Grid,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  SelectChangeEvent,
} from '@mui/material';

interface ConnectionBasicInfoProps {
  connectionName: string;
  connectionType: string;
  onNameChange: (e: React.ChangeEvent<HTMLInputElement | { name?: string; value: unknown }>) => void;
  onTypeChange: (e: SelectChangeEvent) => void;
}

const ConnectionBasicInfo: React.FC<ConnectionBasicInfoProps> = ({
  connectionName,
  connectionType,
  onNameChange,
  onTypeChange,
}) => {
  return (
    <>
      <Grid item xs={12}>
        <TextField
          label="Connection Name"
          name="connectionName"
          value={connectionName}
          onChange={onNameChange}
          fullWidth
          required
        />
      </Grid>

      <Grid item xs={12}>
        <FormControl fullWidth>
          <InputLabel id="connection-type-label">Database Type</InputLabel>
          <Select
            labelId="connection-type-label"
            id="connection-type"
            name="connectionType"
            value={connectionType}
            onChange={onTypeChange}
            label="Database Type"
          >
            <MenuItem value="sqlServer">SQL Server</MenuItem>
            <MenuItem value="mysql">MySQL</MenuItem>
            <MenuItem value="postgresql">PostgreSQL</MenuItem>
            <MenuItem value="oracle">Oracle</MenuItem>
          </Select>
        </FormControl>
      </Grid>
    </>
  );
};

export default ConnectionBasicInfo;