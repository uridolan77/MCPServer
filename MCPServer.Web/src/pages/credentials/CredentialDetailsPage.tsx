import React, { useState, useEffect } from 'react';
import { useParams, useNavigate, useSearchParams } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import {
  Box,
  Button,
  Paper,
  Grid,
  Typography,
  CircularProgress,
  Alert,
  Divider,

  FormControl,
  InputLabel,
  Select,
  MenuItem,
  FormHelperText,
  SelectChangeEvent
} from '@mui/material';
import {
  Save as SaveIcon,
  ArrowBack as ArrowBackIcon,
  Refresh as RefreshIcon
} from '@mui/icons-material';
import {
  PageHeader,
  TextFormField,
  SwitchFormField
} from '@/components';
import { llmProviderApi, CredentialRequest, LlmProvider } from '@/api';
import { useErrorHandler } from '@/hooks';
import { useNotification } from '@/contexts/NotificationContext';
import { validationUtils } from '@/utils';
import { useAuth } from '@/contexts/AuthContext';

// Custom form type for credential creation/editing
interface CredentialFormData {
  providerId: string | number;  // Accept both string and number for flexibility
  name: string;
  isDefault: boolean;
  apiKey?: string;
  organizationId?: string;
  // Add other common credential fields as needed
}

const CredentialDetailsPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { addNotification } = useNotification();
  const { handleError } = useErrorHandler();
  const { user: _ } = useAuth(); // Unused but kept for future use
  const [debugInfo, setDebugInfo] = useState<any>(null);

  const isNewCredential = id === 'new';
  const [credentialFields, setCredentialFields] = useState<Record<string, any>>({});

  // Get providerId from URL if creating a new credential
  const preselectedProviderId = searchParams.get('providerId')
    ? parseInt(searchParams.get('providerId') as string)
    : undefined;

  // Fetch providers for dropdown - with higher priority
  const {
    data: providersData,
    isLoading: isLoadingProviders,
    error: providersError,
    refetch: refetchProviders,
    isSuccess: isProvidersSuccess
  } = useQuery({
    queryKey: ['providers'],
    queryFn: llmProviderApi.getAllProviders,
    staleTime: 60000, // 1 minute
    retry: 3,
    retryDelay: 1000,
    refetchOnMount: true,
    refetchOnWindowFocus: true
  });

  // Log raw API response for debugging
  useEffect(() => {
    console.log('Raw providers API response:', providersData);
    setDebugInfo(providersData);
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
      
      // Check if it has a $values property that's an array (common in .NET)
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

  // Log providers when they change and handle empty providers
  useEffect(() => {
    console.log('Providers loaded:', providers);
    if (isProvidersSuccess) {
      if (providers.length === 0) {
        console.warn('No providers found, will retry in 2 seconds');
        const timer = setTimeout(() => {
          console.log('Retrying provider fetch...');
          refetchProviders();
        }, 2000);
        return () => clearTimeout(timer);
      } else {
        console.log('Providers loaded successfully:', providers.length, 'providers');
      }
    }
  }, [providers, refetchProviders, isProvidersSuccess]);

  // Fetch credential details if editing
  const {
    data: credential,
    isLoading: isLoadingCredential,
    isError
  } = useQuery({
    queryKey: ['credential', id],
    queryFn: () => llmProviderApi.getCredentialById(Number(id)),
    enabled: !isNewCredential && !!id
  });

  // Form setup with empty string as default for providerId
  const {
    control,
    handleSubmit,
    reset,
    watch,
    setValue,
    getValues,
    formState: { errors, isDirty, isSubmitting }
  } = useForm<CredentialFormData>({
    resolver: zodResolver(validationUtils.credentialSchema),
    defaultValues: isNewCredential
      ? {
          providerId: '', // Use empty string instead of 0
          name: '',
          isDefault: false,
          apiKey: '',
          organizationId: ''
        }
      : undefined
  });

  // Set form values when credential data is loaded
  useEffect(() => {
    // Only proceed if we have both credential data and providers
    if (credential && !isNewCredential && isProvidersSuccess && providers.length > 0) {
      console.log('Setting form values with providerId:', credential.providerId);
      console.log('Credential data:', credential);
      console.log('Providers available:', providers.length);

      // Force providerId to be a number
      const providerId = typeof credential.providerId === 'string'
        ? parseInt(credential.providerId as string)
        : credential.providerId;

      console.log('Converted providerId:', providerId, 'Type:', typeof providerId);

      // Verify the provider exists in our list
      const providerExists = providers.some(p => {
        console.log(`Comparing provider ID ${p.id} (${typeof p.id}) with ${providerId} (${typeof providerId})`);
        return p.id === providerId;
      });
      console.log('Provider exists in list:', providerExists);

      if (!providerExists) {
        console.warn('Provider ID not found in available providers:', providerId);
        console.log('Available provider IDs:', providers.map(p => ({ id: p.id, type: typeof p.id })));

        // Try to find a provider with string comparison
        const stringMatchProvider = providers.find(p => String(p.id) === String(providerId));
        if (stringMatchProvider) {
          console.log('Found provider using string comparison:', stringMatchProvider);
        }
      }

      // First reset the form with empty values to clear any previous state
      reset({
        providerId: '', // Use empty string instead of 0
        name: '',
        isDefault: false
      });

      // Set all values at once to avoid race conditions
      setValue('providerId', providerId);
      setValue('name', credential.name);
      setValue('isDefault', credential.isDefault);
    }
  }, [credential, reset, setValue, getValues, isNewCredential, providers, isProvidersSuccess]);

  // Set providerId from URL if creating a new credential
  useEffect(() => {
    if (isNewCredential && preselectedProviderId && providers.length > 0) {
      setValue('providerId', preselectedProviderId);
    }
  }, [isNewCredential, preselectedProviderId, setValue, providers]);

  // Watch providerId to update credential fields
  const watchProviderId = watch('providerId');

  // Log the watched providerId and its type
  useEffect(() => {
    console.log('Current providerId value:', watchProviderId, 'type:', typeof watchProviderId);
  }, [watchProviderId]);

  // Update credential fields based on selected provider
  useEffect(() => {
    console.log('watchProviderId changed:', watchProviderId, 'type:', typeof watchProviderId);

    if (
      watchProviderId !== undefined && 
      watchProviderId !== null && 
      watchProviderId !== '' && 
      providers.length > 0
    ) {
      // Convert watchProviderId to number if it's a string
      const providerIdNum = typeof watchProviderId === 'string' ?
                            parseInt(watchProviderId) : watchProviderId;

      console.log('Looking for provider with ID:', providerIdNum, 'Type:', typeof providerIdNum);

      if (providers.length > 0) {
        console.log('Available providers:', providers.map(p => ({ id: p.id, name: p.name, type: typeof p.id })));

        // Try to find provider by ID with both numeric and string comparison
        let foundProvider = providers.find(p => p.id === providerIdNum);

        // If not found, try string comparison
        if (!foundProvider) {
          console.log('Provider not found with direct comparison, trying string comparison');
          foundProvider = providers.find(p => String(p.id) === String(providerIdNum));
        }

        if (foundProvider) {
          console.log('Found provider:', foundProvider);
          try {
            // Parse config schema to determine fields
            const schema = JSON.parse(foundProvider.configSchema || '{}');
            if (schema && schema.properties) {
              setCredentialFields(schema.properties);
            } else {
              console.warn('No properties in schema:', schema);
              setCredentialFields({});
            }
          } catch (error) {
            console.error('Error parsing config schema:', error);
            setCredentialFields({});
          }
        } else {
          console.warn('Provider not found with ID:', providerIdNum);
          setCredentialFields({});
        }
      } else {
        console.warn('No providers available');
        setCredentialFields({});
      }
    } else {
      console.log('No provider ID selected or providers not loaded');
      setCredentialFields({});
    }
  }, [watchProviderId, providers]);

  // Create credential mutation
  const createCredentialMutation = useMutation({
    mutationFn: (data: CredentialRequest) => llmProviderApi.createCredential(data),
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ['credentials'] });
      addNotification('API key created successfully', 'success');
      navigate(`/credentials/${data.id}`);
    },
    onError: (error) => {
      handleError(error, 'Failed to create API key');
    }
  });

  // Update credential mutation
  const updateCredentialMutation = useMutation({
    mutationFn: (data: { id: number, credential: Partial<CredentialRequest> }) =>
      llmProviderApi.updateCredential(data.id, data.credential),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['credentials'] });
      queryClient.invalidateQueries({ queryKey: ['credential', id] });
      addNotification('API key updated successfully', 'success');
    },
    onError: (error) => {
      handleError(error, 'Failed to update API key');
    }
  });

  // Set default credential mutation
  const setDefaultCredentialMutation = useMutation({
    mutationFn: (credentialId: number) =>
      llmProviderApi.setDefaultCredential(credentialId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['credentials'] });
      queryClient.invalidateQueries({ queryKey: ['credential', id] });
      addNotification('Default credential set successfully', 'success');
    },
    onError: (error) => {
      handleError(error, 'Failed to set default credential');
    }
  });

  // Handle form submission
  const onSubmit = async (data: CredentialFormData) => {
    // Ensure we have a valid providerId
    if (!data.providerId || data.providerId === '') {
      addNotification('Please select a provider', 'error');
      return;
    }

    // Prepare credentials object based on provider schema
    const credentials: Record<string, any> = {};

    // Add API key if provided
    if (data.apiKey) {
      credentials.apiKey = data.apiKey;
    }

    // Add organization ID if provided
    if (data.organizationId) {
      credentials.organization = data.organizationId;
    }

    // Add other fields from the form
    Object.keys(credentialFields).forEach(key => {
      if (data[key as keyof CredentialFormData] !== undefined) {
        credentials[key] = data[key as keyof CredentialFormData];
      }
    });

    if (isNewCredential) {
      // The providerId should already be converted to a number by the schema transform
      // but we'll ensure it's a number here just to be safe
      const providerId = typeof data.providerId === 'string'
        ? parseInt(data.providerId)
        : data.providerId;

      createCredentialMutation.mutate({
        providerId,
        name: data.name,
        credentials
      });
    } else {
      // For update, we'll handle isDefault separately from credentials

      // Ensure providerId is included in the update request
      const providerId = typeof data.providerId === 'string'
        ? parseInt(data.providerId)
        : data.providerId;

      // First update the credential
      updateCredentialMutation.mutate({
        id: Number(id),
        credential: {
          providerId: providerId,
          name: data.name,
          credentials: Object.keys(credentials).length > 0 ? credentials : undefined
        }
      });

      // If isDefault is true, make a separate call to set it as default
      if (data.isDefault && (!credential || !credential.isDefault)) {
        setDefaultCredentialMutation.mutate(Number(id));
      }
    }
  };

  // Get selected provider
  // Convert watchProviderId to number if it's a string and not empty
  const providerIdNum = watchProviderId && typeof watchProviderId === 'string' && watchProviderId !== '' ?
                        parseInt(watchProviderId) : 
                        (typeof watchProviderId === 'number' ? watchProviderId : null);

  // Find provider by ID with both numeric and string comparison
  let selectedProvider: LlmProvider | undefined = undefined;

  if (providers.length > 0 && providerIdNum !== null) {
    // Try direct comparison first
    selectedProvider = providers.find(p => p.id === providerIdNum);

    // If not found, try string comparison
    if (!selectedProvider) {
      selectedProvider = providers.find(p => String(p.id) === String(providerIdNum));
    }
  }

  // Try to find a provider if not already selected
  useEffect(() => {
    // If we have a providerId but no selected provider, check if the provider exists in the options
    if (providerIdNum && !selectedProvider && providers.length > 0) {
      // Try to find a provider with a string comparison
      const fallbackProvider = providers.find(p => String(p.id) === String(providerIdNum));
      if (fallbackProvider) {
        setValue('providerId', fallbackProvider.id);
      }
    }
  }, [selectedProvider, providerIdNum, providers, setValue]);

  // If error loading credential
  if (isError && !isNewCredential) {
    return (
      <Box>
        <PageHeader
          title="API Key Not Found"
          breadcrumbs={[
            { label: 'Dashboard', path: '/dashboard' },
            { label: 'API Keys', path: '/credentials' },
            { label: 'Not Found' }
          ]}
        />
        <Paper sx={{ p: 3, textAlign: 'center' }}>
          <Typography variant="h6" color="error" gutterBottom>
            Error: API key not found
          </Typography>
          <Button
            variant="contained"
            startIcon={<ArrowBackIcon />}
            onClick={() => navigate('/credentials')}
            sx={{ mt: 2 }}
          >
            Back to API Keys
          </Button>
        </Paper>
      </Box>
    );
  }

  // Loading state
  if (isLoadingCredential && !isNewCredential) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', mt: 4 }}>
        <CircularProgress />
      </Box>
    );
  }

  return (
    <Box>
      <PageHeader
        title={isNewCredential ? 'Add New API Key' : `API Key: ${credential?.name}`}
        subtitle={isNewCredential ? 'Create a new API key for an LLM provider' : 'View and edit API key details'}
        breadcrumbs={[
          { label: 'Dashboard', path: '/dashboard' },
          { label: 'API Keys', path: '/credentials' },
          { label: isNewCredential ? 'New API Key' : credential?.name || '' }
        ]}
        action={
          <Button
            variant="outlined"
            startIcon={<ArrowBackIcon />}
            onClick={() => navigate('/credentials')}
          >
            Back to List
          </Button>
        }
      />

      {debugInfo && typeof debugInfo === 'object' && !Array.isArray(debugInfo) && (
        <Alert severity="info" sx={{ mb: 3 }}>
          <Typography variant="body2">
            API Providers Response detected (likely needs formatting): 
            The response appears to be an object with {Object.keys(debugInfo).length} keys: {Object.keys(debugInfo).join(', ')}
          </Typography>
        </Alert>
      )}

      <Paper sx={{ p: 3, mb: 3 }}>
        <form onSubmit={handleSubmit(onSubmit)}>
          <Grid container spacing={3}>
            <Grid item xs={12} md={6}>
              {isLoadingProviders ? (
                <Box sx={{ display: 'flex', alignItems: 'center', mt: 2 }}>
                  <CircularProgress size={24} sx={{ mr: 1 }} />
                  <Typography variant="body2">Loading providers...</Typography>
                </Box>
              ) : (
                <Box>
                  <FormControl fullWidth margin="normal" error={!!errors.providerId} required>
                    <InputLabel id="provider-select-label">Provider</InputLabel>
                    <Select
                      labelId="provider-select-label"
                      id="provider-select"
                      value={watchProviderId || ''}
                      label="Provider"
                      onChange={(e: SelectChangeEvent<string | number>) => {
                        setValue('providerId', e.target.value);
                      }}
                      disabled={!isNewCredential && id !== 'new'}
                    >
                      {providers.length === 0 && (
                        <MenuItem value="">
                          <em>No providers available</em>
                        </MenuItem>
                      )}
                      {providers.map((provider) => (
                        <MenuItem key={provider.id} value={provider.id}>
                          {provider.displayName || provider.name}
                        </MenuItem>
                      ))}
                    </Select>
                    {errors.providerId && (
                      <FormHelperText>{errors.providerId.message}</FormHelperText>
                    )}
                    {!isNewCredential && (
                      <FormHelperText>Provider cannot be changed after creation</FormHelperText>
                    )}
                  </FormControl>
                  {providers.length === 0 && (
                    <Box sx={{ mt: 1, display: 'flex', alignItems: 'center' }}>
                      <Button
                        size="small"
                        startIcon={<RefreshIcon />}
                        onClick={() => refetchProviders()}
                        variant="outlined"
                      >
                        Refresh Providers
                      </Button>
                      <Typography variant="caption" sx={{ ml: 1 }}>
                        No providers found. Click to refresh.
                      </Typography>
                    </Box>
                  )}
                </Box>
              )}
              {providersError && (
                <Alert severity="error" sx={{ mt: 1 }}>
                  <Typography variant="body2" gutterBottom>
                    Error loading providers. This could be due to:
                  </Typography>
                  <ul style={{ margin: 0, paddingLeft: '1.5rem' }}>
                    <li>The API server is not running</li>
                    <li>The API endpoint is incorrect</li>
                    <li>There's a network issue</li>
                  </ul>
                  <Box sx={{ mt: 1 }}>
                    <Button
                      size="small"
                      variant="outlined"
                      onClick={() => refetchProviders()}
                      startIcon={<RefreshIcon />}
                    >
                      Retry
                    </Button>
                  </Box>
                </Alert>
              )}
            </Grid>

            <Grid item xs={12} md={6}>
              <TextFormField
                name="name"
                control={control}
                label="Credential Name"
                error={errors.name}
                required
                disabled={isSubmitting}
                helperText="A descriptive name for this API key"
              />
            </Grid>

            {!isNewCredential && (
              <Grid item xs={12} md={6}>
                <SwitchFormField
                  name="isDefault"
                  control={control}
                  switchLabel="Set as Default"
                  error={errors.isDefault}
                  disabled={isSubmitting || (credential?.isDefault ?? false)}
                  helperText="Make this the default API key for this provider"
                />
              </Grid>
            )}

            <Grid item xs={12}>
              <Divider sx={{ my: 2 }} />
              <Typography variant="h6" gutterBottom>
                API Credentials
              </Typography>

              {!selectedProvider && watchProviderId === '' && (
                <Alert severity="info" sx={{ mb: 2 }}>
                  Please select a provider to see the required credentials.
                </Alert>
              )}

              {selectedProvider && (
                <>
                  <Typography variant="body2" color="text.secondary" paragraph>
                    Enter the credentials for {selectedProvider.displayName || selectedProvider.name}
                  </Typography>

                  {/* API Key field (common for most providers) */}
                  <Grid container spacing={3}>
                    <Grid item xs={12}>
                      <TextFormField
                        name="apiKey"
                        control={control}
                        label="API Key"
                        type="password"
                        required={isNewCredential}
                        disabled={isSubmitting}
                        helperText={isNewCredential
                          ? "Enter the provider's API key"
                          : "Leave blank to keep the current API key"}
                      />
                    </Grid>

                    {/* Organization ID field (for OpenAI) */}
                    {selectedProvider.name === 'OpenAI' && (
                      <Grid item xs={12}>
                        <TextFormField
                          name="organizationId"
                          control={control}
                          label="Organization ID (Optional)"
                          disabled={isSubmitting}
                          helperText="OpenAI organization ID (if applicable)"
                        />
                      </Grid>
                    )}

                    {/* Add other provider-specific fields based on schema */}
                    {Object.entries(credentialFields).map(([key, field]) => {
                      // Skip fields we've already handled
                      if (key === 'apiKey' || key === 'organization') return null;

                      return (
                        <Grid item xs={12} key={key}>
                          <TextFormField
                            name={key as any}
                            control={control}
                            label={field.description || key}
                            required={field.required}
                            disabled={isSubmitting}
                          />
                        </Grid>
                      );
                    })}
                  </Grid>
                </>
              )}
            </Grid>
          </Grid>

          <Box sx={{ mt: 3, display: 'flex', justifyContent: 'flex-end' }}>
            <Button
              type="submit"
              variant="contained"
              color="primary"
              startIcon={<SaveIcon />}
              disabled={isSubmitting || (!isDirty && !isNewCredential)}
            >
              {isSubmitting ? 'Saving...' : isNewCredential ? 'Create API Key' : 'Save Changes'}
            </Button>
          </Box>
        </form>
      </Paper>
    </Box>
  );
};

export default CredentialDetailsPage;
