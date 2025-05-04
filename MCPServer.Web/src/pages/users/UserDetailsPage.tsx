import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import {
  Box,
  Button,
  Paper,
  Grid,
  Typography,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Checkbox,
  ListItemText,
  OutlinedInput,
  FormHelperText,
  Switch,
  FormControlLabel,
  Divider,
  CircularProgress,
  Tabs,
  Tab
} from '@mui/material';
import {
  Save as SaveIcon,
  ArrowBack as ArrowBackIcon,
  Lock as LockIcon
} from '@mui/icons-material';
import { PageHeader, ConfirmDialog } from '@/components';
import { userApi, User } from '@/api';
import { useErrorHandler } from '@/hooks';
import { useNotification } from '@/contexts/NotificationContext';
import { useConfirmDialog } from '@/hooks';
import { useAuth } from '@/contexts/AuthContext';
import { validationUtils } from '@/utils';

interface TabPanelProps {
  children?: React.ReactNode;
  index: number;
  value: number;
}

const TabPanel: React.FC<TabPanelProps> = ({ children, value, index, ...other }) => {
  return (
    <div
      role="tabpanel"
      hidden={value !== index}
      id={`user-tabpanel-${index}`}
      aria-labelledby={`user-tab-${index}`}
      {...other}
    >
      {value === index && <Box sx={{ p: 3 }}>{children}</Box>}
    </div>
  );
};

const ITEM_HEIGHT = 48;
const ITEM_PADDING_TOP = 8;
const MenuProps = {
  PaperProps: {
    style: {
      maxHeight: ITEM_HEIGHT * 4.5 + ITEM_PADDING_TOP,
      width: 250,
    },
  },
};

const availableRoles = ['Admin', 'User', 'ReadOnly'];

const UserDetailsPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { addNotification } = useNotification();
  const { handleError } = useErrorHandler();
  const confirmDialog = useConfirmDialog();
  const { user: currentUser } = useAuth();
  
  const isNewUser = id === 'new';
  const [tabValue, setTabValue] = useState(0);
  
  // Fetch user details if editing
  const {
    data: user,
    isLoading: isLoadingUser,
    isError
  } = useQuery({
    queryKey: ['user', id],
    queryFn: () => userApi.getUserById(id!),
    enabled: !isNewUser && !!id
  });
  
  // Form setup
  const {
    register,
    handleSubmit,
    control,
    reset,
    formState: { errors, isDirty, isSubmitting }
  } = useForm<User>({
    resolver: zodResolver(isNewUser ? validationUtils.createUserSchema : validationUtils.updateUserSchema),
    defaultValues: isNewUser
      ? {
          username: '',
          email: '',
          firstName: '',
          lastName: '',
          roles: ['User'],
          isActive: true
        }
      : undefined
  });
  
  // Set form values when user data is loaded
  useEffect(() => {
    if (user && !isNewUser) {
      reset(user);
    }
  }, [user, reset, isNewUser]);
  
  // Create user mutation
  const createUserMutation = useMutation({
    mutationFn: userApi.createUser,
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ['users'] });
      addNotification('User created successfully', 'success');
      
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
          navigate(`/users/${data.id}`);
        },
        onCancel: () => {
          confirmDialog.hideDialog();
          navigate(`/users/${data.id}`);
        }
      });
    },
    onError: (error) => {
      handleError(error, 'Failed to create user');
    }
  });
  
  // Update user mutation
  const updateUserMutation = useMutation({
    mutationFn: (user: User) => 
      userApi.updateUser(user.id, user),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] });
      queryClient.invalidateQueries({ queryKey: ['user', id] });
      addNotification('User updated successfully', 'success');
    },
    onError: (error) => {
      handleError(error, 'Failed to update user');
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
  
  // Handle form submission
  const onSubmit = async (data: User) => {
    if (isNewUser) {
      createUserMutation.mutate(data);
    } else {
      updateUserMutation.mutate(data);
    }
  };
  
  // Handle tab change
  const handleTabChange = (_: React.SyntheticEvent, newValue: number) => {
    setTabValue(newValue);
  };
  
  // Handle reset password
  const handleResetPassword = () => {
    confirmDialog.showDialog({
      title: 'Reset Password',
      message: 'Are you sure you want to reset this user\'s password? A temporary password will be generated.',
      confirmLabel: 'Reset Password',
      confirmColor: 'warning',
      onConfirm: () => {
        confirmDialog.setLoading(true);
        resetPasswordMutation.mutate(id!);
      }
    });
  };
  
  // If error loading user
  if (isError && !isNewUser) {
    return (
      <Box>
        <PageHeader
          title="User Not Found"
          breadcrumbs={[
            { label: 'Dashboard', path: '/dashboard' },
            { label: 'Users', path: '/users' },
            { label: 'Not Found' }
          ]}
        />
        <Paper sx={{ p: 3, textAlign: 'center' }}>
          <Typography variant="h6" color="error" gutterBottom>
            Error: User not found
          </Typography>
          <Button
            variant="contained"
            startIcon={<ArrowBackIcon />}
            onClick={() => navigate('/users')}
            sx={{ mt: 2 }}
          >
            Back to Users
          </Button>
        </Paper>
      </Box>
    );
  }
  
  // Loading state
  if (isLoadingUser && !isNewUser) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', mt: 4 }}>
        <CircularProgress />
      </Box>
    );
  }
  
  // Check if current user is editing themselves
  const isSelfEdit = !isNewUser && user?.id === currentUser?.id;
  
  return (
    <Box>
      <PageHeader
        title={isNewUser ? 'Add New User' : `User: ${user?.username}`}
        subtitle={isNewUser ? 'Create a new user account' : 'View and edit user details'}
        breadcrumbs={[
          { label: 'Dashboard', path: '/dashboard' },
          { label: 'Users', path: '/users' },
          { label: isNewUser ? 'New User' : user?.username || '' }
        ]}
        action={
          <Box sx={{ display: 'flex', gap: 2 }}>
            {!isNewUser && (
              <Button
                variant="outlined"
                color="warning"
                startIcon={<LockIcon />}
                onClick={handleResetPassword}
                disabled={isSubmitting}
              >
                Reset Password
              </Button>
            )}
            <Button
              variant="outlined"
              startIcon={<ArrowBackIcon />}
              onClick={() => navigate('/users')}
            >
              Back to List
            </Button>
          </Box>
        }
      />
      
      <Paper sx={{ mb: 3 }}>
        <Tabs
          value={tabValue}
          onChange={handleTabChange}
          aria-label="user tabs"
          sx={{ borderBottom: 1, borderColor: 'divider' }}
        >
          <Tab label="Basic Information" />
          <Tab label="Permissions" />
          {!isNewUser && <Tab label="Activity" />}
        </Tabs>
        
        <form onSubmit={handleSubmit(onSubmit)}>
          <TabPanel value={tabValue} index={0}>
            <Grid container spacing={3}>
              <Grid item xs={12} md={6}>
                <TextField
                  fullWidth
                  label="Username"
                  {...register('username')}
                  error={!!errors.username}
                  helperText={errors.username?.message}
                  disabled={!isNewUser || isSubmitting}
                  required
                />
              </Grid>
              
              <Grid item xs={12} md={6}>
                <TextField
                  fullWidth
                  label="Email"
                  type="email"
                  {...register('email')}
                  error={!!errors.email}
                  helperText={errors.email?.message}
                  disabled={isSubmitting}
                  required
                />
              </Grid>
              
              <Grid item xs={12} md={6}>
                <TextField
                  fullWidth
                  label="First Name"
                  {...register('firstName')}
                  error={!!errors.firstName}
                  helperText={errors.firstName?.message}
                  disabled={isSubmitting}
                />
              </Grid>
              
              <Grid item xs={12} md={6}>
                <TextField
                  fullWidth
                  label="Last Name"
                  {...register('lastName')}
                  error={!!errors.lastName}
                  helperText={errors.lastName?.message}
                  disabled={isSubmitting}
                />
              </Grid>
              
              <Grid item xs={12}>
                <FormControlLabel
                  control={
                    <Controller
                      name="isActive"
                      control={control}
                      render={({ field }) => (
                        <Switch
                          checked={field.value}
                          onChange={(e) => field.onChange(e.target.checked)}
                          disabled={isSelfEdit || isSubmitting}
                        />
                      )}
                    />
                  }
                  label="Active Account"
                />
                {isSelfEdit && (
                  <FormHelperText>You cannot deactivate your own account</FormHelperText>
                )}
              </Grid>
            </Grid>
          </TabPanel>
          
          <TabPanel value={tabValue} index={1}>
            <Grid container spacing={3}>
              <Grid item xs={12}>
                <FormControl fullWidth error={!!errors.roles}>
                  <InputLabel id="roles-label">Roles</InputLabel>
                  <Controller
                    name="roles"
                    control={control}
                    render={({ field }) => (
                      <Select
                        labelId="roles-label"
                        multiple
                        value={field.value || []}
                        onChange={field.onChange}
                        input={<OutlinedInput label="Roles" />}
                        renderValue={(selected) => selected.join(', ')}
                        MenuProps={MenuProps}
                        disabled={isSelfEdit || isSubmitting}
                      >
                        {availableRoles.map((role) => (
                          <MenuItem key={role} value={role}>
                            <Checkbox checked={(field.value || []).indexOf(role) > -1} />
                            <ListItemText primary={role} />
                          </MenuItem>
                        ))}
                      </Select>
                    )}
                  />
                  {errors.roles && (
                    <FormHelperText>{errors.roles.message}</FormHelperText>
                  )}
                  {isSelfEdit && (
                    <FormHelperText>You cannot change your own roles</FormHelperText>
                  )}
                </FormControl>
              </Grid>
            </Grid>
          </TabPanel>
          
          {!isNewUser && (
            <TabPanel value={tabValue} index={2}>
              <Grid container spacing={3}>
                <Grid item xs={12} md={6}>
                  <Typography variant="subtitle1">Account Created</Typography>
                  <Typography variant="body1">
                    {user?.createdAt ? new Date(user.createdAt).toLocaleString() : 'N/A'}
                  </Typography>
                </Grid>
                
                <Grid item xs={12} md={6}>
                  <Typography variant="subtitle1">Last Login</Typography>
                  <Typography variant="body1">
                    {user?.lastLoginAt ? new Date(user.lastLoginAt).toLocaleString() : 'Never'}
                  </Typography>
                </Grid>
                
                <Grid item xs={12}>
                  <Divider sx={{ my: 2 }} />
                  <Typography variant="h6" gutterBottom>
                    Activity Log
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    User activity log will be displayed here in a future update.
                  </Typography>
                </Grid>
              </Grid>
            </TabPanel>
          )}
          
          <Box sx={{ p: 3, pt: 0, display: 'flex', justifyContent: 'flex-end' }}>
            <Button
              type="submit"
              variant="contained"
              color="primary"
              startIcon={<SaveIcon />}
              disabled={isSubmitting || (!isDirty && !isNewUser)}
            >
              {isSubmitting ? 'Saving...' : isNewUser ? 'Create User' : 'Save Changes'}
            </Button>
          </Box>
        </form>
      </Paper>
      
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

export default UserDetailsPage;
