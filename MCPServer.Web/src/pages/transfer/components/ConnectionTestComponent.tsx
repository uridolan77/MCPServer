import React from 'react';
import {
  Button,
  CircularProgress,
  Box,
  Alert,
  AlertTitle,
  Collapse
} from '@mui/material';
import { ConnectionTestResult } from '../types/ConnectionTypes';

interface ConnectionTestComponentProps {
  testConnection: () => Promise<void>;
  testing: boolean;
  testResult: ConnectionTestResult | null;
  connectionString: string;
  readOnly?: boolean;
}

const ConnectionTestComponent: React.FC<ConnectionTestComponentProps> = ({
  testConnection,
  testing,
  testResult,
  connectionString,
  readOnly = false
}) => {
  const hasConnectionString = !!connectionString.trim();

  return (
    <Box sx={{ mt: 2 }}>
      <Button
        variant="outlined"
        color="primary"
        onClick={testConnection}
        disabled={testing || !hasConnectionString || readOnly}
        startIcon={testing && <CircularProgress size={20} />}
        fullWidth
      >
        {testing ? 'Testing Connection...' : 'Test Connection'}
      </Button>

      <Collapse in={!!testResult} sx={{ mt: 2 }}>
        {testResult && (
          <Alert 
            severity={testResult.success ? "success" : "error"}
            sx={{ mt: 1 }}
          >
            <AlertTitle>{testResult.success ? "Connection Successful" : "Connection Failed"}</AlertTitle>
            {testResult.message}
          </Alert>
        )}
      </Collapse>
    </Box>
  );
};

export default ConnectionTestComponent;