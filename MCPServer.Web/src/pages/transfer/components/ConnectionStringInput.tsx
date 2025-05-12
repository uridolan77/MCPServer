import React, { useState } from 'react';
import {
  TextField,
  InputAdornment,
  IconButton,
  Tooltip,
  FormHelperText,
  FormControl
} from '@mui/material';
import { Visibility, VisibilityOff } from '@mui/icons-material';
import { ConnectionFormData } from '../types/ConnectionTypes';

interface ConnectionStringInputProps {
  formData: ConnectionFormData;
  handleChange: (e: React.ChangeEvent<HTMLInputElement | { name?: string; value: unknown }>) => void;
  error?: string;
  readOnly?: boolean;
}

const ConnectionStringInput: React.FC<ConnectionStringInputProps> = ({
  formData,
  handleChange,
  error,
  readOnly = false
}) => {
  const [showConnectionString, setShowConnectionString] = useState(false);

  const toggleShowConnectionString = () => {
    setShowConnectionString(!showConnectionString);
  };

  return (
    <FormControl fullWidth error={!!error}>
      <TextField
        fullWidth
        required
        label="Connection String"
        name="connectionString"
        value={formData.connectionString}
        onChange={handleChange}
        multiline
        rows={3}
        disabled={readOnly}
        type={showConnectionString ? 'text' : 'password'}
        error={!!error}
        InputProps={{
          endAdornment: (
            <InputAdornment position="end">
              <Tooltip title={showConnectionString ? "Hide connection string" : "Show connection string"}>
                <IconButton
                  aria-label="toggle connection string visibility"
                  onClick={toggleShowConnectionString}
                  edge="end"
                >
                  {showConnectionString ? <VisibilityOff /> : <Visibility />}
                </IconButton>
              </Tooltip>
            </InputAdornment>
          ),
        }}
      />
      {error && <FormHelperText error>{error}</FormHelperText>}
      <FormHelperText>
        Example: Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword;
      </FormHelperText>
    </FormControl>
  );
};

export default ConnectionStringInput;