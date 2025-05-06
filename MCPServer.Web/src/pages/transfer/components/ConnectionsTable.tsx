import React from 'react';
import {
  Paper,
  Chip,
  IconButton,
} from '@mui/material';
import { DataGrid } from '@mui/x-data-grid';
import { Edit as EditIcon } from '@mui/icons-material';

export default function ConnectionsTable({ connections, onEdit, isLoading }) {
  // Connection columns for DataGrid
  const connectionColumns = [
    { field: 'connectionId', headerName: 'ID', width: 70 },
    { field: 'connectionName', headerName: 'Name', width: 200 },
    { field: 'description', headerName: 'Description', width: 250, flex: 1 },
    {
      field: 'isSource',
      headerName: 'Source',
      width: 120,
      renderCell: (params) => (
        <Chip
          label={params.row.isSource ? 'Yes' : 'No'}
          color={params.row.isSource ? 'success' : 'default'}
          size="small"
        />
      ),
    },
    {
      field: 'isDestination',
      headerName: 'Destination',
      width: 120,
      renderCell: (params) => (
        <Chip
          label={params.row.isDestination ? 'Yes' : 'No'}
          color={params.row.isDestination ? 'success' : 'default'}
          size="small"
        />
      ),
    },
    {
      field: 'isActive',
      headerName: 'Active',
      width: 120,
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
      width: 120,
      renderCell: (params) => (
        <IconButton
          color="primary"
          onClick={() => onEdit(params.row)}
          size="small"
        >
          <EditIcon />
        </IconButton>
      ),
    },
  ];

  return (
    <Paper sx={{ height: 500, width: '100%' }}>
      <DataGrid
        rows={connections || []}
        columns={connectionColumns}
        initialState={{
          pagination: {
            paginationModel: { pageSize: 10, page: 0 },
          },
        }}
        pageSizeOptions={[10, 25, 50]}
        getRowId={(row) => row.connectionId}
        loading={isLoading}
        disableRowSelectionOnClick
      />
    </Paper>
  );
}