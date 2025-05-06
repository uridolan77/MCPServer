import React from 'react';
import {
  Paper,
  Chip,
  IconButton,
  Box,
} from '@mui/material';
import { DataGrid } from '@mui/x-data-grid';
import { Visibility as VisibilityIcon } from '@mui/icons-material';

export default function RunHistoryTable({ runHistory, onViewDetails, isLoading }) {
  // Run history columns for DataGrid
  const runHistoryColumns = [
    { field: 'runId', headerName: 'ID', width: 70 },
    { field: 'configurationName', headerName: 'Configuration', width: 200 },
    {
      field: 'startTime',
      headerName: 'Start Time',
      width: 180,
      valueFormatter: (params) => new Date(params.value).toLocaleString(),
    },
    {
      field: 'endTime',
      headerName: 'End Time',
      width: 180,
      valueFormatter: (params) => params.value ? new Date(params.value).toLocaleString() : 'Running...',
    },
    {
      field: 'status',
      headerName: 'Status',
      width: 150,
      renderCell: (params) => {
        let color: 'default' | 'success' | 'error' | 'info' | 'warning' | 'primary' | 'secondary' = 'default';
        if (params.value === 'Completed') color = 'success';
        else if (params.value === 'Failed') color = 'error';
        else if (params.value === 'Running') color = 'info';
        else if (params.value === 'CompletedWithErrors') color = 'warning';

        return (
          <Chip
            label={params.value}
            color={color}
            size="small"
          />
        );
      },
    },
    {
      field: 'totalTablesProcessed',
      headerName: 'Tables',
      width: 80,
      valueFormatter: (params) => params.value.toLocaleString(),
    },
    {
      field: 'totalRowsProcessed',
      headerName: 'Rows',
      width: 100,
      valueFormatter: (params) => params.value.toLocaleString(),
    },
    {
      field: 'elapsedMs',
      headerName: 'Duration',
      width: 120,
      valueFormatter: (params) => {
        if (!params.value) return 'Running...';
        const seconds = params.value / 1000;
        if (seconds < 60) return `${seconds.toFixed(2)}s`;
        const minutes = Math.floor(seconds / 60);
        const remainingSeconds = seconds % 60;
        return `${minutes}m ${remainingSeconds.toFixed(0)}s`;
      },
    },
    {
      field: 'actions',
      headerName: 'Actions',
      width: 100,
      renderCell: (params) => (
        <IconButton
          color="primary"
          onClick={() => onViewDetails(params.row)}
          title="View Details"
          size="small"
        >
          <VisibilityIcon />
        </IconButton>
      ),
    },
  ];

  return (
    <Paper sx={{ height: 500, width: '100%' }}>
      <DataGrid
        rows={runHistory || []}
        columns={runHistoryColumns}
        initialState={{
          pagination: {
            paginationModel: { pageSize: 10, page: 0 },
          },
          sorting: {
            sortModel: [{ field: 'startTime', sort: 'desc' }],
          },
        }}
        pageSizeOptions={[10, 25, 50]}
        getRowId={(row) => row.runId}
        loading={isLoading}
        disableRowSelectionOnClick
      />
    </Paper>
  );
}