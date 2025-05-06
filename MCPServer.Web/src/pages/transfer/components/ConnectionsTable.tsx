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

interface Connection {
  connectionId: number;
  connectionName: string;
  description?: string;
  isSource?: boolean;
  isDestination?: boolean;
  isActive: boolean;
  connectionAccessLevel?: 'ReadOnly' | 'WriteOnly' | 'ReadWrite';
  lastTestedOn?: string | Date | null;
  connectionString: string;
  connectionStringForDisplay?: string;
  connectionDetails?: {
    server?: string;
    database?: string;
    username?: string;
    password?: string;
    port?: string;
  };
  createdOn?: string | Date;
  lastModifiedOn?: string | Date;
  maxPoolSize?: number;
  minPoolSize?: number;
}

interface ConnectionsTableProps {
  connections: Connection[];
  onEdit: (connection: Connection) => void;
  onTest: (connection: Connection) => void;
  isLoading: boolean;
}

export default function ConnectionsTable({ connections, onEdit, onTest, isLoading }: ConnectionsTableProps) {
  // Connection columns for DataGrid
  const connectionColumns = [
    { field: 'connectionId', headerName: 'ID', width: 70 },
    { field: 'connectionName', headerName: 'Name', width: 200 },
    { field: 'description', headerName: 'Description', width: 250, flex: 1 },
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
      field: 'createdOn',
      headerName: 'Created On',
      width: 180,
      valueGetter: (params: GridValueGetterParams<any, Connection>) => {
        return params.row.createdOn ? new Date(params.row.createdOn) : null;
      },
      valueFormatter: (params: GridValueFormatterParams) => {
        return params.value ? params.value.toLocaleString() : '';
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
      field: 'lastTestedOn',
      headerName: 'Last Tested',
      width: 180,
      valueGetter: (params: GridValueGetterParams<any, Connection>) => {
        return params.row.lastTestedOn ? new Date(params.row.lastTestedOn) : null;
      },
      valueFormatter: (params: GridValueFormatterParams) => {
        return params.value ? params.value.toLocaleString() : 'Never';
      },
    },
    {
      field: 'serverInfo',
      headerName: 'Server',
      width: 250,
      valueGetter: (params: GridValueGetterParams<any, Connection>) => {
        // If we have connection details, use the server from there
        if (params.row.connectionDetails?.server) {
          return `Server=${params.row.connectionDetails.server}`;
        }

        // Otherwise try to extract from connection string
        if (!params.row.connectionString) return '';

        // Use connectionStringForDisplay if available (for hashed strings)
        const str = (params.row.connectionStringForDisplay || params.row.connectionString) as string;

        // Show only the server part
        const serverPart = str.match(/Server=([^;]+)/i);
        return serverPart ? serverPart[0] : '[Unknown]';
      },
    },
    {
      field: 'poolSize',
      headerName: 'Pool Size',
      width: 150,
      valueGetter: (params: GridValueGetterParams<any, Connection>) => {
        const min = params.row.minPoolSize || 0;
        const max = params.row.maxPoolSize || 0;
        return `${min}-${max}`;
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