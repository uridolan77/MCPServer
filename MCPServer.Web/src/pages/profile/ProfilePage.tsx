import React, { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import {
  Box,
  Paper,
  Grid,
  Typography,
  Button,
  Divider,
  Tab,
  Tabs,
  Alert
} from '@mui/material';
import {
  Save as SaveIcon,
  Lock as LockIcon
} from '@mui/icons-material';
import { PageHeader, TextFormField } from '@/components';
import { useAuth } from '@/contexts/AuthContext';
import { useNotification } from '@/contexts/NotificationContext';
import { useErrorHandler } from '@/hooks';
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
      id={`profile-tabpanel-${index}`}
      aria-labelledby={`profile-tab-${index}`}
      {...other}
    >
      {value === index && <Box sx={{ p: 3 }}>{children}</Box>}
    </div>
  );
};

// Profile form schema
const profileSchema = z.object({
  firstName: z.string().optional(),
  lastName: z.string().optional(),
  email: validationUtils.emailSchema
});

// Password change schema
const passwordSchema = z.object({
  currentPassword: z.string().min(1, 'Current password is required'),
  newPassword: validationUtils.passwordSchema,
  confirmPassword: z.string().min(1, 'Confirm password is required')
}).refine(data => data.newPassword === data.confirmPassword, {
  message: 'Passwords do not match',
  path: ['confirmPassword']
});

type ProfileFormData = z.infer<typeof profileSchema>;
type PasswordFormData = z.infer<typeof passwordSchema>;

