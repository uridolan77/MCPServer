import React from 'react';
import {
  Paper,
  Chip,
  IconButton,
  Box,
} from '@mui/material';
import { DataGrid } from '@mui/x-data-grid';
import {
  Edit as EditIcon,
  PlayArrow as PlayArrowIcon,
  History as HistoryIcon
} from '@mui/icons-material';

export default function ConfigurationsTable({
  configurations,
  connections,
  onEdit,
  onExecute,
  onViewHistory,
  isLoading
}) {
  // Configuration columns for DataGrid
  const configColumns = [
    { field: 'configurationId', headerName: 'ID', width: 70 },
    { field: 'configurationName', headerName: 'Name', width: 200 },
    { field: 'description', headerName: 'Description', width: 250, flex: 1 },
    {
      field: 'sourceConnection',
      headerName: 'Source',
      width: 150,
      valueGetter: (params) => params.row.sourceConnection?.connectionName || '',
    },
    {
      field: 'destinationConnection',
      headerName: 'Destination',
      width: 150,
      valueGetter: (params) => params.row.destinationConnection?.connectionName || '',
    },
    {
      field: 'tableMappings',
      headerName: 'Tables',
      width: 100,
      valueGetter: (params) => params.row.tableMappings?.length || 0,
    },
    {
      field: 'isActive',
      headerName: 'Active',
      width: 100,
      renderCell: (params) => (
        <Chip
          label={params.row.isActive ? 'Active' : 'Inactive'}
          color={params.row.isActive ? 'success' : 'default'}
          size="small"
        />
      ),
    },
    {
      field: 'actions',
      headerName: 'Actions',
      width: 180,
      renderCell: (params) => (
        <Box>
          <IconButton
            color="primary"
            onClick={() => onEdit(params.row)}
            title="Edit"
            size="small"
          >
            <EditIcon />
          </IconButton>
          <IconButton
            color="success"
            onClick={() => onExecute(params.row.configurationId)}
            title="Execute"
            size="small"
          >
            <PlayArrowIcon />
          </IconButton>
          <IconButton
            color="info"
            onClick={() => onViewHistory(params.row.configurationId)}
            title="History"
            size="small"
          >
            <HistoryIcon />
          </IconButton>
        </Box>
      ),
    },
  ];

  return (
    <Paper sx={{ height: 500, width: '100%' }}>
      <DataGrid
        rows={configurations || []}
        columns={configColumns}
        initialState={{
          pagination: {
            paginationModel: { pageSize: 10, page: 0 },
          },
        }}
        pageSizeOptions={[10, 25, 50]}
        getRowId={(row) => row.configurationId}
        loading={isLoading}
        disableRowSelectionOnClick
      />
    </Paper>
  );
}