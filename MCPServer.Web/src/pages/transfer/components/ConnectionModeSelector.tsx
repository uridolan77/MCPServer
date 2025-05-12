import React from 'react';
import { Tabs, Tab } from '@mui/material';

interface ConnectionModeSelectorProps {
  connectionMode: 'string' | 'details';
  onModeChange: (event: React.SyntheticEvent, newValue: 'string' | 'details') => void;
}

const ConnectionModeSelector = ({ connectionMode, onModeChange }: ConnectionModeSelectorProps) => {
  return (
    <Tabs
      value={connectionMode}
      onChange={onModeChange}
      aria-label="connection mode tabs"
      sx={{ mb: 2 }}
    >
      <Tab label="Connection String" value="string" />
      <Tab label="Connection Details" value="details" />
    </Tabs>
  );
};

export default ConnectionModeSelector;