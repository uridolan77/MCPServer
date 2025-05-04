import React from 'react';
import { Box } from '@mui/material';
import UsageTable from './UsageTable';

// This component provides a unified view for both types of log tables
const CombinedLogsView: React.FC = () => {
  return (
    <Box>
      {/* Main logs table - already handles both chat and LLM logs */}
      <UsageTable />
    </Box>
  );
};

export default CombinedLogsView;