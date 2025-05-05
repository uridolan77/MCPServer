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
  
  // Calculate summary metrics from filtered data
  const summaryMetrics = useMemo(() => {
    console.log('Raw data:', {
      chatUsageLogs,
      filteredChatLogs
    });
    
    // Directly use chat logs if available
    if (Array.isArray(chatUsageLogs) && chatUsageLogs.length > 0) {
      // Use chat logs array for totals
      const totalRequests = chatUsageLogs.length;
      
      // Directly expose the values without any calculation
      // Just sum the raw values from the API
      let totalTokens = 0;
      let totalCost = 0;
      let successfulRequests = 0;
      
      chatUsageLogs.forEach(log => {
        // Use the raw values directly
        if (log.inputTokenCount) totalTokens += log.inputTokenCount;
        if (log.outputTokenCount) totalTokens += log.outputTokenCount;
        if (log.estimatedCost) totalCost += log.estimatedCost;
        if (log.success) successfulRequests++;
      });
      
      const successRate = (totalRequests > 0) ? (successfulRequests / totalRequests) * 100 : 0;
      
      return {
        totalRequests,
        totalTokens,
        totalCost,
        successRate
      };
    }
    
    // Default to zero values if no data
    return {
      totalRequests: 0,
      totalTokens: 0,
      totalCost: 0,
      successRate: 0
    };
  }, [chatUsageLogs, filteredChatLogs]);
  
  // Configuration for summary cards - depends on which tab is active
  const summaryCards = useMemo(() => {
    // Default cards that just show raw values
    return [
      {
        title: 'Total Messages',
        value: summaryMetrics.totalRequests,
        icon: <ChatIcon />,
        color: theme.palette.primary.main
      },
      {
        title: 'Total Tokens',
        value: summaryMetrics.totalTokens,
        icon: <ListIcon />,
        color: theme.palette.secondary.main
      },
      {
        title: 'Estimated Cost',
        value: `$${summaryMetrics.totalCost}`,
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
  }, [summaryMetrics, theme.palette]);
  
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