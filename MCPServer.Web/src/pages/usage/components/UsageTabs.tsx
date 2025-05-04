import React, { useState, useEffect } from 'react';
import { 
  Box, 
  Card, 
  Tabs, 
  Tab 
} from '@mui/material';
import { useQuery } from '@tanstack/react-query';
import { format } from 'date-fns';
import { llmProviderApi } from '@/api/llmProviderApi';
import { analyticsApi } from '@/api/analyticsApi';
import { useErrorHandler } from '@/hooks';
import { useUsageContext } from './UsageContext';
import UsageChart from './UsageChart';
import CombinedLogsView from './CombinedLogsView';
import SessionsView from './SessionsView';

interface TabPanelProps {
  children?: React.ReactNode;
  index: number;
  value: number;
}

// Tab Panel component
const TabPanel = (props: TabPanelProps) => {
  const { children, value, index, ...other } = props;

  return (
    <div
      role="tabpanel"
      hidden={value !== index}
      id={`usage-tabpanel-${index}`}
      aria-labelledby={`usage-tab-${index}`}
      {...other}
    >
      {value === index && <Box sx={{ pt: 3 }}>{children}</Box>}
    </div>
  );
};

// Define tab indices as constants
export const TAB_CHART_VIEW = 0;
export const TAB_LOGS_VIEW = 1;  // Renamed from TAB_TABLE_VIEW
export const TAB_SESSIONS = 2;    // Adjusted index

