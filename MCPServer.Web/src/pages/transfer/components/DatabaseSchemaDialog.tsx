import React, { useState, useEffect } from 'react';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  Typography,
  Box,
  Paper,
  TableContainer,
  Table,
  TableHead,
  TableBody,
  TableRow,
  TableCell,
  CircularProgress,
  Tabs,
  Tab,
  Accordion,
  AccordionSummary,
  AccordionDetails,
  Chip,
  TextField,
  InputAdornment
} from '@mui/material';
import {
  ExpandMore as ExpandMoreIcon,
  Search as SearchIcon,
  TableView as TableViewIcon,
  ViewColumn as ViewColumnIcon
} from '@mui/icons-material';

interface Column {
  name: string;
  dataType: string;
  isNullable: boolean;
  isIdentity?: boolean;
  isPrimaryKey?: boolean;
  maxLength?: number;
  defaultValue?: string;
}

interface SchemaItem {
  schema: string;
  name: string;
  type: string;
  columnCount: number;
  columns: Column[];
  primaryKey?: string[];
}

interface DatabaseSchemaDialogProps {
  open: boolean;
  isLoading: boolean;
  databaseName: string;
  schema: any[];
  onClose: () => void;
}

interface TabPanelProps {
  children?: React.ReactNode;
  index: number;
  value: number;
}

function TabPanel(props: TabPanelProps) {
  const { children, value, index, ...other } = props;

  return (
    <div
      role="tabpanel"
      hidden={value !== index}
      id={`schema-tabpanel-${index}`}
      aria-labelledby={`schema-tab-${index}`}
      {...other}
    >
      {value === index && (
        <Box sx={{ p: 1 }}>
          {children}
        </Box>
      )}
    </div>
  );
}

