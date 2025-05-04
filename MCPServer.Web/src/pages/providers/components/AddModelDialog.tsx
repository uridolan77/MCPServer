import React, { useState } from 'react';
import { useMutation } from '@tanstack/react-query';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  TextField,
  FormControlLabel,
  Switch,
  Grid,
  CircularProgress,
  Alert,
  InputAdornment
} from '@mui/material';
import { llmProviderApi, LlmModel } from '@/api';
import { useErrorHandler } from '@/hooks';

interface AddModelDialogProps {
  open: boolean;
  onClose: () => void;
  onSuccess: () => void;
  providerId: number;
}

const AddModelDialog: React.FC<AddModelDialogProps> = ({
  open,
  onClose,
  onSuccess,
  providerId
}) => {
  const { handleError } = useErrorHandler();
  const [error, setError] = useState<string | null>(null);
  
  const [model, setModel] = useState<Partial<LlmModel>>({
    providerId,
    name: '',
    modelId: '',
    description: '',
    maxTokens: 4096,
    contextWindow: 8192,
    supportsStreaming: true,
    supportsVision: false,
    supportsTools: false,
    costPer1KInputTokens: 0.0,
    costPer1KOutputTokens: 0.0,
    isEnabled: true
  });

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value, type, checked } = e.target;
    
    if (type === 'checkbox') {
      setModel({ ...model, [name]: checked });
    } else if (type === 'number') {
      setModel({ ...model, [name]: parseFloat(value) });
    } else {
      setModel({ ...model, [name]: value });
    }
  };

  // Add model mutation
  const addModelMutation = useMutation({
    mutationFn: (newModel: Partial<LlmModel>) => llmProviderApi.createModel(newModel as LlmModel),
    onSuccess: () => {
      onSuccess();
    },
    onError: (error) => {
      handleError(error, 'Failed to add model');
      setError('Failed to add model. Please try again.');
    }
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    
    // Validate form
    if (!model.name || !model.modelId) {
      setError('Name and Model ID are required fields');
      return;
    }
    
    addModelMutation.mutate(model as LlmModel);
  };

  return (
    <Dialog
      open={open}
      onClose={onClose}
      aria-labelledby="add-model-dialog-title"
      maxWidth="md"
      fullWidth
    >
      <form onSubmit={handleSubmit}>
        <DialogTitle id="add-model-dialog-title">Add New Model</DialogTitle>
        <DialogContent>
          {error && (
            <Alert severity="error" sx={{ mb: 2 }}>
              {error}
            </Alert>
          )}
          
          <Grid container spacing={2} sx={{ mt: 1 }}>
            <Grid item xs={12} md={6}>
              <TextField
                name="name"
                label="Display Name"
                value={model.name}
                onChange={handleChange}
                fullWidth
                required
                margin="normal"
                helperText="A friendly name for this model"
              />
            </Grid>
            <Grid item xs={12} md={6}>
              <TextField
                name="modelId"
                label="Model ID"
                value={model.modelId}
                onChange={handleChange}
                fullWidth
                required
                margin="normal"
                helperText="The ID used by the provider (e.g., gpt-4)"
              />
            </Grid>
            <Grid item xs={12}>
              <TextField
                name="description"
                label="Description"
                value={model.description}
                onChange={handleChange}
                fullWidth
                multiline
                rows={2}
                margin="normal"
              />
            </Grid>
            <Grid item xs={12} md={6}>
              <TextField
                name="maxTokens"
                label="Max Tokens"
                type="number"
                value={model.maxTokens}
                onChange={handleChange}
                fullWidth
                margin="normal"
                InputProps={{ inputProps: { min: 0 } }}
              />
            </Grid>
            <Grid item xs={12} md={6}>
              <TextField
                name="contextWindow"
                label="Context Window"
                type="number"
                value={model.contextWindow}
                onChange={handleChange}
                fullWidth
                margin="normal"
                InputProps={{ inputProps: { min: 0 } }}
              />
            </Grid>
            <Grid item xs={12} md={6}>
              <TextField
                name="costPer1KInputTokens"
                label="Cost per 1K Input Tokens"
                type="number"
                value={model.costPer1KInputTokens}
                onChange={handleChange}
                fullWidth
                margin="normal"
                InputProps={{
                  startAdornment: <InputAdornment position="start">$</InputAdornment>,
                  inputProps: { min: 0, step: 0.01 }
                }}
              />
            </Grid>
            <Grid item xs={12} md={6}>
              <TextField
                name="costPer1KOutputTokens"
                label="Cost per 1K Output Tokens"
                type="number"
                value={model.costPer1KOutputTokens}
                onChange={handleChange}
                fullWidth
                margin="normal"
                InputProps={{
                  startAdornment: <InputAdornment position="start">$</InputAdornment>,
                  inputProps: { min: 0, step: 0.01 }
                }}
              />
            </Grid>
            <Grid item xs={12} md={4}>
              <FormControlLabel
                control={
                  <Switch
                    name="supportsStreaming"
                    checked={Boolean(model.supportsStreaming)}
                    onChange={handleChange}
                    color="primary"
                  />
                }
                label="Supports Streaming"
              />
            </Grid>
            <Grid item xs={12} md={4}>
              <FormControlLabel
                control={
                  <Switch
                    name="supportsVision"
                    checked={Boolean(model.supportsVision)}
                    onChange={handleChange}
                    color="primary"
                  />
                }
                label="Supports Vision"
              />
            </Grid>
            <Grid item xs={12} md={4}>
              <FormControlLabel
                control={
                  <Switch
                    name="supportsTools"
                    checked={Boolean(model.supportsTools)}
                    onChange={handleChange}
                    color="primary"
                  />
                }
                label="Supports Tools"
              />
            </Grid>
            <Grid item xs={12}>
              <FormControlLabel
                control={
                  <Switch
                    name="isEnabled"
                    checked={Boolean(model.isEnabled)}
                    onChange={handleChange}
                    color="primary"
                  />
                }
                label="Enabled"
              />
            </Grid>
          </Grid>
        </DialogContent>
        <DialogActions>
          <Button onClick={onClose} disabled={addModelMutation.isPending}>
            Cancel
          </Button>
          <Button
            type="submit"
            color="primary"
            variant="contained"
            disabled={addModelMutation.isPending}
            startIcon={addModelMutation.isPending ? <CircularProgress size={20} /> : undefined}
          >
            Add Model
          </Button>
        </DialogActions>
      </form>
    </Dialog>
  );
};

export default AddModelDialog;