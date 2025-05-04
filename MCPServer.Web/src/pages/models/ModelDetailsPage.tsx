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
  Divider,
  Tab,
  Tabs,
  CircularProgress
} from '@mui/material';
import {
  Save as SaveIcon,
  ArrowBack as ArrowBackIcon
} from '@mui/icons-material';
import { 
  PageHeader, 
  TextFormField, 
  SwitchFormField, 
  SelectFormField 
} from '@/components';
import { llmProviderApi, LlmModel } from '@/api';
import { useErrorHandler } from '@/hooks';
import { useNotification } from '@/contexts/NotificationContext';
import { validationUtils } from '@/utils';
import { useAuth } from '@/contexts/AuthContext';

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
      id={`model-tabpanel-${index}`}
      aria-labelledby={`model-tab-${index}`}
      {...other}
    >
      {value === index && <Box sx={{ p: 3 }}>{children}</Box>}
    </div>
  );
};

const ModelDetailsPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { addNotification } = useNotification();
  const { handleError } = useErrorHandler();
  const { user } = useAuth();
  
  const isAdmin = user?.roles.includes('Admin');
  const isNewModel = id === 'new';
  const [tabValue, setTabValue] = useState(0);
  
  // Get providerId from URL if creating a new model
  const preselectedProviderId = searchParams.get('providerId') 
    ? parseInt(searchParams.get('providerId') as string) 
    : undefined;
  
  // Fetch model details if editing
  const {
    data: model,
    isLoading: isLoadingModel,
    isError
  } = useQuery({
    queryKey: ['model', id],
    queryFn: () => llmProviderApi.getModelById(Number(id)),
    enabled: !isNewModel && !!id
  });
  
  // Fetch providers for dropdown
  const { data: providersData = [] } = useQuery({
    queryKey: ['providers'],
    queryFn: llmProviderApi.getAllProviders
  });
  
  // Ensure providers is always an array
  const providers = Array.isArray(providersData) ? providersData : [];
  
  // Form setup
  const {
    control,
    handleSubmit,
    reset,
    watch,
    setValue,
    formState: { errors, isDirty, isSubmitting }
  } = useForm<LlmModel>({
    resolver: zodResolver(validationUtils.modelSchema),
    defaultValues: isNewModel
      ? {
          providerId: preselectedProviderId || 0,
          name: '',
          modelId: '',
          description: '',
          maxTokens: 2000,
          contextWindow: 8000,
          supportsStreaming: true,
          supportsVision: false,
          supportsTools: false,
          costPer1KInputTokens: 0,
          costPer1KOutputTokens: 0,
          isEnabled: true
        }
      : undefined
  });
  
  // Set form values when model data is loaded
  useEffect(() => {
    if (model && !isNewModel) {
      reset(model);
    }
  }, [model, reset, isNewModel]);
  
  // Set providerId from URL if creating a new model
  useEffect(() => {
    if (isNewModel && preselectedProviderId) {
      setValue('providerId', preselectedProviderId);
    }
  }, [isNewModel, preselectedProviderId, setValue]);
  
  // Create model mutation
  const createModelMutation = useMutation({
    mutationFn: llmProviderApi.createModel,
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ['models'] });
      addNotification('Model created successfully', 'success');
      navigate(`/models/${data.id}`);
    },
    onError: (error) => {
      handleError(error, 'Failed to create model');
    }
  });
  
  // Update model mutation
  const updateModelMutation = useMutation({
    mutationFn: (model: LlmModel) => 
      llmProviderApi.updateModel(model.id, model),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['models'] });
      queryClient.invalidateQueries({ queryKey: ['model', id] });
      addNotification('Model updated successfully', 'success');
    },
    onError: (error) => {
      handleError(error, 'Failed to update model');
    }
  });
  
  // Handle form submission
  const onSubmit = async (data: LlmModel) => {
    if (isNewModel) {
      createModelMutation.mutate(data);
    } else {
      updateModelMutation.mutate(data);
    }
  };
  
  // Handle tab change
  const handleTabChange = (_: React.SyntheticEvent, newValue: number) => {
    setTabValue(newValue);
  };
  
  // Watch providerId for display
  const watchProviderId = watch('providerId');
  const selectedProvider = providers.find(p => p.id === watchProviderId);
  
  // If error loading model
  if (isError && !isNewModel) {
    return (
      <Box>
        <PageHeader
          title="Model Not Found"
          breadcrumbs={[
            { label: 'Dashboard', path: '/dashboard' },
            { label: 'LLM Models', path: '/models' },
            { label: 'Not Found' }
          ]}
        />
        <Paper sx={{ p: 3, textAlign: 'center' }}>
          <Typography variant="h6" color="error" gutterBottom>
            Error: Model not found
          </Typography>
          <Button
            variant="contained"
            startIcon={<ArrowBackIcon />}
            onClick={() => navigate('/models')}
            sx={{ mt: 2 }}
          >
            Back to Models
          </Button>
        </Paper>
      </Box>
    );
  }
  
  // Loading state
  if (isLoadingModel && !isNewModel) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', mt: 4 }}>
        <CircularProgress />
      </Box>
    );
  }
  
  return (
    <Box>
      <PageHeader
        title={isNewModel ? 'Add New Model' : `Model: ${model?.name}`}
        subtitle={isNewModel ? 'Create a new LLM model' : 'View and edit model details'}
        breadcrumbs={[
          { label: 'Dashboard', path: '/dashboard' },
          { label: 'LLM Models', path: '/models' },
          { label: isNewModel ? 'New Model' : model?.name || '' }
        ]}
        action={
          <Button
            variant="outlined"
            startIcon={<ArrowBackIcon />}
            onClick={() => navigate('/models')}
          >
            Back to List
          </Button>
        }
      />
      
      <Paper sx={{ mb: 3 }}>
        <Tabs
          value={tabValue}
          onChange={handleTabChange}
          aria-label="model tabs"
          sx={{ borderBottom: 1, borderColor: 'divider' }}
        >
          <Tab label="Basic Information" />
          <Tab label="Capabilities" />
          <Tab label="Pricing" />
          {!isNewModel && <Tab label="Usage" />}
        </Tabs>
        
        <form onSubmit={handleSubmit(onSubmit)}>
          <TabPanel value={tabValue} index={0}>
            <Grid container spacing={3}>
              <Grid item xs={12} md={6}>
                <SelectFormField
                  name="providerId"
                  control={control}
                  label="Provider"
                  error={errors.providerId}
                  required
                  disabled={!isAdmin || isSubmitting || (!isNewModel && !!model)}
                  options={providers.map(provider => ({
                    value: provider.id,
                    label: provider.displayName || provider.name
                  }))}
                />
              </Grid>
              
              <Grid item xs={12} md={6}>
                <TextFormField
                  name="name"
                  control={control}
                  label="Model Name"
                  error={errors.name}
                  required
                  disabled={!isAdmin || isSubmitting}
                  helperText="User-friendly name to display in the UI"
                />
              </Grid>
              
              <Grid item xs={12} md={6}>
                <TextFormField
                  name="modelId"
                  control={control}
                  label="Model ID"
                  error={errors.modelId}
                  required
                  disabled={!isAdmin || isSubmitting}
                  helperText={`ID used by the provider (e.g., "gpt-4" for ${selectedProvider?.name || 'the provider'})`}
                />
              </Grid>
              
              <Grid item xs={12} md={6}>
                <SwitchFormField
                  name="isEnabled"
                  control={control}
                  switchLabel="Model Enabled"
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
            <Grid container spacing={3}>
              <Grid item xs={12} md={6}>
                <TextFormField
                  name="maxTokens"
                  control={control}
                  label="Max Tokens"
                  type="number"
                  error={errors.maxTokens}
                  required
                  disabled={!isAdmin || isSubmitting}
                  helperText="Maximum number of tokens that can be generated in a single response"
                />
              </Grid>
              
              <Grid item xs={12} md={6}>
                <TextFormField
                  name="contextWindow"
                  control={control}
                  label="Context Window"
                  type="number"
                  error={errors.contextWindow}
                  required
                  disabled={!isAdmin || isSubmitting}
                  helperText="Maximum number of tokens that can be processed in a single request"
                />
              </Grid>
              
              <Grid item xs={12} md={4}>
                <SwitchFormField
                  name="supportsStreaming"
                  control={control}
                  switchLabel="Supports Streaming"
                  error={errors.supportsStreaming}
                  disabled={!isAdmin || isSubmitting}
                  helperText="Can generate responses incrementally"
                />
              </Grid>
              
              <Grid item xs={12} md={4}>
                <SwitchFormField
                  name="supportsVision"
                  control={control}
                  switchLabel="Supports Vision"
                  error={errors.supportsVision}
                  disabled={!isAdmin || isSubmitting}
                  helperText="Can process image inputs"
                />
              </Grid>
              
              <Grid item xs={12} md={4}>
                <SwitchFormField
                  name="supportsTools"
                  control={control}
                  switchLabel="Supports Tools"
                  error={errors.supportsTools}
                  disabled={!isAdmin || isSubmitting}
                  helperText="Can use function calling and tools"
                />
              </Grid>
            </Grid>
          </TabPanel>
          
          <TabPanel value={tabValue} index={2}>
            <Grid container spacing={3}>
              <Grid item xs={12} md={6}>
                <TextFormField
                  name="costPer1KInputTokens"
                  control={control}
                  label="Cost per 1K Input Tokens ($)"
                  type="number"
                  error={errors.costPer1KInputTokens}
                  required
                  disabled={!isAdmin || isSubmitting}
                  helperText="Cost in USD for processing 1,000 input tokens"
                />
              </Grid>
              
              <Grid item xs={12} md={6}>
                <TextFormField
                  name="costPer1KOutputTokens"
                  control={control}
                  label="Cost per 1K Output Tokens ($)"
                  type="number"
                  error={errors.costPer1KOutputTokens}
                  required
                  disabled={!isAdmin || isSubmitting}
                  helperText="Cost in USD for generating 1,000 output tokens"
                />
              </Grid>
            </Grid>
          </TabPanel>
          
          {!isNewModel && (
            <TabPanel value={tabValue} index={3}>
              <Typography variant="h6" gutterBottom>
                Usage Statistics
              </Typography>
              <Typography variant="body2" color="text.secondary" paragraph>
                This section will display usage statistics for this model.
              </Typography>
              
              {/* Usage stats will be implemented separately */}
              <Box sx={{ mt: 2, p: 2, bgcolor: 'action.hover', borderRadius: 1 }}>
                <Typography variant="body2" color="text.secondary" align="center">
                  Usage statistics will be displayed here
                </Typography>
              </Box>
            </TabPanel>
          )}
          
          {isAdmin && (tabValue <= 2) && (
            <Box sx={{ p: 3, pt: 0, display: 'flex', justifyContent: 'flex-end' }}>
              <Button
                type="submit"
                variant="contained"
                color="primary"
                startIcon={<SaveIcon />}
                disabled={isSubmitting || (!isDirty && !isNewModel)}
              >
                {isSubmitting ? 'Saving...' : isNewModel ? 'Create Model' : 'Save Changes'}
              </Button>
            </Box>
          )}
        </form>
      </Paper>
    </Box>
  );
};

export default ModelDetailsPage;
