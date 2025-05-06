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
} from '@mui/material';

export default function ConnectionDialog({ open, connection, onClose, onSave }) {
  const [formData, setFormData] = useState({
    connectionId: 0,
    connectionName: '',
    connectionString: '',
    description: '',
    isSource: true,
    isDestination: true,
    isActive: true,
  });

  useEffect(() => {
    if (connection) {
      setFormData({
        connectionId: connection.connectionId || 0,
        connectionName: connection.connectionName || '',
        connectionString: connection.connectionString || '',
        description: connection.description || '',
        isSource: connection.isSource !== undefined ? connection.isSource : true,
        isDestination: connection.isDestination !== undefined ? connection.isDestination : true,
        isActive: connection.isActive !== undefined ? connection.isActive : true,
      });
    } else {
      setFormData({
        connectionId: 0,
        connectionName: '',
        connectionString: '',
        description: '',
        isSource: true,
        isDestination: true,
        isActive: true,
      });
    }
  }, [connection, open]);

  const handleChange = (e) => {
    const { name, value, checked } = e.target;
    setFormData(prev => ({
      ...prev,
      [name]: e.target.type === 'checkbox' ? checked : value
    }));
  };

  const handleSubmit = () => {
    onSave(formData);
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
      <DialogTitle>
        {formData.connectionId ? 'Edit Connection' : 'New Connection'}
      </DialogTitle>
      <DialogContent>
        <Grid container spacing={2} sx={{ mt: 1 }}>
          <Grid item xs={12}>
            <TextField
              label="Connection Name"
              name="connectionName"
              value={formData.connectionName}
              onChange={handleChange}
              fullWidth
              required
            />
          </Grid>
          <Grid item xs={12}>
            <TextField
              label="Connection String"
              name="connectionString"
              value={formData.connectionString}
              onChange={handleChange}
              fullWidth
              required
              multiline
              rows={3}
            />
          </Grid>
          <Grid item xs={12}>
            <TextField
              label="Description"
              name="description"
              value={formData.description}
              onChange={handleChange}
              fullWidth
            />
          </Grid>
          <Grid item xs={4}>
            <FormControlLabel
              control={
                <Switch
                  name="isSource"
                  checked={formData.isSource}
                  onChange={handleChange}
                />
              }
              label="Use as Source"
            />
          </Grid>
          <Grid item xs={4}>
            <FormControlLabel
              control={
                <Switch
                  name="isDestination"
                  checked={formData.isDestination}
                  onChange={handleChange}
                />
              }
              label="Use as Destination"
            />
          </Grid>
          <Grid item xs={4}>
            <FormControlLabel
              control={
                <Switch
                  name="isActive"
                  checked={formData.isActive}
                  onChange={handleChange}
                />
              }
              label="Active"
            />
          </Grid>
        </Grid>
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