const ProfilePage: React.FC = () => {
  const { user } = useAuth();
  const { addNotification } = useNotification();
  const { handleError } = useErrorHandler();
  const [tabValue, setTabValue] = useState(0);
  
  // Profile form
  const {
    control: profileControl,
    handleSubmit: handleProfileSubmit,
    formState: { errors: profileErrors, isDirty: isProfileDirty, isSubmitting: isProfileSubmitting }
  } = useForm<ProfileFormData>({
    resolver: zodResolver(profileSchema),
    defaultValues: {
      firstName: user?.firstName || '',
      lastName: user?.lastName || '',
      email: user?.email || ''
    }
  });
  
  // Password form
  const {
    control: passwordControl,
    handleSubmit: handlePasswordSubmit,
    reset: resetPasswordForm,
    formState: { errors: passwordErrors, isSubmitting: isPasswordSubmitting }
  } = useForm<PasswordFormData>({
    resolver: zodResolver(passwordSchema),
    defaultValues: {
      currentPassword: '',
      newPassword: '',
      confirmPassword: ''
    }
  });
  
  // Handle tab change
  const handleTabChange = (_: React.SyntheticEvent, newValue: number) => {
    setTabValue(newValue);
  };
  
  // Handle profile update
  const onProfileSubmit = async (data: ProfileFormData) => {
    try {
      // TODO: Implement profile update API call
      // await userApi.updateUser(user.id, data);
      
      // For now, just simulate a successful update
      setTimeout(() => {
        addNotification('Profile updated successfully', 'success');
      }, 1000);
    } catch (error) {
      handleError(error, 'Failed to update profile');
    }
  };
  
  // Handle password change
  const onPasswordSubmit = async (data: PasswordFormData) => {
    try {
      // TODO: Implement password change API call
      // await userApi.changePassword(user.id, data.currentPassword, data.newPassword);
      
      // For now, just simulate a successful update
      setTimeout(() => {
        addNotification('Password changed successfully', 'success');
        resetPasswordForm();
      }, 1000);
    } catch (error) {
      handleError(error, 'Failed to change password');
    }
  };
  
  return (
    <Box>
      <PageHeader
        title="My Profile"
        subtitle="View and update your profile information"
        breadcrumbs={[
          { label: 'Dashboard', path: '/dashboard' },
          { label: 'My Profile' }
        ]}
      />
      
      <Paper sx={{ mb: 3 }}>
        <Tabs
          value={tabValue}
          onChange={handleTabChange}
          aria-label="profile tabs"
          sx={{ borderBottom: 1, borderColor: 'divider' }}
        >
          <Tab label="Profile Information" />
          <Tab label="Change Password" />
          <Tab label="API Keys" />
        </Tabs>
        
        <TabPanel value={tabValue} index={0}>
          <form onSubmit={handleProfileSubmit(onProfileSubmit)}>
            <Grid container spacing={3}>
              <Grid item xs={12} md={6}>
                <TextFormField
                  name="firstName"
                  control={profileControl}
                  label="First Name"
                  error={profileErrors.firstName}
                  disabled={isProfileSubmitting}
                />
              </Grid>
              
              <Grid item xs={12} md={6}>
                <TextFormField
                  name="lastName"
                  control={profileControl}
                  label="Last Name"
                  error={profileErrors.lastName}
                  disabled={isProfileSubmitting}
                />
              </Grid>
              
              <Grid item xs={12}>
                <TextFormField
                  name="email"
                  control={profileControl}
                  label="Email Address"
                  type="email"
                  error={profileErrors.email}
                  required
                  disabled={isProfileSubmitting}
                />
              </Grid>
              
              <Grid item xs={12}>
                <Box sx={{ display: 'flex', justifyContent: 'flex-end' }}>
                  <Button
                    type="submit"
                    variant="contained"
                    color="primary"
                    startIcon={<SaveIcon />}
                    disabled={isProfileSubmitting || !isProfileDirty}
                  >
                    {isProfileSubmitting ? 'Saving...' : 'Save Changes'}
                  </Button>
                </Box>
              </Grid>
            </Grid>
          </form>
        </TabPanel>
        
        <TabPanel value={tabValue} index={1}>
          <form onSubmit={handlePasswordSubmit(onPasswordSubmit)}>
            <Grid container spacing={3}>
              <Grid item xs={12}>
                <Alert severity="info" sx={{ mb: 2 }}>
                  Your password must be at least 8 characters long and include uppercase, lowercase, number, and special character.
                </Alert>
              </Grid>
              
              <Grid item xs={12}>
                <TextFormField
                  name="currentPassword"
                  control={passwordControl}
                  label="Current Password"
                  type="password"
                  error={passwordErrors.currentPassword}
                  required
                  disabled={isPasswordSubmitting}
                />
              </Grid>
              
              <Grid item xs={12}>
                <TextFormField
                  name="newPassword"
                  control={passwordControl}
                  label="New Password"
                  type="password"
                  error={passwordErrors.newPassword}
                  required
                  disabled={isPasswordSubmitting}
                />
              </Grid>
              
              <Grid item xs={12}>
                <TextFormField
                  name="confirmPassword"
                  control={passwordControl}
                  label="Confirm New Password"
                  type="password"
                  error={passwordErrors.confirmPassword}
                  required
                  disabled={isPasswordSubmitting}
                />
              </Grid>
              
              <Grid item xs={12}>
                <Box sx={{ display: 'flex', justifyContent: 'flex-end' }}>
                  <Button
                    type="submit"
                    variant="contained"
                    color="primary"
                    startIcon={<LockIcon />}
                    disabled={isPasswordSubmitting}
                  >
                    {isPasswordSubmitting ? 'Changing...' : 'Change Password'}
                  </Button>
                </Box>
              </Grid>
            </Grid>
          </form>
        </TabPanel>
        
        <TabPanel value={tabValue} index={2}>
          <Typography variant="h6" gutterBottom>
            My API Keys
          </Typography>
          <Typography variant="body2" color="text.secondary" paragraph>
            This section will display your personal API keys for different providers.
          </Typography>
          
          <Button
            variant="contained"
            color="primary"
            onClick={() => window.location.href = '/credentials'}
          >
            Manage API Keys
          </Button>
          
          {/* API keys list will be implemented separately */}
          <Box sx={{ mt: 2, p: 2, bgcolor: 'action.hover', borderRadius: 1 }}>
            <Typography variant="body2" color="text.secondary" align="center">
              Your API keys will be displayed here
            </Typography>
          </Box>
        </TabPanel>
      </Paper>
      
      <Paper sx={{ p: 3 }}>
        <Typography variant="h6" gutterBottom>
          Account Information
        </Typography>
        <Divider sx={{ mb: 2 }} />
        
        <Grid container spacing={2}>
          <Grid item xs={12} sm={4}>
            <Typography variant="body2" color="text.secondary">
              Username
            </Typography>
            <Typography variant="body1">
              {user?.username}
            </Typography>
          </Grid>
          
          <Grid item xs={12} sm={4}>
            <Typography variant="body2" color="text.secondary">
              Roles
            </Typography>
            <Typography variant="body1">
              {user?.roles.join(', ')}
            </Typography>
          </Grid>
          
          <Grid item xs={12} sm={4}>
            <Typography variant="body2" color="text.secondary">
              Account Created
            </Typography>
            <Typography variant="body1">
              {user?.createdAt ? new Date(user.createdAt).toLocaleDateString() : 'N/A'}
            </Typography>
          </Grid>
        </Grid>
      </Paper>
    </Box>
  );
};

export default ProfilePage;
