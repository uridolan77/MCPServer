import React, { useEffect } from 'react';
import { Box, Container, Typography, Paper, Stack } from '@mui/material';
import { LocalizationProvider } from '@mui/x-date-pickers';
import { AdapterDateFns } from '@mui/x-date-pickers/AdapterDateFns';
import { PageHeader } from '@/components';
import { UsageContextProvider, useUsageContext } from './components/UsageContext';
import UsageDateFilter from './components/UsageDateFilter';
import UsageFilters from './components/UsageFilters';
import UsageSummaryCards from './components/UsageSummaryCards';
import UsageTabs from './components/UsageTabs';
import { analyticsApi } from '@/api/analyticsApi';
import { format } from 'date-fns';
import axios from 'axios';
import { API_BASE_URL } from '@/config';

const UsageStatsPage: React.FC = () => {
  const { startDate, endDate } = useUsageContext();

  // Debug: Make a direct API call to check the response
  useEffect(() => {
    const fetchData = async () => {
      try {
        // Format dates for API call
        const formattedStartDate = startDate ? format(startDate, "yyyy-MM-dd'T'HH:mm:ss.SSS'Z'") : undefined;
        const formattedEndDate = endDate ? format(endDate, "yyyy-MM-dd'T'HH:mm:ss.SSS'Z'") : undefined;
        
        console.log('Making direct API call to fetch chat logs with dates:', { formattedStartDate, formattedEndDate });
        
        // Make a raw axios call to see the exact response structure
        const url = `${API_BASE_URL}/chat-usage/logs`;
        const params = new URLSearchParams();
        if (formattedStartDate) params.append('startDate', formattedStartDate);
        if (formattedEndDate) params.append('endDate', formattedEndDate);
        
        console.log(`Making raw axios call to ${url}`);
        const rawResponse = await axios.get(url, { params });
        console.log('Raw API response:', rawResponse.data);
        
        if (rawResponse.data && Array.isArray(rawResponse.data)) {
          console.log('Raw response is an array with', rawResponse.data.length, 'items');
          if (rawResponse.data.length > 0) {
            console.log('First item in raw response:', rawResponse.data[0]);
            console.log('Token values:',
              'inputTokenCount =', rawResponse.data[0].inputTokenCount,
              'outputTokenCount =', rawResponse.data[0].outputTokenCount,
              'types:', 
              'inputTokenCount is', typeof rawResponse.data[0].inputTokenCount,
              'outputTokenCount is', typeof rawResponse.data[0].outputTokenCount
            );
          }
        } else if (rawResponse.data && rawResponse.data.$values) {
          console.log('Raw response has $values array with', rawResponse.data.$values.length, 'items');
          if (rawResponse.data.$values.length > 0) {
            console.log('First item in raw $values response:', rawResponse.data.$values[0]);
            console.log('Token values:',
              'inputTokenCount =', rawResponse.data.$values[0].inputTokenCount,
              'outputTokenCount =', rawResponse.data.$values[0].outputTokenCount,
              'types:', 
              'inputTokenCount is', typeof rawResponse.data.$values[0].inputTokenCount,
              'outputTokenCount is', typeof rawResponse.data.$values[0].outputTokenCount
            );
          }
        }
        
        // Use the configured API client as well
        const response = await analyticsApi.getChatUsageLogs(
          formattedStartDate,
          formattedEndDate,
          undefined, // modelId
          undefined, // providerId
          undefined, // sessionId
          1, // page
          50 // pageSize
        );
        
        console.log('API client response:', response);
        console.log('Number of logs returned:', response?.length || 0);
        
        if (Array.isArray(response) && response.length > 0) {
          console.log('First log sample from API client:', response[0]);
          console.log('Token values from API client:',
            'inputTokenCount =', response[0].inputTokenCount,
            'outputTokenCount =', response[0].outputTokenCount,
            'types:', 
            'inputTokenCount is', typeof response[0].inputTokenCount,
            'outputTokenCount is', typeof response[0].outputTokenCount
          );
          
          // Check for potential type conversion issues
          console.log('JSON stringify/parse test:');
          const serialized = JSON.stringify(response[0]);
          const deserialized = JSON.parse(serialized);
          console.log('After JSON roundtrip:',
            'inputTokenCount =', deserialized.inputTokenCount,
            'outputTokenCount =', deserialized.outputTokenCount,
            'types:', 
            'inputTokenCount is', typeof deserialized.inputTokenCount,
            'outputTokenCount is', typeof deserialized.outputTokenCount
          );
        }
      } catch (error) {
        console.error('Error in direct API call:', error);
      }
    };
    
    fetchData();
  }, [startDate, endDate]);

  return (
    <LocalizationProvider dateAdapter={AdapterDateFns}>
      <UsageContextProvider>
        <Container maxWidth={false}>
          <Box sx={{ mb: 3 }}>
            <Typography variant="h4">Usage Statistics</Typography>
          </Box>

          <Paper sx={{ p: 3, mb: 3 }}>
            <Stack direction={{ xs: 'column', md: 'row' }} spacing={2} justifyContent="space-between" alignItems="flex-end">
              <UsageFilters />
              <UsageDateFilter />
            </Stack>
          </Paper>

          <UsageSummaryCards />
          
          <UsageTabs />
        </Container>
      </UsageContextProvider>
    </LocalizationProvider>
  );
};

export default UsageStatsPage;
