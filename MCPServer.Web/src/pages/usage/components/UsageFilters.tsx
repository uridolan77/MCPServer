import React from 'react';
import { 
  Box, 
  Card, 
  CardContent, 
  Grid, 
  Typography, 
  IconButton,
  useTheme
} from '@mui/material';
import { DatePicker } from '@mui/x-date-pickers';
import { Refresh as RefreshIcon } from '@mui/icons-material';
import { useQueryClient } from '@tanstack/react-query';
import { useUsageContext } from './UsageContext';
import { ProviderSelector, ModelSelector } from '@/components/selectors';

const UsageFilters: React.FC = () => {
  const theme = useTheme();
  const queryClient = useQueryClient();
  
  const {
    startDate,
    setStartDate,
    endDate,
    setEndDate,
    selectedModelId,
    setSelectedModelId,
    selectedProviderId,
    setSelectedProviderId,
    setModels,
    setProviders
  } = useUsageContext();

  // Handle model filter change
  const handleModelChange = (value: string | number) => {
    setSelectedModelId(value);
  };

  // Handle provider filter change
  const handleProviderChange = (value: string | number) => {
    setSelectedProviderId(value);
  };

  // Handle refresh button click
  const handleRefresh = () => {
    // Invalidate and refetch the providers and models queries
    queryClient.invalidateQueries({ queryKey: ['providers'] });
    queryClient.invalidateQueries({ queryKey: ['models'] });
  };

  return (
    <Card sx={{ mb: 3 }}>
      <CardContent>
        <Box display="flex" justifyContent="space-between" alignItems="center" mb={2}>
          <Typography variant="h6">Filter Usage Data</Typography>
          <IconButton onClick={handleRefresh} size="small" title="Refresh data">
            <RefreshIcon />
          </IconButton>
        </Box>
        
        <Grid container spacing={2}>
          <Grid item xs={12} sm={6} md={3}>
            <DatePicker
              label="Start Date"
              value={startDate}
              onChange={(newValue) => setStartDate(newValue)}
              slotProps={{ 
                textField: { 
                  fullWidth: true,
                  size: "small"
                } 
              }}
            />
          </Grid>
          
          <Grid item xs={12} sm={6} md={3}>
            <DatePicker
              label="End Date"
              value={endDate}
              onChange={(newValue) => setEndDate(newValue)}
              slotProps={{ 
                textField: { 
                  fullWidth: true,
                  size: "small"
                } 
              }}
            />
          </Grid>
          
          <Grid item xs={12} sm={6} md={3}>
            <ModelSelector
              value={selectedModelId}
              onChange={handleModelChange}
              providerId={selectedProviderId}
              fullWidth
              size="small"
              label="Model"
              allOptionLabel="All Models"
            />
          </Grid>
          
          <Grid item xs={12} sm={6} md={3}>
            <ProviderSelector
              value={selectedProviderId}
              onChange={handleProviderChange}
              fullWidth
              size="small"
              label="Provider"
              allOptionLabel="All Providers"
            />
          </Grid>
        </Grid>
      </CardContent>
    </Card>
  );
};

export default UsageFilters;