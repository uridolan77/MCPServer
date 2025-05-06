import React, { useState, useCallback } from 'react';
import { Box, Typography, Dialog, DialogContent, DialogTitle, IconButton } from '@mui/material';
import { Close as CloseIcon } from '@mui/icons-material';
import Wizard from '@/components/Wizard';
import {
  BasicConnectionSettings,
  AdvancedConnectionSettings,
  JSONStructureEditor
} from '@/components/ConnectionWizard';
import DatabaseSchemaSelector from '@/components/DatabaseSchemaSelector';
import DataTransferService from '@/services/dataTransfer.service';
import { useSnackbar } from '@/hooks/useSnackbar';

interface ConnectDataSourceWizardProps {
  open: boolean;
  onClose: () => void;
  onComplete?: (data: any) => void;
  initialData?: any;
}

const ConnectDataSourceWizard: React.FC<ConnectDataSourceWizardProps> = ({
  open,
  onClose,
  onComplete,
  initialData = {}
}) => {
  const { showSnackbar } = useSnackbar();
  const [testConnectionResult, setTestConnectionResult] = useState<{
    success: boolean;
    message: string;
  } | null>(null);

  // Test connection
  const testConnection = useCallback(async (connectionData: any) => {
    if (!connectionData.connection) return false;

    try {
      const conn = connectionData.connection;

      // Convert connection access level to numeric value required by API
      let accessLevelValue = 0; // Default to ReadOnly

      if (conn.connectionAccessLevel) {
        switch (conn.connectionAccessLevel) {
          case 'ReadOnly':
            accessLevelValue = 0;
            break;
          case 'WriteOnly':
            accessLevelValue = 1;
            break;
          case 'ReadWrite':
            accessLevelValue = 2;
            break;
        }
      }

      // Format connection data for the API
      const connectionRequest = {
        connectionId: conn.connectionId || 0,
        connectionName: conn.connectionName,
        connectionString: conn.connectionString,
        description: conn.description || '',
        connectionAccessLevel: accessLevelValue,
        isActive: conn.isActive !== undefined ? conn.isActive : true,
        maxPoolSize: conn.maxPoolSize || 100,
        minPoolSize: conn.minPoolSize || 5,
        timeout: conn.timeout || 30,
        encrypt: conn.encrypt !== undefined ? conn.encrypt : true,
        trustServerCertificate: conn.trustServerCertificate !== undefined ? conn.trustServerCertificate : true,
        createdBy: "System",
        lastModifiedBy: "System",
        createdOn: new Date().toISOString(),
        lastModifiedOn: new Date().toISOString()
      };

      const response = await DataTransferService.testConnection(connectionRequest);

      setTestConnectionResult(response);

      if (response.success) {
        showSnackbar(`Connection successful to ${response.database} on ${response.server}`, 'success');
        return true;
      } else {
        showSnackbar(`Connection failed: ${response.message}`, 'error');
        return false;
      }
    } catch (error: any) {
      console.error('Error testing connection:', error);
      showSnackbar('Error testing connection', 'error');

      setTestConnectionResult({
        success: false,
        message: error.message || 'Unknown error'
      });

      return false;
    } finally {
      // No loading state to reset
    }
  }, [showSnackbar]);

  // Wizard steps - defined as components rendered for each step
  const steps = [
    {
      label: 'Basic Settings',
      content: <BasicConnectionSettings />,
      validation: () => true // Basic validation happens in the component itself
    },
    {
      label: 'Advanced Settings',
      content: <AdvancedConnectionSettings />,
      optional: true // This step is optional
    },
    {
      label: 'Schema Selection',
      content: <DatabaseSchemaSelector />,
      validation: async (formData: any) => {
        // Before showing schema, validate the connection
        if (!testConnectionResult?.success) {
          const success = await testConnection(formData);
          return success;
        }
        return true;
      }
    },
    {
      label: 'JSON Editor',
      content: <JSONStructureEditor />,
      validation: (formData: any) => {
        // Validate JSON
        return formData?.isValidJson !== false;
      }
    }
  ];

  // Handle wizard completion
  const handleComplete = (wizardData: any) => {
    // Process the data as needed
    if (onComplete) {
      onComplete(wizardData);
    }

    // Show completion message
    showSnackbar('Database connection and schema mapping completed successfully', 'success');

    // Close the wizard
    onClose();
  };

  return (
    <Dialog
      open={open}
      onClose={onClose}
      fullWidth
      maxWidth="lg"
      keepMounted
      disablePortal={false}
      disableEnforceFocus
      PaperProps={{
        sx: {
          height: '90vh',
          maxHeight: '900px',
          display: 'flex',
          flexDirection: 'column'
        }
      }}
    >
      <DialogTitle sx={{ m: 0, p: 2 }}>
        <Typography variant="h6">Connect Data Source</Typography>
        <IconButton
          aria-label="close"
          onClick={onClose}
          sx={{
            position: 'absolute',
            right: 8,
            top: 8,
            color: (theme) => theme.palette.grey[500],
          }}
        >
          <CloseIcon />
        </IconButton>
      </DialogTitle>

      <DialogContent dividers sx={{ p: 0, display: 'flex', flexDirection: 'column', flexGrow: 1 }}>
        <Box sx={{ p: 3, flexGrow: 1, display: 'flex', flexDirection: 'column' }}>
          <Wizard
            steps={steps}
            onComplete={handleComplete}
            onCancel={onClose}
            initialData={initialData}
            title="Connect and Map Database Schema"
            showStepNumbers
          />
        </Box>
      </DialogContent>
    </Dialog>
  );
};

export default ConnectDataSourceWizard;