import React, { useMemo } from 'react';
import { 
  Box, 
  Card, 
  CardContent, 
  Typography, 
  useTheme, 
  ToggleButtonGroup, 
  ToggleButton 
} from '@mui/material';
import { useState } from 'react';
import {
  ResponsiveContainer,
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  LineChart,
  Line
} from 'recharts';
import { useUsageContext } from './UsageContext';

type ChartType = 'bar' | 'line';
type ChartMetric = 'requests' | 'tokens' | 'cost';

const UsageChart: React.FC = () => {
  const theme = useTheme();
  const { filteredUsageLogs, isLoading, selectedModelId, selectedProviderId } = useUsageContext();
  const [chartType, setChartType] = useState<ChartType>('bar');
  const [metric, setMetric] = useState<ChartMetric>('requests');

  // Transform and aggregate data for charts
  const chartData = useMemo(() => {
    if (!Array.isArray(filteredUsageLogs) || filteredUsageLogs.length === 0) {
      return [];
    }

    // Group logs by date
    const dateGroups: Record<string, {
      date: string;
      requests: number;
      tokens: number;
      cost: number;
    }> = {};

    filteredUsageLogs.forEach(log => {
      const date = new Date(log.requestTimestamp).toLocaleDateString();
      
      if (!dateGroups[date]) {
        dateGroups[date] = {
          date,
          requests: 0,
          tokens: 0,
          cost: 0
        };
      }
      
      dateGroups[date].requests += 1;
      dateGroups[date].tokens += (log.totalTokens || 0);
      dateGroups[date].cost += (log.estimatedCost || 0);
    });

    // Convert grouped data to array for chart
    return Object.values(dateGroups).sort((a, b) => 
      new Date(a.date).getTime() - new Date(b.date).getTime()
    );
  }, [filteredUsageLogs]);

  // Handle chart type change
  const handleChartTypeChange = (
    event: React.MouseEvent<HTMLElement>,
    newChartType: ChartType | null
  ) => {
    if (newChartType !== null) {
      setChartType(newChartType);
    }
  };

  // Handle metric change
  const handleMetricChange = (
    event: React.MouseEvent<HTMLElement>,
    newMetric: ChartMetric | null
  ) => {
    if (newMetric !== null) {
      setMetric(newMetric);
    }
  };

  // Get label and color based on selected metric
  const getMetricConfig = () => {
    switch (metric) {
      case 'requests':
        return {
          label: 'Number of Requests',
          color: theme.palette.primary.main
        };
      case 'tokens':
        return {
          label: 'Total Tokens',
          color: theme.palette.secondary.main
        };
      case 'cost':
        return {
          label: 'Estimated Cost ($)',
          color: theme.palette.success.main
        };
      default:
        return {
          label: 'Value',
          color: theme.palette.primary.main
        };
    }
  };

  const metricConfig = getMetricConfig();

  // Generate subtitle based on filters
  const chartSubtitle = useMemo(() => {
    const hasModelFilter = selectedModelId !== '';
    const hasProviderFilter = selectedProviderId !== '';
    
    if (!hasModelFilter && !hasProviderFilter) {
      return 'All Data';
    }
    
    let subtitle = '';
    if (hasProviderFilter) {
      subtitle += `Provider: ${selectedProviderId}`;
    }
    
    if (hasModelFilter) {
      if (subtitle) subtitle += ' | ';
      subtitle += `Model: ${selectedModelId}`;
    }
    
    return subtitle;
  }, [selectedModelId, selectedProviderId]);

  // Render appropriate chart based on type
  const renderChart = () => {
    if (chartData.length === 0) {
      return (
        <Box 
          display="flex" 
          justifyContent="center" 
          alignItems="center" 
          height={400}
        >
          <Typography variant="body1" color="text.secondary">
            No data available for the selected filters
          </Typography>
        </Box>
      );
    }

    if (chartType === 'bar') {
      return (
        <ResponsiveContainer width="100%" height={400}>
          <BarChart data={chartData} margin={{ top: 20, right: 30, left: 20, bottom: 50 }}>
            <CartesianGrid strokeDasharray="3 3" />
            <XAxis dataKey="date" angle={-45} textAnchor="end" height={70} />
            <YAxis />
            <Tooltip 
              formatter={(value) => {
                if (metric === 'cost') {
                  return [`$${Number(value).toFixed(2)}`, metricConfig.label];
                }
                return [value, metricConfig.label];
              }}
            />
            <Legend />
            <Bar 
              dataKey={metric} 
              name={metricConfig.label} 
              fill={metricConfig.color} 
            />
          </BarChart>
        </ResponsiveContainer>
      );
    }

    return (
      <ResponsiveContainer width="100%" height={400}>
        <LineChart data={chartData} margin={{ top: 20, right: 30, left: 20, bottom: 50 }}>
          <CartesianGrid strokeDasharray="3 3" />
          <XAxis dataKey="date" angle={-45} textAnchor="end" height={70} />
          <YAxis />
          <Tooltip 
            formatter={(value) => {
              if (metric === 'cost') {
                return [`$${Number(value).toFixed(2)}`, metricConfig.label];
              }
              return [value, metricConfig.label];
            }}
          />
          <Legend />
          <Line 
            type="monotone" 
            dataKey={metric} 
            name={metricConfig.label} 
            stroke={metricConfig.color} 
            activeDot={{ r: 8 }} 
          />
        </LineChart>
      </ResponsiveContainer>
    );
  };

  return (
    <Card sx={{ mb: 4 }}>
      <CardContent>
        <Box display="flex" justifyContent="space-between" alignItems="center" mb={2}>
          <Box>
            <Typography variant="h6">Usage Over Time</Typography>
            {chartSubtitle && (
              <Typography variant="body2" color="text.secondary">
                {chartSubtitle}
              </Typography>
            )}
          </Box>
          <Box>
            <ToggleButtonGroup
              value={metric}
              exclusive
              onChange={handleMetricChange}
              size="small"
              sx={{ mr: 2 }}
            >
              <ToggleButton value="requests">Requests</ToggleButton>
              <ToggleButton value="tokens">Tokens</ToggleButton>
              <ToggleButton value="cost">Cost</ToggleButton>
            </ToggleButtonGroup>
            
            <ToggleButtonGroup
              value={chartType}
              exclusive
              onChange={handleChartTypeChange}
              size="small"
            >
              <ToggleButton value="bar">Bar</ToggleButton>
              <ToggleButton value="line">Line</ToggleButton>
            </ToggleButtonGroup>
          </Box>
        </Box>
        
        {isLoading ? (
          <Box 
            display="flex" 
            justifyContent="center" 
            alignItems="center" 
            height={400}
          >
            <Typography>Loading chart data...</Typography>
          </Box>
        ) : (
          renderChart()
        )}
      </CardContent>
    </Card>
  );
};

export default UsageChart;