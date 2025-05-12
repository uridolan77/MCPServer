import React from 'react';
import {
  Grid,
  TextField,
  FormControl,
  FormLabel,
  RadioGroup,
  FormControlLabel,
  Radio,
  Typography,
  Switch,
  Box,
  Divider
} from '@mui/material';

// Update interface to match standardized Connection properties
interface ConnectionSettingsProps {
  formData: {
    description: string;
    connectionAccessLevel: string;
    isSource: boolean;
    isDestination: boolean;
    isActive: boolean;
    lastTestedOn: Date | null;
    maxPoolSize: number | null;
    minPoolSize: number | null;
  };
  connectionTested: boolean;
  onFormChange: (e: React.ChangeEvent<HTMLInputElement | { name?: string; value: unknown }>) => void;
  onConnectionPurposeChange: (e: React.ChangeEvent<HTMLInputElement>) => void;
}

const ConnectionSettings: React.FC<ConnectionSettingsProps> = ({
  formData,
  connectionTested,
  onFormChange,
  onConnectionPurposeChange
}) => {
  return (
    <>
      <Grid item xs={12}>
        <Divider sx={{ my: 2 }} />
        <Typography variant="subtitle1" sx={{ mb: 2 }}>
          Connection Settings
        </Typography>
        {!connectionTested && (
          <Typography variant="body2" color="error" sx={{ mb: 2 }}>
            Please test the connection successfully before editing these settings
          </Typography>
        )}
      </Grid>

      <Grid item xs={12}>
        <TextField
          label="Description"
          name="description"
          value={formData.description}
          onChange={onFormChange}
          fullWidth
          disabled={!connectionTested}
        />
      </Grid>

      <Grid item xs={12}>
        <FormControl component="fieldset" disabled={!connectionTested}>
          <FormLabel component="legend">Connection Access Level</FormLabel>
          <RadioGroup
            row
            name="connectionPurpose"
            value={
              formData.connectionAccessLevel === 'ReadWrite'
                ? 'both'
                : formData.connectionAccessLevel === 'ReadOnly'
                ? 'source'
                : 'destination'
            }
            onChange={onConnectionPurposeChange}
          >
            <FormControlLabel value="source" control={<Radio />} label="Read Only (Source)" />
            <FormControlLabel value="destination" control={<Radio />} label="Write Only (Destination)" />
            <FormControlLabel value="both" control={<Radio />} label="Read/Write (Both)" />
          </RadioGroup>
        </FormControl>
      </Grid>

      {formData.lastTestedOn && (
        <Grid item xs={12}>
          <Typography variant="body2" color="textSecondary">
            Last tested successfully on: {formData.lastTestedOn.toLocaleString()}
          </Typography>
        </Grid>
      )}

      <Grid item xs={12}>
        <FormControlLabel
          control={
            <Switch
              name="isActive"
              checked={formData.isActive}
              onChange={onFormChange}
              disabled={!connectionTested}
            />
          }
          label="Active"
        />
      </Grid>

      <Grid item xs={12} md={6}>
        <Typography variant="body2" gutterBottom>
          Max Pool Size: {formData.maxPoolSize}
        </Typography>
        <Box sx={{ px: 1 }}>
          <input
            type="range"
            min="10"
            max="1000"
            step="10"
            name="maxPoolSize"
            value={formData.maxPoolSize || 0}
            onChange={onFormChange}
            disabled={!connectionTested}
            style={{ width: '100%' }}
          />
        </Box>
      </Grid>

      <Grid item xs={12} md={6}>
        <Typography variant="body2" gutterBottom>
          Min Pool Size: {formData.minPoolSize}
        </Typography>
        <Box sx={{ px: 1 }}>
          <input
            type="range"
            min="1"
            max="100"
            step="1"
            name="minPoolSize"
            value={formData.minPoolSize || 0}
            onChange={onFormChange}
            disabled={!connectionTested}
            style={{ width: '100%' }}
          />
        </Box>
      </Grid>
    </>
  );
};

export default ConnectionSettings;