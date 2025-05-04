import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Box,
  Button,
  Chip,
  IconButton,
  Tooltip,
  Typography,
  Alert
} from '@mui/material';
import {
  Add as AddIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
  Visibility as VisibilityIcon
} from '@mui/icons-material';
import { PageHeader, DataTable, ConfirmDialog } from '@/components';
import { llmProviderApi, LlmProvider } from '@/api';
import { useConfirmDialog, useErrorHandler } from '@/hooks';
import { useAuth } from '@/contexts/AuthContext';
import { useNotification } from '@/contexts/NotificationContext';

const ProvidersListPage: React.FC = () => {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { user } = useAuth();
  const { addNotification } = useNotification();
  const { handleError } = useErrorHandler();
  const confirmDialog = useConfirmDialog();
  
  const isAdmin = user?.roles.includes('Admin');
  const [debugInfo, setDebugInfo] = useState<any>(null);
  
  // Fetch providers
  const {
    data: providersData,
    isLoading,
    isError,
    refetch,
    error
  } = useQuery({
    queryKey: ['providers'],
    queryFn: llmProviderApi.getAllProviders
  });
  
  // Debug logging - capture the raw response for inspection
  useEffect(() => {
    console.log('Raw providers data received:', providersData);
    setDebugInfo(providersData);
    if (!providersData) {
      console.warn('No providers data received');
    } else if (!Array.isArray(providersData)) {
      console.warn('Providers data is not an array:', typeof providersData);
      if (typeof providersData === 'object') {
        console.log('Provider data keys:', Object.keys(providersData));
      }
    } else {
      console.log(`Received ${providersData.length} providers`);
    }
  }, [providersData]);

  // Special response handling for different API formats
  const providers = React.useMemo(() => {
    if (!providersData) return [];
    
    // If it's already an array, use it
    if (Array.isArray(providersData)) {
      return providersData;
    }
    
    // If it's an object, try to find providers
    if (typeof providersData === 'object') {
      // Check if it has a data property that's an array
      if (providersData.data && Array.isArray(providersData.data)) {
        return providersData.data;
      }
      
      // Check if it has a $values property that's an array
      if (providersData.$values && Array.isArray(providersData.$values)) {
        return providersData.$values;
      }
      
      // Try to find a property that's an array
      for (const key in providersData) {
        if (Array.isArray(providersData[key])) {
          return providersData[key];
        }
      }
    }
    
    // If we get here, return an empty array
    return [];
  }, [providersData]);
  
  // Delete provider mutation
  const deleteProviderMutation = useMutation({
    mutationFn: llmProviderApi.deleteProvider,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['providers'] });
      addNotification('Provider deleted successfully', 'success');
      confirmDialog.hideDialog();
    },
    onError: (error) => {
      handleError(error, 'Failed to delete provider');
      confirmDialog.hideDialog();
    }
  });
  
  // Handle delete provider
  const handleDeleteProvider = (id: number) => {
    confirmDialog.showDialog({
      title: 'Delete Provider',
      message: 'Are you sure you want to delete this provider? This action cannot be undone.',
      confirmLabel: 'Delete',
      confirmColor: 'error',
      onConfirm: () => {
        confirmDialog.setLoading(true);
        deleteProviderMutation.mutate(id);
      }
    });
  };
  
  // Table columns
  const columns = [
    {
      id: 'name' as keyof LlmProvider,
      label: 'Name',
      minWidth: 150,
      sortable: true,
      searchable: true
    },
    {
      id: 'displayName' as keyof LlmProvider,
      label: 'Display Name',
      minWidth: 150,
      sortable: true,
      searchable: true
    },
    {
      id: 'apiEndpoint' as keyof LlmProvider,
      label: 'API Endpoint',
      minWidth: 200,
      sortable: true,
      searchable: true,
      format: (value: string) => (
        <Typography variant="body2" noWrap sx={{ maxWidth: 300 }}>
          {value}
        </Typography>
      )
    },
    {
      id: 'isEnabled' as keyof LlmProvider,
      label: 'Status',
      minWidth: 100,
      align: 'center',
      sortable: true,
      format: (value: boolean) => (
        <Chip
          label={value ? 'Enabled' : 'Disabled'}
          color={value ? 'success' : 'error'}
          size="small"
        />
      )
    },
    {
      id: 'actions',
      label: 'Actions',
      minWidth: 150,
      align: 'center',
      format: (row: LlmProvider) => (
        <Box sx={{ display: 'flex', justifyContent: 'center' }}>
          <Tooltip title="View Details">
            <IconButton
              size="small"
              color="info"
              onClick={() => navigate(`/providers/${row.id}`)}
            >
              <VisibilityIcon />
            </IconButton>
          </Tooltip>
          
          {isAdmin && (
            <>
              <Tooltip title="Edit">
                <IconButton
                  size="small"
                  color="primary"
                  onClick={() => navigate(`/providers/${row.id}`)}
                >
                  <EditIcon />
                </IconButton>
              </Tooltip>
              
              <Tooltip title="Delete">
                <IconButton
                  size="small"
                  color="error"
                  onClick={() => handleDeleteProvider(row.id)}
                >
                  <DeleteIcon />
                </IconButton>
              </Tooltip>
            </>
          )}
        </Box>
      )
    }
  ];
  
  return (
    <Box>
      <PageHeader
        title="LLM Providers"
        subtitle="Manage your language model providers"
        breadcrumbs={[
          { label: 'Dashboard', path: '/dashboard' },
          { label: 'LLM Providers' }
        ]}
        action={
          isAdmin && (
            <Button
              variant="contained"
              color="primary"
              startIcon={<AddIcon />}
              onClick={() => navigate('/providers/new')}
            >
              Add Provider
            </Button>
          )
        }
      />
      
      {isError && (
        <Alert severity="error" sx={{ mb: 3 }}>
          Error loading providers: {error instanceof Error ? error.message : 'Unknown error'}
        </Alert>
      )}
      
      {debugInfo && typeof debugInfo === 'object' && !Array.isArray(debugInfo) && (
        <Alert severity="info" sx={{ mb: 3 }}>
          <Typography variant="body2">
            API Response detected (likely needs formatting): The response appears to be an object with {Object.keys(debugInfo).length} keys: {Object.keys(debugInfo).join(', ')}
          </Typography>
        </Alert>
      )}
      
      <DataTable
        columns={columns}
        data={providers}
        title="Providers"
        isLoading={isLoading}
        onRefresh={refetch}
        getRowId={(row) => row.id}
        defaultSortColumn="name"
      />
      
      <ConfirmDialog
        open={confirmDialog.isOpen}
        title={confirmDialog.title}
        message={confirmDialog.message}
        confirmLabel={confirmDialog.confirmLabel}
        cancelLabel={confirmDialog.cancelLabel}
        onConfirm={confirmDialog.onConfirm}
        onCancel={confirmDialog.onCancel}
        isLoading={confirmDialog.isLoading}
        confirmColor={confirmDialog.confirmColor}
      />
    </Box>
  );
};

export default ProvidersListPage;
