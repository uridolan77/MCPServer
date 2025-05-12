import {
  Paper,
  Chip,
  IconButton,
  Tooltip,
} from '@mui/material';
import {
  DataGrid,
  GridRenderCellParams,
  GridValueGetterParams,
  GridValueFormatterParams
} from '@mui/x-data-grid';
import {
  Edit as EditIcon,
  PlayArrow as TestIcon
} from '@mui/icons-material';
import { Connection } from '../types/Connection';

interface ConnectionsTableProps {
  connections: Connection[];
  onEdit: (connection: Connection) => void;
  onTest: (connection: Connection) => void;
  isLoading: boolean;
}

export default function ConnectionsTable({ connections, onEdit, onTest, isLoading }: ConnectionsTableProps) {
  // Log the connections prop to debug
  console.log('ConnectionsTable received connections:', connections);
  console.log('ConnectionsTable connections type:', typeof connections);
  console.log('ConnectionsTable connections is array:', Array.isArray(connections));
  console.log('ConnectionsTable connections length:', connections?.length);

  // Connection columns for DataGrid
  const connectionColumns = [
    { field: 'connectionId', headerName: 'ID', width: 70 },
    { field: 'connectionName', headerName: 'Name', width: 200 },
    { field: 'description', headerName: 'Description', width: 250, flex: 1 },
    {
      field: 'server',
      headerName: 'Server',
      width: 180
    },
    {
      field: 'database',
      headerName: 'Database',
      width: 150
    },
    {
      field: 'connectionAccessLevel',
      headerName: 'Access Level',
      width: 150,
      renderCell: (params: GridRenderCellParams<any, Connection>) => {
        // If connectionAccessLevel is not available, derive it from isSource and isDestination
        let accessLevel = params.row.connectionAccessLevel;
        if (!accessLevel) {
          if (params.row.isSource && params.row.isDestination) {
            accessLevel = 'ReadWrite';
          } else if (params.row.isSource) {
            accessLevel = 'ReadOnly';
          } else if (params.row.isDestination) {
            accessLevel = 'WriteOnly';
          }
        }

        let chipColor: 'default' | 'primary' | 'secondary' | 'error' | 'info' | 'success' | 'warning' = 'default';
        let label = accessLevel || '';

        if (accessLevel === 'ReadWrite') {
          chipColor = 'success';
          label = 'Read/Write';
        } else if (accessLevel === 'ReadOnly') {
          chipColor = 'info';
          label = 'Read Only';
        } else if (accessLevel === 'WriteOnly') {
          chipColor = 'warning';
          label = 'Write Only';
        }

        return (
          <Chip
            label={label}
            color={chipColor}
            size="small"
          />
        );
      },
    },
    {
      field: 'timeout',
      headerName: 'Timeout',
      width: 100,
      valueGetter: (params: GridValueGetterParams<any, Connection>) => {
        return params.row.timeout || 30;
      },
    },
    {
      field: 'poolSize',
      headerName: 'Pool Size',
      width: 120,
      valueGetter: (params: GridValueGetterParams<any, Connection>) => {
        const min = params.row.minPoolSize || 0;
        const max = params.row.maxPoolSize || 0;
        return `${min}-${max}`;
      },
    },
    {
      field: 'lastModifiedOn',
      headerName: 'Last Modified',
      width: 180,
      valueGetter: (params: GridValueGetterParams<any, Connection>) => {
        return params.row.lastModifiedOn ? new Date(params.row.lastModifiedOn) : null;
      },
      valueFormatter: (params: GridValueFormatterParams) => {
        return params.value ? params.value.toLocaleString() : '';
      },
    },
    {
      field: 'isActive',
      headerName: 'Active',
      width: 120,
      renderCell: (params: GridRenderCellParams<any, Connection>) => (
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
      renderCell: (params: GridRenderCellParams<any, Connection>) => (
        <>
          <Tooltip title="Edit Connection">
            <IconButton
              color="primary"
              onClick={() => onEdit(params.row)}
              size="small"
              sx={{ mr: 1 }}
            >
              <EditIcon />
            </IconButton>
          </Tooltip>
          <Tooltip title="Test Connection">
            <IconButton
              color="success"
              onClick={() => onTest(params.row)}
              size="small"
            >
              <TestIcon />
            </IconButton>
          </Tooltip>
        </>
      ),
    },
  ];

  // Create a safe rows array for DataGrid
  const safeRows = Array.isArray(connections) ? connections : [];
  console.log('Safe rows for DataGrid:', safeRows);
  console.log('Safe rows length:', safeRows.length);
  console.log('Safe rows type:', typeof safeRows);

  // Check if rows have connectionId property
  if (safeRows.length > 0) {
    console.log('First row has connectionId:', 'connectionId' in safeRows[0]);
    console.log('Sample row:', safeRows[0]);
    console.log('Sample row keys:', Object.keys(safeRows[0]));
    
    // Debug field availability with case variations
    const row = safeRows[0];
    console.log('Server field check:', {
      'server': row.server, 
      'Server': (row as any).Server,
      'has lowercase': 'server' in row,
      'has uppercase': 'Server' in row
    });
    
    console.log('Database field check:', {
      'database': row.database, 
      'Database': (row as any).Database,
      'has lowercase': 'database' in row,
      'has uppercase': 'Database' in row
    });
    
    // Normalize case sensitivity issues in all rows
    safeRows.forEach(row => {
      if (!row.server && (row as any).Server) {
        row.server = (row as any).Server;
      }
      if (!row.database && (row as any).Database) {
        row.database = (row as any).Database;
      }
      // Also check other potential field case issues
      if (!row.connectionName && (row as any).ConnectionName) {
        row.connectionName = (row as any).ConnectionName;
      }
      if (!row.description && (row as any).Description) {
        row.description = (row as any).Description;
      }
    });
    
    console.log('Sample row connectionId type:', typeof safeRows[0].connectionId);
  } else {
    console.warn('No rows to display in ConnectionsTable');
  }

  return (
    <Paper sx={{ height: 500, width: '100%' }}>
      <DataGrid
        rows={safeRows}
        columns={connectionColumns}
        initialState={{
          pagination: {
            paginationModel: { pageSize: 10, page: 0 },
          },
        }}
        pageSizeOptions={[10, 25, 50]}
        getRowId={(row) => {
          // Check for different ID fields in order of preference
          if (row.connectionId !== undefined) {
            return row.connectionId;
          } else if (row.id !== undefined) {
            return row.id;
          } else if (row.$id !== undefined) {
            return row.$id;
          } else {
            // Generate a fallback ID based on object properties
            // This is a last resort to prevent errors
            console.warn('Row missing ID field, generating fallback ID:', row);
            const idString = JSON.stringify(row);
            const fallbackId = `fallback-${Math.abs(
              idString.split('').reduce((acc, char) => {
                return (acc << 5) - acc + char.charCodeAt(0) | 0;
              }, 0)
            )}`;
            
            // Add the ID to the row object to make it consistent
            (row as any).connectionId = fallbackId;
            return fallbackId;
          }
        }}
        loading={isLoading}
        disableRowSelectionOnClick
      />
    </Paper>
  );
}