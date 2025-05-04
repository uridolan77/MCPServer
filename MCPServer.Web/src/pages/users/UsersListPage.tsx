import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Box,
  Button,
  Chip,
  IconButton,
  Tooltip,
  Typography
} from '@mui/material';
import {
  Add as AddIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
  Visibility as VisibilityIcon,
  Lock as LockIcon
} from '@mui/icons-material';
import { PageHeader, DataTable, ConfirmDialog } from '@/components';
import { userApi, User } from '@/api';
import { useConfirmDialog, useErrorHandler } from '@/hooks';
import { useAuth } from '@/contexts/AuthContext';
import { useNotification } from '@/contexts/NotificationContext';
import { dateUtils } from '@/utils';

const UsersListPage: React.FC = () => {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { user: currentUser } = useAuth();
  const { addNotification } = useNotification();
  const { handleError } = useErrorHandler();
  const confirmDialog = useConfirmDialog();
  
  // Fetch users
  const {
    data: users = [],
    isLoading,
    refetch
  } = useQuery({
    queryKey: ['users'],
    queryFn: userApi.getAllUsers
  });
  
  // Delete user mutation
  const deleteUserMutation = useMutation({
    mutationFn: userApi.deleteUser,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] });
      addNotification('User deleted successfully', 'success');
      confirmDialog.hideDialog();
    },
    onError: (error) => {
      handleError(error, 'Failed to delete user');
      confirmDialog.hideDialog();
    }
  });
  
  // Reset password mutation
  const resetPasswordMutation = useMutation({
    mutationFn: userApi.resetPassword,
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ['users'] });
      addNotification('Password reset successfully', 'success');
      confirmDialog.hideDialog();
      
      // Show temporary password
      confirmDialog.showDialog({
        title: 'Temporary Password',
        message: `The temporary password is: ${data.temporaryPassword}\n\nPlease share this with the user securely.`,
        confirmLabel: 'Copy to Clipboard',
        cancelLabel: 'Close',
        onConfirm: () => {
          navigator.clipboard.writeText(data.temporaryPassword);
          addNotification('Password copied to clipboard', 'info');
          confirmDialog.hideDialog();
        }
      });
    },
    onError: (error) => {
      handleError(error, 'Failed to reset password');
      confirmDialog.hideDialog();
    }
  });
  
  // Handle delete user
  const handleDeleteUser = (id: string) => {
    confirmDialog.showDialog({
      title: 'Delete User',
      message: 'Are you sure you want to delete this user? This action cannot be undone.',
      confirmLabel: 'Delete',
      confirmColor: 'error',
      onConfirm: () => {
        confirmDialog.setLoading(true);
        deleteUserMutation.mutate(id);
      }
    });
  };
  
  // Handle reset password
  const handleResetPassword = (id: string) => {
    confirmDialog.showDialog({
      title: 'Reset Password',
      message: 'Are you sure you want to reset this user\'s password? A temporary password will be generated.',
      confirmLabel: 'Reset Password',
      confirmColor: 'warning',
      onConfirm: () => {
        confirmDialog.setLoading(true);
        resetPasswordMutation.mutate(id);
      }
    });
  };
  
  // Table columns
  const columns = [
    {
      id: 'username' as keyof User,
      label: 'Username',
      minWidth: 150,
      sortable: true,
      searchable: true
    },
    {
      id: 'email' as keyof User,
      label: 'Email',
      minWidth: 200,
      sortable: true,
      searchable: true
    },
    {
      id: 'roles' as keyof User,
      label: 'Roles',
      minWidth: 150,
      sortable: false,
      format: (value: string[]) => (
        <Box>
          {value.map((role) => (
            <Chip
              key={role}
              label={role}
              color={role === 'Admin' ? 'primary' : 'default'}
              size="small"
              sx={{ mr: 0.5, mb: 0.5 }}
            />
          ))}
        </Box>
      )
    },
    {
      id: 'isActive' as keyof User,
      label: 'Status',
      minWidth: 100,
      align: 'center',
      sortable: true,
      format: (value: boolean) => (
        <Chip
          label={value ? 'Active' : 'Inactive'}
          color={value ? 'success' : 'error'}
          size="small"
        />
      )
    },
    {
      id: 'lastLoginAt' as keyof User,
      label: 'Last Login',
      minWidth: 150,
      sortable: true,
      format: (value: string) => dateUtils.formatDateTime(value)
    },
    {
      id: 'actions',
      label: 'Actions',
      minWidth: 200,
      align: 'center',
      format: (row: User) => (
        <Box sx={{ display: 'flex', justifyContent: 'center' }}>
          <Tooltip title="View Details">
            <IconButton
              size="small"
              color="info"
              onClick={() => navigate(`/users/${row.id}`)}
            >
              <VisibilityIcon />
            </IconButton>
          </Tooltip>
          
          <Tooltip title="Edit">
            <IconButton
              size="small"
              color="primary"
              onClick={() => navigate(`/users/${row.id}`)}
            >
              <EditIcon />
            </IconButton>
          </Tooltip>
          
          <Tooltip title="Reset Password">
            <IconButton
              size="small"
              color="warning"
              onClick={() => handleResetPassword(row.id)}
              disabled={row.id === currentUser?.id}
            >
              <LockIcon />
            </IconButton>
          </Tooltip>
          
          <Tooltip title="Delete">
            <IconButton
              size="small"
              color="error"
              onClick={() => handleDeleteUser(row.id)}
              disabled={row.id === currentUser?.id}
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
        title="User Management"
        subtitle="Manage system users and their permissions"
        breadcrumbs={[
          { label: 'Dashboard', path: '/dashboard' },
          { label: 'Users' }
        ]}
        action={
          <Button
            variant="contained"
            color="primary"
            startIcon={<AddIcon />}
            onClick={() => navigate('/users/new')}
          >
            Add User
          </Button>
        }
      />
      
      <DataTable
        columns={columns}
        data={users}
        title="Users"
        isLoading={isLoading}
        onRefresh={refetch}
        getRowId={(row) => row.id}
        defaultSortColumn="username"
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

export default UsersListPage;
