import React from 'react';
import { Box } from '@mui/material';
import { LocalizationProvider } from '@mui/x-date-pickers';
import { AdapterDateFns } from '@mui/x-date-pickers/AdapterDateFns';
import { PageHeader } from '@/components';
import { UsageContextProvider } from './components/UsageContext';
import UsageFilters from './components/UsageFilters';
import UsageSummaryCards from './components/UsageSummaryCards';
import UsageTabs from './components/UsageTabs';

const UsageStatsPage: React.FC = () => {
  return (
    <LocalizationProvider dateAdapter={AdapterDateFns}>
      <UsageContextProvider>
        <Box>
          <PageHeader
            title="Usage Statistics"
            subtitle="Monitor your LLM and Chat usage and costs"
            breadcrumbs={[
              { label: 'Dashboard', path: '/dashboard' },
              { label: 'Usage Statistics' }
            ]}
          />
          <UsageFilters />
          <UsageSummaryCards />
          <UsageTabs />
        </Box>
      </UsageContextProvider>
    </LocalizationProvider>
  );
};

export default UsageStatsPage;