const UsageTabs: React.FC = () => {
  const [tabValue, setTabValue] = useState(0);
  const { handleError } = useErrorHandler();

  // Get context from hook
  const { 
    startDate, 
    endDate, 
    selectedModelId,
    selectedProviderId,
    setUsageLogs,
    setChatUsageLogs,
    setFilteredUsageLogs,
    setFilteredChatLogs,
    setOverallStats,
    setDashboardStats,
    setIsLoading,
    setActiveTabIndex, // New setter for active tab
    refreshData: contextRefreshData // Renamed to avoid conflicts
  } = useUsageContext();

  // Update the active tab in context when tab changes
  useEffect(() => {
    if (setActiveTabIndex) {
      setActiveTabIndex(tabValue);
    }
  }, [tabValue, setActiveTabIndex]);

  // Function to format dates for API calls
  const formatDateForApi = (date: Date | null) => {
    return date ? format(date, "yyyy-MM-dd'T'HH:mm:ss.SSS'Z'") : undefined;
  };

  // Fetch chat usage logs with larger page size
  const { data: chatLogs = [], isLoading: isLoadingChatLogs, refetch: refetchChatLogs } = useQuery({
    queryKey: ['chat-usage-logs', selectedModelId, selectedProviderId, startDate, endDate],
    queryFn: async () => {
      try {
        setIsLoading(true);
        
        return await analyticsApi.getChatUsageLogs(
          formatDateForApi(startDate),
          formatDateForApi(endDate),
          selectedModelId !== '' ? Number(selectedModelId) : undefined,
          selectedProviderId !== '' ? Number(selectedProviderId) : undefined,
          undefined, // sessionId
          1,        // page
          200       // increased pageSize to get more results at once
        );
      } catch (error) {
        console.error('Error fetching chat usage logs:', error);
        handleError(error, 'Failed to fetch chat usage data');
        return [];
      } finally {
        setIsLoading(false);
      }
    }
  });

  // Fetch overall stats
  const { data: overallStatsData, isLoading: isLoadingStats, refetch: refetchStats } = useQuery({
    queryKey: ['overall-stats'],
    queryFn: async () => {
      try {
        return await analyticsApi.getOverallStats();
      } catch (error) {
        console.error('Error fetching overall stats:', error);
        handleError(error, 'Failed to fetch overall statistics');
        return null;
      }
    }
  });

  // Fetch dashboard stats
  const { data: dashboardStatsData, isLoading: isLoadingDashboardStats, refetch: refetchDashboard } = useQuery({
    queryKey: ['dashboard-stats', startDate, endDate, selectedModelId, selectedProviderId],
    queryFn: async () => {
      try {
        // Include model and provider filters in dashboard stats
        return await analyticsApi.getDashboardStats(
          formatDateForApi(startDate),
          formatDateForApi(endDate),
          selectedModelId !== '' ? Number(selectedModelId) : undefined,
          selectedProviderId !== '' ? Number(selectedProviderId) : undefined
        );
      } catch (error) {
        console.error('Error fetching dashboard stats:', error);
        handleError(error, 'Failed to fetch dashboard statistics');
        return null;
      }
    }
  });

  // Fetch LLM usage data
  const { data: llmUsageLogs = [], isLoading: isLoadingLlm, refetch: refetchLlmUsage } = useQuery({
    queryKey: ['llm-usage', startDate, endDate, selectedModelId, selectedProviderId],
    queryFn: async () => {
      try {
        setIsLoading(true);
        return await llmProviderApi.getUserUsage(
          formatDateForApi(startDate),
          formatDateForApi(endDate),
          selectedModelId !== '' ? Number(selectedModelId) : undefined,
          selectedProviderId !== '' ? Number(selectedProviderId) : undefined
        );
      } catch (error) {
        console.error('Error fetching LLM usage:', error);
        handleError(error, 'Failed to fetch usage data');
        return [];
      } finally {
        setIsLoading(false);
      }
    },
  });

  // Function to refresh all data
  const refreshAllData = () => {
    refetchChatLogs();
    refetchStats();
    refetchDashboard();
    refetchLlmUsage();
  };

  // Update context with fetched data and apply filtering
  useEffect(() => {
    if (Array.isArray(llmUsageLogs)) {
      setUsageLogs(llmUsageLogs);
      
      // Filter logs based on selected model and provider
      // (This is a backup in case the API doesn't support filtering)
      const filtered = llmUsageLogs.filter(log => {
        const matchesModel = !selectedModelId || log.modelId === Number(selectedModelId);
        const matchesProvider = !selectedProviderId || log.providerId === Number(selectedProviderId);
        return matchesModel && matchesProvider;
      });
      
      setFilteredUsageLogs(filtered);
      
      // Log filtering details for debugging
      console.log(`Filtered LLM logs: ${filtered.length} of ${llmUsageLogs.length} logs match filters.`,
        { modelFilter: selectedModelId, providerFilter: selectedProviderId });
    }
  }, [llmUsageLogs, selectedModelId, selectedProviderId, setUsageLogs, setFilteredUsageLogs]);

  // Update context with chat logs and apply filtering
  useEffect(() => {
    if (Array.isArray(chatLogs)) {
      setChatUsageLogs(chatLogs);
      
      // Filter chat logs based on selected model and provider
      // (This is a backup in case the API doesn't support filtering)
      const filtered = chatLogs.filter(log => {
        const matchesModel = !selectedModelId || log.modelId === Number(selectedModelId);
        const matchesProvider = !selectedProviderId || log.providerId === Number(selectedProviderId);
        return matchesModel && matchesProvider;
      });
      
      setFilteredChatLogs(filtered);
      
      // Log filtering details for debugging
      console.log(`Filtered chat logs: ${filtered.length} of ${chatLogs.length} logs match filters.`,
        { modelFilter: selectedModelId, providerFilter: selectedProviderId });
    }
  }, [chatLogs, selectedModelId, selectedProviderId, setChatUsageLogs, setFilteredChatLogs]);

  // Update context with overall stats
  useEffect(() => {
    if (overallStatsData) {
      setOverallStats(overallStatsData);
    }
  }, [overallStatsData, setOverallStats]);

  // Update context with dashboard stats
  useEffect(() => {
    if (dashboardStatsData) {
      setDashboardStats(dashboardStatsData);
    }
  }, [dashboardStatsData, setDashboardStats]);

  // Set up refresh function in context
  useEffect(() => {
    // We need a way to call refreshAllData from the context
    // This is a workaround that doesn't break hook rules
    const originalRefresh = contextRefreshData;
    if (typeof originalRefresh === 'function' && originalRefresh !== refreshAllData) {
      // Only set up a global refresh function once
      window.__mcpRefreshUsageData = refreshAllData;
    }
  }, []);

  // Handle tab change
  const handleTabChange = (event: React.SyntheticEvent, newValue: number) => {
    setTabValue(newValue);
  };

  // Refresh data when filters change
  useEffect(() => {
    console.log("Filters changed, refreshing data:");
    console.log({ modelId: selectedModelId, providerId: selectedProviderId });
    refreshAllData();
  }, [selectedModelId, selectedProviderId]);

  return (
    <Card>
      <Box sx={{ borderBottom: 1, borderColor: 'divider' }}>
        <Tabs 
          value={tabValue} 
          onChange={handleTabChange} 
          aria-label="usage statistics tabs"
        >
          <Tab label="Chart View" id="usage-tab-0" aria-controls="usage-tabpanel-0" />
          <Tab label="Logs View" id="usage-tab-1" aria-controls="usage-tabpanel-1" />
          <Tab label="Sessions" id="usage-tab-2" aria-controls="usage-tabpanel-2" />
        </Tabs>
      </Box>
      
      <TabPanel value={tabValue} index={0}>
        <UsageChart />
      </TabPanel>
      
      <TabPanel value={tabValue} index={1}>
        <CombinedLogsView />
      </TabPanel>
      
      <TabPanel value={tabValue} index={2}>
        <SessionsView />
      </TabPanel>
    </Card>
  );
};

export default UsageTabs;