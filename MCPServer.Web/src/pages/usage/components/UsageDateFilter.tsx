import React from 'react';
import { Box, Stack, Typography } from '@mui/material';
import { DatePicker } from '@mui/x-date-pickers';
import { format, startOfMonth, endOfDay } from 'date-fns';
import { useUsageContext } from './UsageContext';

const UsageDateFilter: React.FC = () => {
  const { startDate, endDate, setStartDate, setEndDate, refreshData } = useUsageContext();

  // Handle start date change
  const handleStartDateChange = (newDate: Date | null) => {
    setStartDate(newDate);
  };

  // Handle end date change
  const handleEndDateChange = (newDate: Date | null) => {
    // If a new date is selected, set it to the end of that day
    if (newDate) {
      setEndDate(endOfDay(newDate));
    } else {
      setEndDate(null);
    }
  };

  // Format date for display
  const formatDateDisplay = (date: Date | null): string => {
    return date ? format(date, 'MMM d, yyyy') : 'Not set';
  };

  return (
    <Box>
      <Typography variant="subtitle2" gutterBottom>
        Date Range
      </Typography>
      <Stack direction="row" spacing={2} alignItems="center">
        <DatePicker
          label="Start Date"
          value={startDate}
          onChange={handleStartDateChange}
          slotProps={{
            textField: {
              size: 'small',
              fullWidth: true,
            },
          }}
        />
        <Typography variant="body2">to</Typography>
        <DatePicker
          label="End Date"
          value={endDate}
          onChange={handleEndDateChange}
          slotProps={{
            textField: {
              size: 'small',
              fullWidth: true,
            },
          }}
        />
      </Stack>
    </Box>
  );
};

export default UsageDateFilter;