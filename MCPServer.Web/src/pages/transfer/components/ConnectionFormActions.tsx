import React from 'react';
import { DialogActions, Button, CircularProgress } from '@mui/material';

interface ConnectionFormActionsProps {
  onClose: () => void;
  onSave: () => void;
  onTest: () => void;
  onViewSchema: () => void;
  isTesting: boolean;
  connectionTested: boolean;
  testResult: any;
  disabled?: boolean;
  isNew: boolean;
}

const ConnectionFormActions = ({
  onClose,
  onSave,
  onTest,
  onViewSchema,
  isTesting,
  connectionTested,
  testResult,
  disabled = false,
  isNew
}: ConnectionFormActionsProps) => {
  return (
    <DialogActions sx={{ p: 2, pt: 0 }}>
      <Button 
        onClick={onTest} 
        variant="outlined" 
        color="primary"
        disabled={isTesting || disabled}
        startIcon={isTesting ? <CircularProgress size={20} /> : null}
      >
        {isTesting ? 'Testing...' : 'Test Connection'}
      </Button>
      
      <Button 
        onClick={onViewSchema} 
        variant="outlined" 
        color="primary"
        disabled={!connectionTested || !testResult?.success || disabled}
      >
        View Schema
      </Button>
      
      <Button onClick={onClose} color="secondary">
        Cancel
      </Button>
      
      <Button 
        onClick={onSave} 
        color="primary" 
        variant="contained"
        disabled={disabled}
      >
        {isNew ? 'Create' : 'Save'}
      </Button>
    </DialogActions>
  );
};

export default ConnectionFormActions;