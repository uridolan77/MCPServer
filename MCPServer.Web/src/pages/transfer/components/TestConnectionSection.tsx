import React from 'react';
import {
  Box,
  Button,
  CircularProgress,
  Alert,
  Grid
} from '@mui/material';

interface TestConnectionSectionProps {
  isTesting: boolean;
  testResult: null | {
    success: boolean;
    message: string;
    detailedError?: string;
    server?: string;
    database?: string;
    errorCode?: number;
    errorType?: string;
    innerException?: string;
  };
  onTestConnection: () => void;
}

const TestConnectionSection: React.FC<TestConnectionSectionProps> = ({
  isTesting,
  testResult,
  onTestConnection
}) => {
  return (
    <Grid item xs={12}>
      <Box sx={{ display: 'flex', justifyContent: 'flex-start', mt: 1, mb: 2 }}>
        <Button
          variant="outlined"
          color="primary"
          onClick={onTestConnection}
          disabled={isTesting}
          sx={{ mr: 2 }}
        >
          {isTesting ? <CircularProgress size={24} /> : 'Test Connection'}
        </Button>

        {testResult && (
          <Alert
            severity={testResult.success ? 'success' : 'error'}
            sx={{ flexGrow: 1 }}
          >
            {testResult.success ? (
              `Connection successful to ${testResult.database} on ${testResult.server}`
            ) : (
              <div>
                <div><strong>Connection failed:</strong> {testResult.message}</div>
                {testResult.detailedError && testResult.detailedError !== testResult.message && (
                  <div style={{ marginTop: '8px', fontSize: '0.9em' }}>
                    <strong>Details:</strong> {testResult.detailedError}
                  </div>
                )}
                {testResult.errorCode && (
                  <div style={{ marginTop: '4px', fontSize: '0.9em' }}>
                    <strong>Error code:</strong> {testResult.errorCode}
                  </div>
                )}
                {testResult.innerException && (
                  <div style={{ marginTop: '4px', fontSize: '0.9em' }}>
                    <strong>Inner exception:</strong> {testResult.innerException}
                  </div>
                )}
              </div>
            )}
          </Alert>
        )}
      </Box>
    </Grid>
  );
};

export default TestConnectionSection;