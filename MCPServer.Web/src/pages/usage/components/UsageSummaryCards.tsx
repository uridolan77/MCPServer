import React, { useMemo } from 'react';
import { 
  Box, 
  Card, 
  CardContent, 
  Grid, 
  Typography, 
  Stack, 
  Skeleton,
  useTheme 
} from '@mui/material';
import { 
  Analytics as AnalyticsIcon,
  AttachMoney as MoneyIcon,
  FormatListNumbered as ListIcon,
  Timeline as TimelineIcon,
  QuestionAnswer as ChatIcon,
  Timer as TimerIcon,
  Message as MessageIcon,
  Speed as SpeedIcon
} from '@mui/icons-material';
import { useUsageContext } from './UsageContext';
import { TAB_LOGS_VIEW, TAB_SESSIONS } from './UsageTabs';

const UsageSummaryCards: React.FC = () => {
  const theme = useTheme();
  const { 
    filteredUsageLogs,
    filteredChatLogs,
    chatUsageLogs,
    overallStats, 
    dashboardStats,
    isLoading,
    activeTabIndex
  } = useUsageContext();
  
  // Calculate chat-specific metrics from filtered chat logs
  const chatMetrics = useMemo(() => {
    if (!Array.isArray(filteredChatLogs) || filteredChatLogs.length === 0) {
      return {
        totalMessages: 0,
        avgResponseLength: 0,
        avgResponseTime: 0,
        topModels: []
      };
    }

    const totalMessages = filteredChatLogs.length;
    
    // Calculate average response length (in characters)
    const totalResponseLength = filteredChatLogs.reduce(
      (sum, log) => sum + (log.response?.length || 0), 
      0
    );
    const avgResponseLength = totalResponseLength / totalMessages;
    
    // Calculate average response time
    const totalResponseTime = filteredChatLogs.reduce(
      (sum, log) => sum + (log.duration || 0), 
      0
    );
    const avgResponseTime = totalResponseTime / totalMessages;
    
    // Get model usage counts
    const modelCounts = filteredChatLogs.reduce((acc, log) => {
      const modelName = log.modelName || 'Unknown';
      acc[modelName] = (acc[modelName] || 0) + 1;
      return acc;
    }, {} as Record<string, number>);
    
    // Convert to array and sort
    const topModels = Object.entries(modelCounts)
      .map(([name, count]) => ({ name, count }))
      .sort((a, b) => b.count - a.count)
      .slice(0, 3);
      
    return {
      totalMessages,
      avgResponseLength,
      avgResponseTime,
      topModels
    };
  }, [filteredChatLogs]);
  
  // Calculate summary metrics from filtered data
  const summaryMetrics = useMemo(() => {
    // Use the original data arrays if they exist, as filteredLogs might be filtered by search
    // Always prioritize chat logs if available
    if (Array.isArray(chatUsageLogs) && chatUsageLogs.length > 0) {
      // Use the full chatUsageLogs array to calculate totals, not just filtered ones
      const logsToCalculate = activeTabIndex === TAB_LOGS_VIEW && filteredChatLogs?.length > 0 ? 
        filteredChatLogs : chatUsageLogs;
      
      const totalRequests = logsToCalculate.length;
      
      // Calculate total tokens
      const totalTokens = logsToCalculate.reduce(
        (sum, log) => sum + ((log.inputTokenCount || 0) + (log.outputTokenCount || 0)), 
        0
      );
      
      // Calculate total cost
      const totalCost = logsToCalculate.reduce(
        (sum, log) => sum + (log.estimatedCost || 0), 
        0
      );
      
      // Calculate success rate
      const successRate = calculateSuccessRate(logsToCalculate);
      
      return {
        totalRequests,
        totalTokens,
        totalCost,
        successRate
      };
    } 
    else if (activeTabIndex === TAB_LOGS_VIEW && Array.isArray(filteredUsageLogs) && filteredUsageLogs.length > 0) {
      const totalRequests = filteredUsageLogs.length;
      
      // Calculate tokens with appropriate fallbacks for various field names
      const totalTokens = filteredUsageLogs.reduce(
        (sum, log) => {
          if (log.totalTokens !== undefined) {
            return sum + (log.totalTokens || 0);
          } else {
            // If totalTokens isn't available, try to calculate from input + output
            return sum + ((log.inputTokens || 0) + (log.outputTokens || 0));
          }
        }, 
        0
      );
      
      // Calculate cost
      const totalCost = filteredUsageLogs.reduce(
        (sum, log) => sum + (log.estimatedCost || 0), 
        0
      );
      
      // Calculate success rate
      const successfulRequests = filteredUsageLogs.filter(
        log => log.status === 'Success' || log.status === 'Succeeded' || log.success === true
      ).length;
      
      const successRate = totalRequests > 0 ? (successfulRequests / totalRequests) * 100 : 0;
      
      return {
        totalRequests,
        totalTokens,
        totalCost,
        successRate
      };
    }
    
    // If no filtered data is available, but we have dashboard stats
    if (dashboardStats) {
      return {
        totalRequests: dashboardStats.totalMessages,
        totalTokens: dashboardStats.totalTokens,
        totalCost: dashboardStats.totalCost,
        successRate: dashboardStats.successRate
      };
    }
    
    // Fall back to overall stats
    if (overallStats) {
      return {
        totalRequests: overallStats.totalMessages,
        totalTokens: overallStats.totalTokensUsed,
        totalCost: overallStats.totalCost,
        successRate: 0 // Can't calculate success rate from overall stats
      };
    }
    
    // Default to zero values if no data
    return {
      totalRequests: 0,
      totalTokens: 0,
      totalCost: 0,
      successRate: 0
    };
  }, [filteredUsageLogs, filteredChatLogs, chatUsageLogs, overallStats, dashboardStats, activeTabIndex]);
  
  // Helper function to calculate success rate from chat logs
  function calculateSuccessRate(logs: any[]) {
    if (!Array.isArray(logs) || logs.length === 0) return 0;
    
    const successfulLogs = logs.filter(log => 
      log.success === true || log.status === 'Success' || log.status === 'Succeeded'
    ).length;
    
    return (successfulLogs / logs.length) * 100;
  }
  
  // Configuration for summary cards - depends on which tab is active
  const summaryCards = useMemo(() => {
    // Show chat-specific cards when on the Logs View tab
    if (activeTabIndex === TAB_LOGS_VIEW) {
      return [
        {
          title: 'Total Messages',
          value: summaryMetrics.totalRequests.toLocaleString(),
          icon: <ChatIcon />,
          color: theme.palette.primary.main
        },
        {
          title: 'Total Tokens',
          value: summaryMetrics.totalTokens.toLocaleString(),
          icon: <ListIcon />,
          color: theme.palette.secondary.main
        },
        {
          title: 'Estimated Cost',
          value: `$${summaryMetrics.totalCost.toFixed(2)}`,
          icon: <MoneyIcon />,
          color: theme.palette.success.main
        },
        {
          title: 'Success Rate',
          value: `${summaryMetrics.successRate.toFixed(1)}%`,
          icon: <SpeedIcon />,
          color: theme.palette.info.main
        }
      ];
    }
  
    // Default cards for other tabs
    return [
      {
        title: 'Total Requests',
        value: summaryMetrics.totalRequests.toLocaleString(),
        icon: <AnalyticsIcon />,
        color: theme.palette.primary.main
      },
      {
        title: 'Total Tokens',
        value: summaryMetrics.totalTokens.toLocaleString(),
        icon: <ListIcon />,
        color: theme.palette.secondary.main
      },
      {
        title: 'Estimated Cost',
        value: `$${summaryMetrics.totalCost.toFixed(2)}`,
        icon: <MoneyIcon />,
        color: theme.palette.success.main
      },
      {
        title: 'Success Rate',
        value: `${summaryMetrics.successRate.toFixed(1)}%`,
        icon: <TimelineIcon />,
        color: theme.palette.info.main
      }
    ];
  }, [activeTabIndex, summaryMetrics, theme.palette]);
  
  return (
    <Grid container spacing={3} sx={{ mb: 3 }}>
      {summaryCards.map((card, index) => (
        <Grid item xs={12} sm={6} md={3} key={index}>
          <Card sx={{ height: '100%' }}>
            <CardContent>
              <Stack direction="row" spacing={2} alignItems="center">
                <Box
                  sx={{
                    borderRadius: '50%',
                    backgroundColor: `${card.color}20`,
                    p: 1,
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                  }}
                >
                  <Box sx={{ color: card.color }}>{card.icon}</Box>
                </Box>
                <Box>
                  <Typography variant="body2" color="text.secondary">
                    {card.title}
                  </Typography>
                  {isLoading ? (
                    <Skeleton width={80} height={32} />
                  ) : (
                    <Typography variant="h5">{card.value}</Typography>
                  )}
                </Box>
              </Stack>
            </CardContent>
          </Card>
        </Grid>
      ))}
    </Grid>
  );
};

export default UsageSummaryCards;