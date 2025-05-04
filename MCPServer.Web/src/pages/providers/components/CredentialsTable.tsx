import React from 'react';
import { useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import {
  Box,
  Chip,
  IconButton,
  Tooltip
} from '@mui/material';
import {
  Edit as EditIcon,
  Visibility as VisibilityIcon
} from '@mui/icons-material';
import { DataTable } from '@/components';
import { llmProviderApi, Credential } from '@/api';
import { useErrorHandler } from '@/hooks';
import { dateUtils } from '@/utils';
import { useAuth } from '@/contexts/AuthContext';

interface CredentialsTableProps {
  providerId: number;
}

const CredentialsTable: React.FC<CredentialsTableProps> = ({ providerId }) => {
  const navigate = useNavigate();
  const { handleError } = useErrorHandler();
  const { user } = useAuth();
  
  // Fetch credentials for this provider
  const {
    data: credentials = [],
    isLoading,
    refetch
  } = useQuery({
    queryKey: ['credentials', 'provider', providerId],
    queryFn: () => llmProviderApi.getCredentialsByProviderId(providerId),
    onError: (error) => {
      handleError(error, 'Failed to fetch credentials');
    }
  });
  
  // Table columns
  const columns = [
    {
      id: 'name' as keyof Credential,
      label: 'Name',
      minWidth: 150,
      sortable: true,
      searchable: true
    },
    {
      id: 'keyType' as keyof Credential,
      label: 'Key Type',
      minWidth: 120,
      sortable: true
    },
    {
      id: 'isDefault' as keyof Credential,
      label: 'Default',
      minWidth: 100,
      align: 'center',
      sortable: true,
      format: (value: boolean) => (
        <Chip
          label={value ? 'Default' : 'No'}
          color={value ? 'primary' : 'default'}
          size="small"
        />
      )
    },
    {
      id: 'isEnabled' as keyof Credential,
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
      id: 'createdAt' as keyof Credential,
      label: 'Created',
      minWidth: 150,
      sortable: true,
      format: (value: string) => dateUtils.formatDateTime(value)
    },
    {
      id: 'actions',
      label: 'Actions',
      minWidth: 100,
      align: 'center',
      format: (row: Credential) => (
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
        </Box>
      )
    }
  ];
  
  return (
    <DataTable
      columns={columns}
      data={credentials}
      isLoading={isLoading}
      onRefresh={refetch}
      getRowId={(row) => row.id}
      defaultSortColumn="name"
      emptyMessage="No credentials found for this provider"
    />
  );
};

export default CredentialsTable;
