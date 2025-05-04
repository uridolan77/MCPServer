import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import {
  Box,
  Button,
  Chip,
  IconButton,
  Tooltip
} from '@mui/material';
import {
  Add as AddIcon,
  Edit as EditIcon,
  Visibility as VisibilityIcon
} from '@mui/icons-material';
import { DataTable } from '@/components';
import { llmProviderApi, LlmModel } from '@/api';
import { useErrorHandler } from '@/hooks';
import { formatUtils } from '@/utils';
import { useAuth } from '@/contexts/AuthContext';
import AddModelDialog from './AddModelDialog';

interface ModelsTableProps {
  providerId: number;
}

const ModelsTable: React.FC<ModelsTableProps> = ({ providerId }) => {
  const navigate = useNavigate();
  const { handleError } = useErrorHandler();
  const { user } = useAuth();
  const [isAddModelDialogOpen, setIsAddModelDialogOpen] = useState(false);
  
  const isAdmin = user?.roles.includes('Admin');
  
  // Fetch models for this provider
  const {
    data: models = [],
    isLoading,
    refetch
  } = useQuery({
    queryKey: ['models', 'provider', providerId],
    queryFn: () => llmProviderApi.getModelsByProviderId(providerId),
    onError: (error) => {
      handleError(error, 'Failed to fetch models');
    }
  });

  const handleAddModelSuccess = () => {
    refetch();
    setIsAddModelDialogOpen(false);
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
      minWidth: 100,
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
            <Tooltip title="Edit">
              <IconButton
                size="small"
                color="primary"
                onClick={() => navigate(`/models/${row.id}`)}
              >
                <EditIcon />
              </IconButton>
            </Tooltip>
          )}
        </Box>
      )
    }
  ];
  
  return (
    <>
      {isAdmin && (
        <Box sx={{ mb: 2, display: 'flex', justifyContent: 'flex-end' }}>
          <Button
            variant="contained"
            color="primary"
            startIcon={<AddIcon />}
            onClick={() => setIsAddModelDialogOpen(true)}
          >
            Add Model
          </Button>
        </Box>
      )}
      
      <DataTable
        columns={columns}
        data={models}
        isLoading={isLoading}
        onRefresh={refetch}
        getRowId={(row) => row.id}
        defaultSortColumn="name"
        emptyMessage="No models found for this provider"
      />
      
      {isAddModelDialogOpen && (
        <AddModelDialog
          open={isAddModelDialogOpen}
          onClose={() => setIsAddModelDialogOpen(false)}
          onSuccess={handleAddModelSuccess}
          providerId={providerId}
        />
      )}
    </>
  );
};

export default ModelsTable;