export default function DatabaseSchemaDialog({ open, isLoading, databaseName, schema, onClose }: DatabaseSchemaDialogProps) {
  const [tabValue, setTabValue] = useState(0);
  const [searchTerm, setSearchTerm] = useState('');
  const [normalizedSchema, setNormalizedSchema] = useState<SchemaItem[]>([]);
  
  // Process schema data when it changes
  useEffect(() => {
    if (!schema || schema.length === 0) {
      console.log('No schema data available');
      setNormalizedSchema([]);
      return;
    }

    console.log('Processing schema data:', schema);
    
    try {
      let processedSchema: SchemaItem[] = [];
      
      // Based on the screenshot, the schema data is structured as [index, Array(tables)]
      // Extract the tables array if available
      const tablesArray = Array.isArray(schema) && schema.length > 1 && Array.isArray(schema[1]) 
        ? schema[1] 
        : Array.isArray(schema) ? schema : [];
      
      // Process each table in the array
      tablesArray.forEach((table) => {
        if (table && typeof table === 'object') {
          // Get table details
          const tableName = table.name || '';
          const schemaName = table.schema || 'dbo';
          const objectType = table.type || 'Table';
          const columnCount = table.columnCount || 0;
          
          // Process columns - handle the nested structure with $values
          let tableColumns: Column[] = [];
          
          // Check if columns exists and has a $values property (as seen in the sample data)
          if (table.columns && table.columns.$values && Array.isArray(table.columns.$values)) {
            tableColumns = table.columns.$values.map((col: any) => ({
              name: col.name || '',
              dataType: col.dataType || 'unknown',
              isNullable: col.isNullable === true,
              isIdentity: col.isIdentity === true,
              isPrimaryKey: col.isPrimaryKey === true,
              maxLength: col.maxLength,
              defaultValue: col.defaultValue
            }));
          }
          // Also try direct columns array if $values isn't present
          else if (Array.isArray(table.columns)) {
            tableColumns = table.columns.map((col: any) => ({
              name: col.name || '',
              dataType: col.dataType || 'unknown',
              isNullable: col.isNullable === true,
              isIdentity: col.isIdentity === true,
              isPrimaryKey: col.isPrimaryKey === true,
              maxLength: col.maxLength,
              defaultValue: col.defaultValue
            }));
          }
          
          // Add table with its columns to the processed schema
          processedSchema.push({
            schema: schemaName,
            name: tableName,
            type: objectType,
            columnCount: tableColumns.length || columnCount,
            columns: tableColumns
          });
        }
      });
      
      console.log('Normalized schema data:', processedSchema);
      setNormalizedSchema(processedSchema);
    } catch (error) {
      console.error('Error processing schema data:', error);
      setNormalizedSchema([]);
    }
  }, [schema]);
  
  const handleChangeTab = (_event: React.SyntheticEvent, newValue: number) => {
    setTabValue(newValue);
  };
  
  // Filter schema based on search term
  const filteredSchema = normalizedSchema.filter(item => 
    item.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
    item.schema.toLowerCase().includes(searchTerm.toLowerCase())
  );
  
  // Get tables and views
  const tables = filteredSchema.filter(item => item.type === 'Table');
  const views = filteredSchema.filter(item => 
    item.type === 'View' || item.name.toLowerCase().includes('view')
  );
  
  // Sort all items alphabetically
  const sortedTables = [...tables].sort((a, b) => 
    `${a.schema}.${a.name}`.localeCompare(`${b.schema}.${b.name}`)
  );
  const sortedViews = [...views].sort((a, b) => 
    `${a.schema}.${a.name}`.localeCompare(`${b.schema}.${b.name}`)
  );
  
  // Render table columns
  const renderColumns = (columns: Column[]) => {
    if (!columns || columns.length === 0) {
      return <Typography color="text.secondary" sx={{ py: 2, textAlign: 'center' }}>No column information available</Typography>;
    }
    
    return (
      <TableContainer component={Paper} variant="outlined">
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Name</TableCell>
              <TableCell>Data Type</TableCell>
              <TableCell align="center">Nullable</TableCell>
              <TableCell align="center">PK</TableCell>
              <TableCell align="center">Identity</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {columns.map((column, index) => (
              <TableRow 
                key={column.name || `column-${index}`}
                sx={{ 
                  backgroundColor: column.isPrimaryKey ? 'rgba(25, 118, 210, 0.08)' : 'inherit'
                }}
              >
                <TableCell>
                  <Typography variant="body2" fontWeight={column.isPrimaryKey ? 'bold' : 'normal'}>
                    {column.name}
                  </Typography>
                </TableCell>
                <TableCell>
                  <Typography variant="body2">
                    {column.dataType}{column.maxLength ? `(${column.maxLength})` : ''}
                  </Typography>
                </TableCell>
                <TableCell align="center">
                  {column.isNullable ? 'Yes' : 'No'}
                </TableCell>
                <TableCell align="center">
                  {column.isPrimaryKey && (
                    <Chip size="small" color="primary" label="PK" />
                  )}
                </TableCell>
                <TableCell align="center">
                  {column.isIdentity && (
                    <Chip size="small" color="secondary" label="Identity" />
                  )}
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>
    );
  };
  
  // Render tables and views
  const renderSchemaItems = (items: SchemaItem[]) => {
    if (items.length === 0) {
      return <Typography color="text.secondary" sx={{ py: 2, textAlign: 'center' }}>No items found</Typography>;
    }
    
    return items.map((item, index) => (
      <Accordion key={`${item.schema}.${item.name}` || `item-${index}`} sx={{ mb: 1 }}>
        <AccordionSummary
          expandIcon={<ExpandMoreIcon />}
          aria-controls={`${item.schema}-${item.name}-content`}
          id={`${item.schema}-${item.name}-header`}
        >
          <Box display="flex" alignItems="center" width="100%">
            <Box flexGrow={1} display="flex" alignItems="center">
              <Typography variant="subtitle1" fontWeight="medium" sx={{ mr: 1 }}>
                {item.name}
              </Typography>
              <Typography variant="caption" color="text.secondary" sx={{ mr: 1 }}>
                ({item.schema})
              </Typography>
              <Chip 
                size="small" 
                label={`${item.columnCount} columns`} 
                variant="outlined"
              />
            </Box>
            <Typography variant="caption" color="text.secondary">
              {item.type}
            </Typography>
          </Box>
        </AccordionSummary>
        <AccordionDetails sx={{ pt: 0 }}>
          {renderColumns(item.columns)}
        </AccordionDetails>
      </Accordion>
    ));
  };
  
  return (
    <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
      <DialogTitle>
        Database Schema: {databaseName}
      </DialogTitle>
      <DialogContent>
        {isLoading ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', my: 4 }}>
            <CircularProgress />
          </Box>
        ) : !normalizedSchema || normalizedSchema.length === 0 ? (
          <Box sx={{ textAlign: 'center', my: 4 }}>
            <Typography color="text.secondary" sx={{ mb: 2 }}>No schema information available</Typography>
            {schema && schema.length > 0 && (
              <Box sx={{ mt: 2, p: 2, border: '1px solid #ddd', borderRadius: 1 }}>
                <Typography variant="body2" color="error" gutterBottom>
                  Schema data received but couldn't be processed properly.
                </Typography>
                <Typography variant="caption" color="text.secondary">
                  Schema contains {Array.isArray(schema) ? schema.length : '?'} items
                </Typography>
                <Accordion sx={{ mt: 2 }}>
                  <AccordionSummary expandIcon={<ExpandMoreIcon />}>
                    <Typography variant="subtitle2" color="text.secondary">Raw Schema Data</Typography>
                  </AccordionSummary>
                  <AccordionDetails>
                    <Box sx={{ maxHeight: 300, overflow: 'auto' }}>
                      <pre style={{ fontSize: '11px' }}>
                        {JSON.stringify(schema, null, 2)}
                      </pre>
                    </Box>
                  </AccordionDetails>
                </Accordion>
              </Box>
            )}
          </Box>
        ) : (
          <Box sx={{ width: '100%', mt: 1 }}>
            <Box sx={{ mb: 2 }}>
              <TextField
                fullWidth
                variant="outlined"
                placeholder="Search tables and views..."
                size="small"
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                InputProps={{
                  startAdornment: (
                    <InputAdornment position="start">
                      <SearchIcon />
                    </InputAdornment>
                  ),
                }}
              />
            </Box>
            
            <Box sx={{ borderBottom: 1, borderColor: 'divider' }}>
              <Tabs value={tabValue} onChange={handleChangeTab}>
                <Tab icon={<TableViewIcon fontSize="small" />} iconPosition="start" label={`Tables (${tables.length})`} />
                <Tab icon={<ViewColumnIcon fontSize="small" />} iconPosition="start" label={`Views (${views.length})`} />
              </Tabs>
            </Box>
            
            <Box sx={{ mt: 2, mb: 1 }}>
              <Typography variant="subtitle2" color="primary" gutterBottom>
                {tabValue === 0 ? `Tables: ${tables.length}` : `Views: ${views.length}`}
              </Typography>
              {searchTerm && (
                <Typography variant="caption" color="text.secondary">
                  Showing filtered results for "{searchTerm}"
                </Typography>  
              )}
            </Box>
            
            <TabPanel value={tabValue} index={0}>
              {renderSchemaItems(sortedTables)}
            </TabPanel>
            
            <TabPanel value={tabValue} index={1}>
              {renderSchemaItems(sortedViews)}
            </TabPanel>
          </Box>
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Close</Button>
      </DialogActions>
    </Dialog>
  );
}