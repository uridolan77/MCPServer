import React, { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import {
  Box,
  Button,
  Paper,
  Grid,
  Typography,
  Divider,
  Tab,
  Tabs,
  CircularProgress
} from '@mui/material';
import {
  Save as SaveIcon,
  ArrowBack as ArrowBackIcon
} from '@mui/icons-material';
import { PageHeader, TextFormField, SwitchFormField, SelectFormField } from '@/components';
import { llmProviderApi, LlmProvider } from '@/api';
import { useErrorHandler } from '@/hooks';
import { useNotification } from '@/contexts/NotificationContext';
import { validationUtils } from '@/utils';
import { useAuth } from '@/contexts/AuthContext';
import { ModelsTable, CredentialsTable } from './components';

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
      id={`provider-tabpanel-${index}`}
      aria-labelledby={`provider-tab-${index}`}
      {...other}
    >
      {value === index && <Box sx={{ p: 3 }}>{children}</Box>}
    </div>
  );
};

const ProviderDetailsPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { addNotification } = useNotification();
  const { handleError } = useErrorHandler();
  const { user } = useAuth();

  const isAdmin = user?.roles.includes('Admin');
  const isNewProvider = id === 'new';
  const [tabValue, setTabValue] = useState(0);

  // Fetch provider details if editing
  const {
    data: provider,
    isLoading: isLoadingProvider,
    isError
  } = useQuery({
    queryKey: ['provider', id],
    queryFn: () => llmProviderApi.getProviderById(Number(id)),
    enabled: !isNewProvider && !!id
  });

  // Form setup
  const {
    control,
    handleSubmit,
    reset,
    formState: { errors, isDirty, isSubmitting }
  } = useForm<LlmProvider>({
    resolver: zodResolver(validationUtils.providerSchema),
    defaultValues: isNewProvider
      ? {
          name: '',
          displayName: '',
          apiEndpoint: '',
          description: '',
          isEnabled: true,
          authType: 'ApiKey',
          configSchema: '{}'
        }
      : undefined
  });

  // Set form values when provider data is loaded
  React.useEffect(() => {
    if (provider && !isNewProvider) {
      reset(provider);
    }
  }, [provider, reset, isNewProvider]);

  // Create provider mutation
  const createProviderMutation = useMutation({
    mutationFn: llmProviderApi.createProvider,
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ['providers'] });
      addNotification('Provider created successfully', 'success');
      navigate(`/providers/${data.id}`);
    },
    onError: (error) => {
      handleError(error, 'Failed to create provider');
    }
  });

  // Update provider mutation
  const updateProviderMutation = useMutation({
    mutationFn: (provider: LlmProvider) =>
      llmProviderApi.updateProvider(provider.id, provider),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['providers'] });
      queryClient.invalidateQueries({ queryKey: ['provider', id] });
      addNotification('Provider updated successfully', 'success');
    },
    onError: (error) => {
      handleError(error, 'Failed to update provider');
    }
  });

  // Handle form submission
  const onSubmit = async (data: LlmProvider) => {
    if (isNewProvider) {
      createProviderMutation.mutate(data);
    } else {
      updateProviderMutation.mutate(data);
    }
  };

  // Handle tab change
  const handleTabChange = (_: React.SyntheticEvent, newValue: number) => {
    setTabValue(newValue);
  };

  // If error loading provider
  if (isError && !isNewProvider) {
    return (
      <Box>
        <PageHeader
          title="Provider Not Found"
          breadcrumbs={[
            { label: 'Dashboard', path: '/dashboard' },
            { label: 'LLM Providers', path: '/providers' },
            { label: 'Not Found' }
          ]}
        />
        <Paper sx={{ p: 3, textAlign: 'center' }}>
          <Typography variant="h6" color="error" gutterBottom>
            Error: Provider not found
          </Typography>
          <Button
            variant="contained"
            startIcon={<ArrowBackIcon />}
            onClick={() => navigate('/providers')}
            sx={{ mt: 2 }}
          >
            Back to Providers
          </Button>
        </Paper>
      </Box>
    );
  }

  // Loading state
  if (isLoadingProvider && !isNewProvider) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', mt: 4 }}>
        <CircularProgress />
      </Box>
    );
  }

  return (
    <Box>
      <PageHeader
        title={isNewProvider ? 'Add New Provider' : `Provider: ${provider?.displayName || provider?.name}`}
        subtitle={isNewProvider ? 'Create a new LLM provider' : 'View and edit provider details'}
        breadcrumbs={[
          { label: 'Dashboard', path: '/dashboard' },
          { label: 'LLM Providers', path: '/providers' },
          { label: isNewProvider ? 'New Provider' : provider?.name || '' }
        ]}
        action={
          <Button
            variant="outlined"
            startIcon={<ArrowBackIcon />}
            onClick={() => navigate('/providers')}
          >
            Back to List
          </Button>
        }
      />

      <Paper sx={{ mb: 3 }}>
        <Tabs
          value={tabValue}
          onChange={handleTabChange}
          aria-label="provider tabs"
          sx={{ borderBottom: 1, borderColor: 'divider' }}
        >
          <Tab label="Basic Information" />
          <Tab label="Configuration Schema" />
          {!isNewProvider && <Tab label="Models" />}
          {!isNewProvider && <Tab label="Credentials" />}
        </Tabs>

        <form onSubmit={handleSubmit(onSubmit)}>
          <TabPanel value={tabValue} index={0}>
            <Grid container spacing={3}>
              <Grid item xs={12} md={6}>
                <TextFormField
                  name="name"
                  control={control}
                  label="Provider Name"
                  error={errors.name}
                  required
                  disabled={!isAdmin || isSubmitting}
                  helperText="Unique identifier for the provider (e.g., 'OpenAI')"
                />
              </Grid>

              <Grid item xs={12} md={6}>
                <TextFormField
                  name="displayName"
                  control={control}
                  label="Display Name"
                  error={errors.displayName}
                  required
                  disabled={!isAdmin || isSubmitting}
                  helperText="User-friendly name to display in the UI"
                />
              </Grid>

              <Grid item xs={12}>
                <TextFormField
                  name="apiEndpoint"
                  control={control}
                  label="API Endpoint"
                  error={errors.apiEndpoint}
                  required
                  disabled={!isAdmin || isSubmitting}
                  helperText="Base URL for API requests (e.g., 'https://api.openai.com/v1/chat/completions')"
                />
              </Grid>

              <Grid item xs={12} md={6}>
                <SelectFormField
                  name="authType"
                  control={control}
                  label="Authentication Type"
                  error={errors.authType}
                  required
                  disabled={!isAdmin || isSubmitting}
                  options={[
                    { value: 'ApiKey', label: 'API Key' },
                    { value: 'OAuth', label: 'OAuth' },
                    { value: 'Basic', label: 'Basic Auth' },
                    { value: 'Custom', label: 'Custom' }
                  ]}
                />
              </Grid>

              <Grid item xs={12} md={6}>
                <SwitchFormField
                  name="isEnabled"
                  control={control}
                  switchLabel="Provider Enabled"
                  error={errors.isEnabled}
                  disabled={!isAdmin || isSubmitting}
                />
              </Grid>

              <Grid item xs={12}>
                <TextFormField
                  name="description"
                  control={control}
                  label="Description"
                  error={errors.description}
                  multiline
                  rows={4}
                  disabled={!isAdmin || isSubmitting}
                />
              </Grid>
            </Grid>
          </TabPanel>

          <TabPanel value={tabValue} index={1}>
            <TextFormField
              name="configSchema"
              control={control}
              label="Configuration Schema (JSON)"
              error={errors.configSchema}
              required
              multiline
              rows={15}
              disabled={!isAdmin || isSubmitting}
              helperText="JSON schema defining the configuration options for this provider"
            />
          </TabPanel>

          {!isNewProvider && (
            <TabPanel value={tabValue} index={2}>
              <Typography variant="h6" gutterBottom>
                Models
              </Typography>
              <Typography variant="body2" color="text.secondary" paragraph>
                Models associated with this provider
              </Typography>

              <Box sx={{ mb: 3 }}>
                <Button
                  variant="contained"
                  color="primary"
                  onClick={() => navigate(`/models/new?providerId=${id}`)}
                  disabled={!isAdmin}
                >
                  Add Model
                </Button>
              </Box>

              <ModelsTable providerId={Number(id)} />
            </TabPanel>
          )}

          {!isNewProvider && (
            <TabPanel value={tabValue} index={3}>
              <Typography variant="h6" gutterBottom>
                Credentials
              </Typography>
              <Typography variant="body2" color="text.secondary" paragraph>
                API keys and credentials associated with this provider
              </Typography>

              <Box sx={{ mb: 3 }}>
                <Button
                  variant="contained"
                  color="primary"
                  onClick={() => navigate(`/credentials/new?providerId=${id}`)}
                >
                  Add Credential
                </Button>
              </Box>

              <CredentialsTable providerId={Number(id)} />
            </TabPanel>
          )}

          {isAdmin && (tabValue === 0 || tabValue === 1) && (
            <Box sx={{ p: 3, pt: 0, display: 'flex', justifyContent: 'flex-end' }}>
              <Button
                type="submit"
                variant="contained"
                color="primary"
                startIcon={<SaveIcon />}
                disabled={isSubmitting || (!isDirty && !isNewProvider)}
              >
                {isSubmitting ? 'Saving...' : isNewProvider ? 'Create Provider' : 'Save Changes'}
              </Button>
            </Box>
          )}
        </form>
      </Paper>
    </Box>
  );
};

export default ProviderDetailsPage;
