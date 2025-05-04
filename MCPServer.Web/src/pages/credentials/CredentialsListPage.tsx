import React, { useState, useEffect } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Box,
  Button,
  Chip,
  IconButton,
  Tooltip,
  Typography,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Alert
} from '@mui/material';
import {
  Add as AddIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
  Visibility as VisibilityIcon,
  Star as StarIcon,
  StarBorder as StarBorderIcon
} from '@mui/icons-material';
import { PageHeader, DataTable, ConfirmDialog } from '@/components';
import { llmProviderApi, LlmProviderCredential } from '@/api';
import { useConfirmDialog, useErrorHandler } from '@/hooks';
import { useAuth } from '@/contexts/AuthContext';
import { useNotification } from '@/contexts/NotificationContext';
import { dateUtils } from '@/utils';

const CredentialsListPage: React.FC = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const queryClient = useQueryClient();
  const { user } = useAuth();
  const { addNotification } = useNotification();
  const { handleError } = useErrorHandler();
  const confirmDialog = useConfirmDialog();
  const [debugInfo, setDebugInfo] = useState<any>(null);
  const [credentialsDebugInfo, setCredentialsDebugInfo] = useState<any>(null);
  
  const providerId = searchParams.get('providerId') 
    ? parseInt(searchParams.get('providerId') as string) 
    : undefined;
  
  const [selectedProviderId, setSelectedProviderId] = useState<number | undefined>(providerId);
  
  // Fetch providers for filter
  const { 
    data: providersData,
    isError: isProvidersError,
    error: providersError 
  } = useQuery({
    queryKey: ['providers'],
    queryFn: llmProviderApi.getAllProviders
  });
  
  // Debug logging - capture the raw response for providers
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
  
  // Ensure providers is always an array with enhanced extraction
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
  
  // Fetch credentials
  const {
    data: credentialsData,
    isLoading,
    isError: isCredentialsError,
    error: credentialsError,
    refetch
  } = useQuery({
    queryKey: ['credentials', selectedProviderId],
    queryFn: () => selectedProviderId 
      ? llmProviderApi.getCredentialsByProviderId(selectedProviderId)
      : llmProviderApi.getUserCredentials()
  });
  
  // Debug logging for credentials data
  useEffect(() => {
    console.log('Raw credentials data received:', credentialsData);
    setCredentialsDebugInfo(credentialsData);
    if (!credentialsData) {
      console.warn('No credentials data received');
    } else if (!Array.isArray(credentialsData)) {
      console.warn('Credentials data is not an array:', typeof credentialsData);
      if (typeof credentialsData === 'object') {
        console.log('Credentials data keys:', Object.keys(credentialsData));
      }
    } else {
      console.log(`Received ${credentialsData.length} credentials`);
    }
  }, [credentialsData]);
  
  // Ensure credentials is always an array with enhanced extraction
  const credentials = React.useMemo(() => {
    if (!credentialsData) return [];
    
    // If it's already an array, use it
    if (Array.isArray(credentialsData)) {
      return credentialsData;
    }
    
    // If it's an object, try to find credentials
    if (typeof credentialsData === 'object') {
      // Check if it has a data property that's an array
      if (credentialsData.data && Array.isArray(credentialsData.data)) {
        return credentialsData.data;
      }
      
      // Check if it has a $values property that's an array
      if (credentialsData.$values && Array.isArray(credentialsData.$values)) {
        return credentialsData.$values;
      }
      
      // Try to find a property that's an array
      for (const key in credentialsData) {
        if (Array.isArray(credentialsData[key])) {
          return credentialsData[key];
        }
      }
    }
    
    // If we get here, return an empty array
    return [];
  }, [credentialsData]);
  
  // Delete credential mutation
  const deleteCredentialMutation = useMutation({
    mutationFn: llmProviderApi.deleteCredential,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['credentials'] });
      addNotification('Credential deleted successfully', 'success');
      confirmDialog.hideDialog();
    },
    onError: (error) => {
      handleError(error, 'Failed to delete credential');
      confirmDialog.hideDialog();
    }
  });
  
  // Set default credential mutation
  const setDefaultCredentialMutation = useMutation({
    mutationFn: (credential: Partial<LlmProviderCredential>) => 
      llmProviderApi.updateCredential(credential.id!, { ...credential, isDefault: true }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['credentials'] });
      addNotification('Default credential updated successfully', 'success');
    },
    onError: (error) => {
      handleError(error, 'Failed to update default credential');
    }
  });
  
  // Handle delete credential
  const handleDeleteCredential = (id: number) => {
    confirmDialog.showDialog({
      title: 'Delete Credential',
      message: 'Are you sure you want to delete this credential? This action cannot be undone.',
      confirmLabel: 'Delete',
      confirmColor: 'error',
      onConfirm: () => {
        confirmDialog.setLoading(true);
        deleteCredentialMutation.mutate(id);
      }
    });
  };
  
  // Handle set as default
  const handleSetDefault = (credential: LlmProviderCredential) => {
    if (credential.isDefault) return;
    
    setDefaultCredentialMutation.mutate({
      id: credential.id,
      providerId: credential.providerId,
      isDefault: true
    });
  };
  
  // Handle provider filter change
  const handleProviderChange = (event: React.ChangeEvent<{ value: unknown }>) => {
    const value = event.target.value as number | '';
    setSelectedProviderId(value === '' ? undefined : value);
  };
  
  // Table columns
  const columns = [
    {
      id: 'name' as keyof LlmProviderCredential,
      label: 'Name',
      minWidth: 150,
      sortable: true,
      searchable: true
    },
    {
      id: 'providerId' as keyof LlmProviderCredential,
      label: 'Provider',
      minWidth: 150,
      sortable: true,
      searchable: true,
      format: (value: number) => {
        const provider = providers.find(p => p.id === value);
        return provider ? provider.displayName || provider.name : value;
      }
    },
    {
      id: 'isDefault' as keyof LlmProviderCredential,
      label: 'Default',
      minWidth: 100,
      align: 'center',
      sortable: true,
      format: (value: boolean, row: LlmProviderCredential) => (
        <IconButton
          color={value ? 'warning' : 'default'}
          onClick={() => handleSetDefault(row)}
          disabled={value}
          size="small"
        >
          {value ? <StarIcon /> : <StarBorderIcon />}
        </IconButton>
      )
    },
    {
      id: 'isEnabled' as keyof LlmProviderCredential,
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
      id: 'lastUsedAt' as keyof LlmProviderCredential,
      label: 'Last Used',
      minWidth: 150,
      sortable: true,
      format: (value: string) => value ? dateUtils.formatDateTime(value) : 'Never'
    },
    {
      id: 'actions',
      label: 'Actions',
      minWidth: 150,
      align: 'center',
      format: (row: LlmProviderCredential) => (
        <Box sx={{ display: 'flex', justifyContent: 'center' }}>
          <Tooltip title="View Details">
            <IconButton
              size="small"
              color="info"
              onClick={() => navigate(`/credentials/${row.id}`)}
            >
              <VisibilityIcon />
            </IconButton>
          </Tooltip>
          
          <Tooltip title="Edit">
            <IconButton
              size="small"
              color="primary"
              onClick={() => navigate(`/credentials/${row.id}`)}
            >
              <EditIcon />
            </IconButton>
          </Tooltip>
          
          <Tooltip title="Delete">
            <IconButton
              size="small"
              color="error"
              onClick={() => handleDeleteCredential(row.id)}
            >
              <DeleteIcon />
            </IconButton>
          </Tooltip>
        </Box>
      )
    }
  ];
  
  return (
    <Box>
      <PageHeader
        title="API Keys"
        subtitle="Manage your LLM provider credentials"
        breadcrumbs={[
          { label: 'Dashboard', path: '/dashboard' },
          { label: 'API Keys' }
        ]}
        action={
          <Button
            variant="contained"
            color="primary"
            startIcon={<AddIcon />}
            onClick={() => navigate('/credentials/new')}
          >
            Add API Key
          </Button>
        }
      />
      
      {isProvidersError && (
        <Alert severity="error" sx={{ mb: 3 }}>
          Error loading providers: {providersError instanceof Error ? providersError.message : 'Unknown error'}
        </Alert>
      )}
      
      {isCredentialsError && (
        <Alert severity="error" sx={{ mb: 3 }}>
          Error loading credentials: {credentialsError instanceof Error ? credentialsError.message : 'Unknown error'}
        </Alert>
      )}
      
      {debugInfo && typeof debugInfo === 'object' && !Array.isArray(debugInfo) && (
        <Alert severity="info" sx={{ mb: 3 }}>
          <Typography variant="body2">
            API Providers Response detected (likely needs formatting): 
            The response appears to be an object with {Object.keys(debugInfo).length} keys: {Object.keys(debugInfo).join(', ')}
          </Typography>
        </Alert>
      )}
      
      {credentialsDebugInfo && typeof credentialsDebugInfo === 'object' && !Array.isArray(credentialsDebugInfo) && (
        <Alert severity="info" sx={{ mb: 3 }}>
          <Typography variant="body2">
            API Credentials Response detected (likely needs formatting): 
            The response appears to be an object with {Object.keys(credentialsDebugInfo).length} keys: {Object.keys(credentialsDebugInfo).join(', ')}
            {credentialsDebugInfo.data && Array.isArray(credentialsDebugInfo.data) && (
              <span> (Found {credentialsDebugInfo.data.length} credentials in the 'data' property)</span>
            )}
          </Typography>
        </Alert>
      )}
      
      <Box sx={{ mb: 3, display: 'flex', justifyContent: 'flex-end' }}>
        <FormControl sx={{ minWidth: 200 }}>
          <InputLabel id="provider-filter-label">Filter by Provider</InputLabel>
          <Select
            labelId="provider-filter-label"
            value={selectedProviderId || ''}
            onChange={handleProviderChange}
            label="Filter by Provider"
          >
            <MenuItem value="">
              <em>All Providers</em>
            </MenuItem>
            {providers.map((provider) => (
              <MenuItem key={provider.id} value={provider.id}>
                {provider.displayName || provider.name}
              </MenuItem>
            ))}
          </Select>
        </FormControl>
      </Box>
      
      <DataTable
        columns={columns}
        data={credentials}
        title="API Keys"
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

export default CredentialsListPage;
