import React, { useState } from 'react';
import {
  Paper,
  Typography,
  Box,
  List,
  ListItem,
  ListItemIcon,
  ListItemText,
  Collapse,
  IconButton,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Chip
} from '@mui/material';
import {
  KeyboardArrowDown as KeyboardArrowDownIcon,
  KeyboardArrowRight as KeyboardArrowRightIcon,
  TableRows as TableRowsIcon,
  ViewColumn as ViewColumnIcon,
  VpnKey as VpnKeyIcon,
} from '@mui/icons-material';

interface DatabaseSchemaViewerProps {
  schema: any[];
}

const DatabaseSchemaViewer: React.FC<DatabaseSchemaViewerProps> = ({ schema }) => {
  const [expandedTables, setExpandedTables] = useState<Record<string, boolean>>({});

  const toggleTable = (tableId: string) => {
    setExpandedTables(prev => ({
      ...prev,
      [tableId]: !prev[tableId]
    }));
  };

  if (!schema || schema.length === 0) {
    return (
      <Box p={3} textAlign="center">
        <Typography variant="body1">No schema information available</Typography>
      </Box>
    );
  }

  return (
    <Box>
      <Typography variant="h6" gutterBottom>
        Database Objects ({schema.length})
      </Typography>
      <List>
        {schema.map((table) => {
          const tableId = `${table.schema}.${table.name}`;
          const isExpanded = !!expandedTables[tableId];
          const columns = table.columns || [];
          const primaryKeys = table.primaryKey || [];
          
          return (
            <React.Fragment key={tableId}>
              <Paper variant="outlined" sx={{ mb: 1 }}>
                <ListItem button onClick={() => toggleTable(tableId)}>
                  <ListItemIcon>
                    {table.type === 'Table' ? <TableRowsIcon color="primary" /> : <ViewColumnIcon color="secondary" />}
                  </ListItemIcon>
                  <ListItemText 
                    primary={
                      <Box sx={{ display: 'flex', alignItems: 'center' }}>
                        <Typography variant="subtitle1" sx={{ mr: 1 }}>
                          {table.name}
                        </Typography>
                        <Chip 
                          size="small" 
                          label={table.schema} 
                          color="default" 
                          variant="outlined" 
                          sx={{ mr: 1 }}
                        />
                        <Chip 
                          size="small" 
                          label={table.type} 
                          color={table.type === 'Table' ? 'primary' : 'secondary'} 
                          variant="outlined"
                        />
                      </Box>
                    }
                    secondary={`${columns.length} columns`}
                  />
                  <IconButton edge="end">
                    {isExpanded ? <KeyboardArrowDownIcon /> : <KeyboardArrowRightIcon />}
                  </IconButton>
                </ListItem>
                <Collapse in={isExpanded} timeout="auto" unmountOnExit>
                  <Box p={2}>
                    <TableContainer>
                      <Table size="small">
                        <TableHead>
                          <TableRow>
                            <TableCell>Name</TableCell>
                            <TableCell>Data Type</TableCell>
                            <TableCell>Nullable</TableCell>
                            <TableCell>Default</TableCell>
                            <TableCell>Attributes</TableCell>
                          </TableRow>
                        </TableHead>
                        <TableBody>
                          {columns.map((column: any) => (
                            <TableRow key={column.name}>
                              <TableCell>
                                <Box sx={{ display: 'flex', alignItems: 'center' }}>
                                  {primaryKeys.includes(column.name) && (
                                    <VpnKeyIcon fontSize="small" color="primary" sx={{ mr: 1 }} />
                                  )}
                                  {column.name}
                                </Box>
                              </TableCell>
                              <TableCell>
                                {column.dataType}
                                {column.maxLength ? `(${column.maxLength})` : ''}
                              </TableCell>
                              <TableCell>{column.isNullable ? 'YES' : 'NO'}</TableCell>
                              <TableCell>{column.defaultValue || ''}</TableCell>
                              <TableCell>
                                {column.isPrimaryKey && <Chip size="small" label="PK" color="primary" sx={{ mr: 0.5 }} />}
                                {column.isIdentity && <Chip size="small" label="Identity" color="info" sx={{ mr: 0.5 }} />}
                              </TableCell>
                            </TableRow>
                          ))}
                        </TableBody>
                      </Table>
                    </TableContainer>
                  </Box>
                </Collapse>
              </Paper>
            </React.Fragment>
          );
        })}
      </List>
    </Box>
  );
};

export default DatabaseSchemaViewer;