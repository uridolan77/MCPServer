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
  Alert
} from '@mui/material';
import {
  Add as AddIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
  Visibility as VisibilityIcon
} from '@mui/icons-material';
import { PageHeader, DataTable, ConfirmDialog } from '@/components';
import { ProviderSelector } from '@/components/selectors';
import { llmProviderApi, LlmModel } from '@/api';
import { useConfirmDialog, useErrorHandler } from '@/hooks';
import { useAuth } from '@/contexts/AuthContext';
import { useNotification } from '@/contexts/NotificationContext';
import { formatUtils } from '@/utils';

const ModelsListPage: React.FC = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const queryClient = useQueryClient();
  const { user } = useAuth();
  const { addNotification } = useNotification();
  const { handleError } = useErrorHandler();
  const confirmDialog = useConfirmDialog();
  const [modelsDebugInfo, setModelsDebugInfo] = useState<any>(null);

  const providerId = searchParams.get('providerId') 
    ? parseInt(searchParams.get('providerId') as string) 
    : undefined;

  const [selectedProviderId, setSelectedProviderId] = useState<number | string>(providerId || '');

  const isAdmin = user?.roles.includes('Admin');

  // Fetch models
  const {
    data: modelsData,
    isLoading,
    refetch,
    isError: isModelsError,
    error: modelsError
  } = useQuery({
    queryKey: ['models', selectedProviderId],
    queryFn: () => selectedProviderId 
      ? llmProviderApi.getModelsByProviderId(selectedProviderId as number)
      : llmProviderApi.getAllModels()
  });

  // Debug logging for models data
  useEffect(() => {
    console.log('Raw models data received:', modelsData);
    setModelsDebugInfo(modelsData);
    if (!modelsData) {
      console.warn('No models data received');
    } else if (!Array.isArray(modelsData)) {
      console.warn('Models data is not an array:', typeof modelsData);
      if (typeof modelsData === 'object') {
        console.log('Model data keys:', Object.keys(modelsData));
      }
    } else {
      console.log(`Received ${modelsData.length} models`);
    }
  }, [modelsData]);

  // Ensure models is always an array with enhanced extraction
  const models = React.useMemo(() => {
    if (!modelsData) return [];

    // If it's already an array, use it
    if (Array.isArray(modelsData)) {
      return modelsData;
    }

    // If it's an object, try to find models
    if (typeof modelsData === 'object') {
      // Check if it has a data property that's an array
      if (modelsData.data && Array.isArray(modelsData.data)) {
        return modelsData.data;
      }

      // Check if it has a $values property that's an array
      if (modelsData.$values && Array.isArray(modelsData.$values)) {
        return modelsData.$values;
      }

      // Try to find a property that's an array
      for (const key in modelsData) {
        if (Array.isArray(modelsData[key])) {
          return modelsData[key];
        }
      }
    }

    // If we get here, return an empty array
    return [];
  }, [modelsData]);

  // Delete model mutation
  const deleteModelMutation = useMutation({
    mutationFn: llmProviderApi.deleteModel,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['models'] });
      addNotification('Model deleted successfully', 'success');
      confirmDialog.hideDialog();
    },
    onError: (error) => {
      handleError(error, 'Failed to delete model');
      confirmDialog.hideDialog();
    }
  });

  // Handle delete model
  const handleDeleteModel = (id: number) => {
    confirmDialog.showDialog({
      title: 'Delete Model',
      message: 'Are you sure you want to delete this model? This action cannot be undone.',
      confirmLabel: 'Delete',
      confirmColor: 'error',
      onConfirm: () => {
        confirmDialog.setLoading(true);
        deleteModelMutation.mutate(id);
      }
    });
  };

  // Handle provider filter change
  const handleProviderChange = (value: string | number) => {
    setSelectedProviderId(value);
  };

  // Table columns
  const columns = [
    {
      id: 'name' as keyof LlmModel,
      label: 'Name',
      minWidth: 150,
      sortable: true,
      searchable: true
    },
    {
      id: 'modelId' as keyof LlmModel,
      label: 'Model ID',
      minWidth: 150,
      sortable: true,
      searchable: true
    },
    {
      id: 'providerId' as keyof LlmModel,
      label: 'Provider',
      minWidth: 150,
      sortable: true,
      searchable: true,
      format: (value: number) => {
        const provider = models.find(m => m.providerId === value)?.provider;
        return provider ? provider.displayName || provider.name : value;
      }
    },
    {
      id: 'maxTokens' as keyof LlmModel,
      label: 'Max Tokens',
      minWidth: 120,
      align: 'right',
      sortable: true,
      format: (value: number) => formatUtils.formatNumber(value)
    },
    {
      id: 'contextWindow' as keyof LlmModel,
      label: 'Context Window',
      minWidth: 150,
      align: 'right',
      sortable: true,
      format: (value: number) => formatUtils.formatNumber(value)
    },
    {
      id: 'costPer1KInputTokens' as keyof LlmModel,
      label: 'Input Cost',
      minWidth: 120,
      align: 'right',
      sortable: true,
      format: (value: number) => `$${value.toFixed(4)}`
    },
    {
      id: 'costPer1KOutputTokens' as keyof LlmModel,
      label: 'Output Cost',
      minWidth: 120,
      align: 'right',
      sortable: true,
      format: (value: number) => `$${value.toFixed(4)}`
    },
    {
      id: 'isEnabled' as keyof LlmModel,
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
      format: (row: LlmModel) => (
        <Box sx={{ display: 'flex', justifyContent: 'center' }}>
          <Tooltip title="View Details">
            <IconButton
              size="small"
              color="info"
              onClick={() => navigate(`/models/${row.id}`)}
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
                  onClick={() => navigate(`/models/${row.id}`)}
                >
                  <EditIcon />
                </IconButton>
              </Tooltip>

              <Tooltip title="Delete">
                <IconButton
                  size="small"
                  color="error"
                  onClick={() => handleDeleteModel(row.id)}
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
        title="LLM Models"
        subtitle="Manage your language models"
        breadcrumbs={[
          { label: 'Dashboard', path: '/dashboard' },
          { label: 'LLM Models' }
        ]}
        action={
          isAdmin && (
            <Button
              variant="contained"
              color="primary"
              startIcon={<AddIcon />}
              onClick={() => navigate('/models/new')}
            >
              Add Model
            </Button>
          )
        }
      />

      {isModelsError && (
        <Alert severity="error" sx={{ mb: 3 }}>
          Error loading models: {modelsError instanceof Error ? modelsError.message : 'Unknown error'}
        </Alert>
      )}

      {modelsDebugInfo && typeof modelsDebugInfo === 'object' && !Array.isArray(modelsDebugInfo) && (
        <Alert severity="info" sx={{ mb: 3 }}>
          <Typography variant="body2">
            API Models Response detected (likely needs formatting): 
            The response appears to be an object with {Object.keys(modelsDebugInfo).length} keys: {Object.keys(modelsDebugInfo).join(', ')}
            {modelsDebugInfo.data && Array.isArray(modelsDebugInfo.data) && (
              <span> (Found {modelsDebugInfo.data.length} models in the 'data' property)</span>
            )}
          </Typography>
        </Alert>
      )}

      <Box sx={{ mb: 3, display: 'flex', justifyContent: 'flex-end' }}>
        <ProviderSelector
          value={selectedProviderId}
          onChange={handleProviderChange}
          sx={{ minWidth: 200 }}
          label="Filter by Provider"
          allOptionLabel="All Providers"
        />
      </Box>

      <DataTable
        columns={columns}
        data={models}
        title="Models"
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

export default ModelsListPage;
