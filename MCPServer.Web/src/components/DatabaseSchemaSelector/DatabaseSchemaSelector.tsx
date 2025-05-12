import React, { useState, useEffect, useCallback } from 'react';
import {
  Box,
  Typography,
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
  InputAdornment,
  Button,
  Checkbox,
  FormControlLabel,
  Alert,
  IconButton,
  Tooltip
} from '@mui/material';
import {
  ExpandMore as ExpandMoreIcon,
  Search as SearchIcon,
  TableView as TableViewIcon,
  ViewColumn as ViewColumnIcon,
  SelectAll as SelectAllIcon,
  ClearAll as ClearAllIcon,
  Refresh as RefreshIcon,
  Error as ErrorIcon
} from '@mui/icons-material';
import DataTransferService from '@/services/dataTransfer.service';

// Define interfaces for our schema data
interface Column {
  name: string;
  dataType: string;
  isNullable: boolean;
  isIdentity?: boolean;
  isPrimaryKey?: boolean;
  maxLength?: number;
  defaultValue?: string;
  selected?: boolean;
}

interface SchemaItem {
  schema: string;
  name: string;
  type: string;
  columnCount: number;
  columns: Column[];
  primaryKey?: string[];
  selected?: boolean;
}

interface TabPanelProps {
  children?: React.ReactNode;
  index: number;
  value: number;
}

interface DatabaseSchemaSelectorProps {
  connectionId?: number | null;
  connectionString?: string;
  initialSelectedTables?: string[];
  onSchemaLoaded?: (success: boolean) => void;
  onSelectionChange?: (selectedTables: SchemaItem[]) => void;
  onGenerateJson?: (json: string) => void;
  formData?: any;
  updateFormData?: (data: any) => void;
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

export const DatabaseSchemaSelector: React.FC<DatabaseSchemaSelectorProps> = ({
  connectionId,
  connectionString,
  initialSelectedTables = [],
  onSchemaLoaded,
  onSelectionChange,
  onGenerateJson,
  formData,
  updateFormData
}) => {
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);
  const [databaseName, setDatabaseName] = useState<string>('Database');
  const [schema, setSchema] = useState<any[]>([]);
  const [tabValue, setTabValue] = useState(0);
  const [searchTerm, setSearchTerm] = useState('');
  const [normalizedSchema, setNormalizedSchema] = useState<SchemaItem[]>([]);
  
  // Use a ref to store callback functions to avoid dependency changes
  const callbacksRef = React.useRef({
    updateFormData,
    onSelectionChange,
    onGenerateJson,
    onSchemaLoaded
  });
  
  // Update ref when callbacks change
  React.useEffect(() => {
    callbacksRef.current = {
      updateFormData,
      onSelectionChange,
      onGenerateJson,
      onSchemaLoaded
    };
  }, [updateFormData, onSelectionChange, onGenerateJson, onSchemaLoaded]);
  
  // Fetch schema data when connectionId or connectionString changes
  const fetchDatabaseSchema = useCallback(async () => {
    if (!connectionId && !connectionString) {
      console.log('No connection information provided to DatabaseSchemaSelector:', 
        { connectionId, connectionString });
      setError('No connection information provided');
      callbacksRef.current.onSchemaLoaded?.(false);
      return;
    }

    // Debug the connectionId value
    console.log('fetchDatabaseSchema: Using connectionId:', connectionId);
    
    setIsLoading(true);
    setError(null);
    
    try {
      let response;
      
      if (connectionId) {
        console.log('Fetching schema using connectionId:', connectionId);
        // Use the connectionId to fetch schema
        response = await DataTransferService.getDatabaseSchema(connectionId);
      } else if (connectionString) {
        console.log('Fetching schema using connectionString');
        
        // Check if the connection string needs SQL Server authentication placeholders resolved
        let processedConnectionString = connectionString;
        
        if (processedConnectionString.includes("Username=") && !processedConnectionString.includes("User ID=")) {
          processedConnectionString = processedConnectionString.replace(/Username=([^;]+)/g, "User ID=$1");
        }
        
        if (processedConnectionString.includes("Password=") && processedConnectionString.includes("{{")) {
          // Handle potential Key Vault references - use safe authentication
          console.log("Connection string contains potential secure placeholders, using connection object");
          response = await DataTransferService.fetchDatabaseSchema({
            connectionString: processedConnectionString,
            connectionName: databaseName || "TempConnection",
            isActive: true
          });
        } else {
          // Use the connectionString directly
          response = await DataTransferService.getDatabaseSchemaByConnectionString(processedConnectionString);
        }
      }
      
      if (!response) {
        throw new Error('No response from server');
      }
      
      console.log('Schema response received:', response);
      
      // Handle different response formats
      if (response.schema) {
        // New API format with schema property
        setSchema(response.schema);
      } else if (response.data && response.data.schema) {
        // Nested schema in data property
        setSchema(response.data.schema);
      } else {
        // Direct schema data or other format
        setSchema(response);
      }
      
      // Extract database name from response if available
      let dbName = null;
      const responseObj = typeof response === 'string' ? JSON.parse(response) : response;
      
      if (responseObj) {
        if (responseObj.database) {
          dbName = responseObj.database;
        } else if (responseObj.databaseName) {
          dbName = responseObj.databaseName;
        } else if (responseObj.data && responseObj.data.database) {
          dbName = responseObj.data.database;
        } else if (Array.isArray(responseObj) && responseObj[0] && responseObj[0].databaseName) {
          dbName = responseObj[0].databaseName;
        } else {
          // Try to extract from connection string
          const dbNameMatch = connectionString?.match(/Initial Catalog=([^;]+)/i) || 
                          connectionString?.match(/Database=([^;]+)/i);
          if (dbNameMatch && dbNameMatch[1]) {
            dbName = dbNameMatch[1];
          }
        }
      }
      
      if (dbName) {
        console.log('Database name detected:', dbName);
        setDatabaseName(dbName);
      } else {
        console.log('No database name found in response, using default');
        setDatabaseName('Database');
      }
      
      callbacksRef.current.onSchemaLoaded?.(true);
    } catch (error: any) {
      console.error('Error fetching database schema:', error);
      let errorMessage = 'Failed to load database schema';
      
      // Extract detailed error from API response
      if (error.response) {
        const responseData = error.response.data;
        if (typeof responseData === 'object') {
          if (responseData.message) {
            errorMessage = responseData.message;
          } else if (responseData.error) {
            errorMessage = responseData.error;
          } else if (responseData.errorCode) {
            errorMessage = `Error ${responseData.errorCode}: ${responseData.error || 'Database connection failed'}`;
          }
        } else if (typeof responseData === 'string') {
          errorMessage = responseData;
        }
      } else if (error.message) {
        errorMessage = error.message;
      }
      
      console.log('Error message to display:', errorMessage);
      setError(errorMessage);
      callbacksRef.current.onSchemaLoaded?.(false);
    } finally {
      setIsLoading(false);
    }
  }, [connectionId, connectionString]);
  
  // Process schema data when it changes
  useEffect(() => {
    if (!schema || schema.length === 0) {
      console.log('No schema data available');
      setNormalizedSchema([]);
      return;
    }

    try {
      let processedSchema: SchemaItem[] = [];
      
      // Check if schema is an array or an object with tables
      let tablesArray: any[] = [];
      
      if (Array.isArray(schema)) {
        // If it's a direct array, use it
        tablesArray = schema;
      } else if (schema.tables) {
        // If it has a tables property, use that
        // Check if tables has $values (API format) or is a direct array
        tablesArray = schema.tables.$values || schema.tables;
      } else if (schema[0] && schema[0].tables) {
        // Some API responses nest tables under the first element
        tablesArray = schema[0].tables.$values || schema[0].tables;
      } else if (schema[1] && Array.isArray(schema[1])) {
        // Handle specific format where second element is the tables array
        tablesArray = schema[1];
      }
      
      // Ensure tablesArray is an array
      if (!Array.isArray(tablesArray)) {
        console.warn('Could not locate tables array in schema data:', schema);
        tablesArray = [];
      }
      
      console.log('Processing tables array:', tablesArray);
      
      // Process each table in the array
      tablesArray.forEach((table) => {
        if (table && typeof table === 'object') {
          // Get table details
          const tableName = table.name || '';
          const schemaName = table.schema || 'dbo';
          const objectType = table.type || 'Table';
          const columnCount = table.columnCount || 0;
          
          // Check if this table should be initially selected
          const tableKey = `${schemaName}.${tableName}`;
          const isInitiallySelected = initialSelectedTables.includes(tableKey);
          
          // Process columns - handle the nested structure with $values
          let tableColumns: Column[] = [];
          
          // Check different possible column formats
          if (table.columns) {
            if (table.columns.$values && Array.isArray(table.columns.$values)) {
              // Handle $values format
              tableColumns = table.columns.$values.map((col: any) => ({
                name: col.name || '',
                dataType: col.dataType || 'unknown',
                isNullable: col.isNullable === true,
                isIdentity: col.isIdentity === true,
                isPrimaryKey: col.isPrimaryKey === true,
                maxLength: col.maxLength,
                defaultValue: col.defaultValue,
                selected: isInitiallySelected
              }));
            } else if (Array.isArray(table.columns)) {
              // Handle direct array format
              tableColumns = table.columns.map((col: any) => ({
                name: col.name || '',
                dataType: col.dataType || 'unknown',
                isNullable: col.isNullable === true,
                isIdentity: col.isIdentity === true,
                isPrimaryKey: col.isPrimaryKey === true,
                maxLength: col.maxLength,
                defaultValue: col.defaultValue,
                selected: isInitiallySelected
              }));
            }
          }
          
          // Add table with its columns to the processed schema
          processedSchema.push({
            schema: schemaName,
            name: tableName,
            type: objectType,
            columnCount: tableColumns.length || columnCount,
            columns: tableColumns,
            selected: isInitiallySelected
          });
        }
      });
      
      console.log('Processed schema:', processedSchema);
      setNormalizedSchema(processedSchema);
      
      // Use callbacksRef to avoid dependency cycles
      if (callbacksRef.current.updateFormData) {
        callbacksRef.current.updateFormData({
          schema: processedSchema,
          databaseName
        });
      }
      
      // Notify parent about selection change
      callbacksRef.current.onSelectionChange?.(processedSchema.filter(t => t.selected));
    } catch (error: any) {
      console.error('Error processing schema data:', error);
      setError(error.message || 'Error processing schema data');
      setNormalizedSchema([]);
    }
  }, [schema, initialSelectedTables]);
  
  // Initial data fetch
  useEffect(() => {
    fetchDatabaseSchema();
  }, [fetchDatabaseSchema]);
  
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
  
  // Handle selection changes for tables
  const handleTableSelectionChange = (tableIndex: number, isSelected: boolean) => {
    setNormalizedSchema(prev => {
      const updated = [...prev];
      updated[tableIndex].selected = isSelected;
      
      // If table is unselected, unselect all its columns
      if (!isSelected) {
        updated[tableIndex].columns = updated[tableIndex].columns.map(col => ({
          ...col,
          selected: false
        }));
      }
      
      // Notify parent about selection change using callbacksRef
      callbacksRef.current.onSelectionChange?.(updated.filter(t => t.selected));
      
      // Update form data if in wizard using callbacksRef
      if (callbacksRef.current.updateFormData) {
        callbacksRef.current.updateFormData({ schema: updated });
      }
      
      return updated;
    });
  };
  
  // Handle selection changes for columns
  const handleColumnSelectionChange = (tableIndex: number, columnIndex: number, isSelected: boolean) => {
    setNormalizedSchema(prev => {
      const updated = [...prev];
      updated[tableIndex].columns[columnIndex].selected = isSelected;
      
      // If any column is selected, make sure the table is selected
      if (isSelected) {
        updated[tableIndex].selected = true;
      }
      
      // Notify parent about selection change using callbacksRef
      callbacksRef.current.onSelectionChange?.(updated.filter(t => t.selected));
      
      // Update form data if in wizard using callbacksRef
      if (callbacksRef.current.updateFormData) {
        callbacksRef.current.updateFormData({ schema: updated });
      }
      
      return updated;
    });
  };
  
  // Select or deselect all tables
  const handleSelectAllTables = (selected: boolean) => {
    setNormalizedSchema(prev => {
      const updated = prev.map(item => ({
        ...item,
        selected,
        columns: item.columns.map(col => ({
          ...col,
          selected: selected
        }))
      }));
      
      // Notify parent about selection change using callbacksRef
      callbacksRef.current.onSelectionChange?.(updated.filter(t => t.selected));
      
      // Update form data if in wizard using callbacksRef
      if (callbacksRef.current.updateFormData) {
        callbacksRef.current.updateFormData({ schema: updated });
      }
      
      return updated;
    });
  };
  
  // Select or deselect all columns for a specific table
  const handleSelectAllColumns = (tableIndex: number, selected: boolean) => {
    setNormalizedSchema(prev => {
      const updated = [...prev];
      
      // If selecting all columns, make sure the table is selected too
      if (selected) {
        updated[tableIndex].selected = true;
      }
      
      updated[tableIndex].columns = updated[tableIndex].columns.map(col => ({
        ...col,
        selected
      }));
      
      // Notify parent about selection change using callbacksRef
      callbacksRef.current.onSelectionChange?.(updated.filter(t => t.selected));
      
      // Update form data if in wizard using callbacksRef
      if (callbacksRef.current.updateFormData) {
        callbacksRef.current.updateFormData({ schema: updated });
      }
      
      return updated;
    });
  };
  
  // Generate the meta-schema JSON from selected tables/columns
  const generateMetaSchemaJson = useCallback(() => {
    try {
      // Only include selected tables and columns
      const selectedTables = normalizedSchema.filter(t => t.selected);
      
      // Create the base schema structure based on the DatabaseSchemaExtractor format
      const metaSchema = {
        metadata: {
          id: `${databaseName.toLowerCase()}-schema-v1`,
          version: "1.0.0",
          name: `${databaseName} Database Schema`,
          description: `Schema definition for the ${databaseName} database`,
          created: new Date().toISOString(),
          modified: new Date().toISOString()
        },
        entities: [] as any[],
        relationships: [] as any[]
      };
      
      // Generate database entity
      const databaseEntity = {
        name: "Database",
        description: "Represents a database instance",
        values: [
          {
            id: databaseName.toLowerCase(),
            name: databaseName,
            description: `Database '${databaseName}'`,
            vendor: "SQL Server",
            version: "2019", // This would come from your actual DB in production
            created: new Date().toISOString(),
            collation: "SQL_Latin1_General_CP1_CI_AS", // Example value
            characterSet: "UTF-16"
          }
        ]
      };
      
      // Generate schema entities
      const schemaEntities = [...new Set(selectedTables.map(t => t.schema))].map(schemaName => ({
        id: schemaName.toLowerCase(),
        databaseId: databaseName.toLowerCase(),
        name: schemaName,
        description: `Schema '${schemaName}' in database '${databaseName}'`,
        owner: "dbo", // Example value
        created: new Date().toISOString(),
        lastModified: new Date().toISOString()
      }));
      
      const schemaEntity = {
        name: "Schema",
        description: "Represents a schema within a database",
        values: schemaEntities
      };
      
      // Generate table entities
      const tableEntities = selectedTables.filter(t => t.type === "Table").map(table => {
        const tableId = `${table.schema.toLowerCase()}.${table.name.toLowerCase()}`;
        return {
          id: tableId,
          schemaId: table.schema.toLowerCase(),
          name: table.name,
          description: `Table '${table.schema}.${table.name}' in database '${databaseName}'`,
          type: "regular",
          created: new Date().toISOString(),
          lastModified: new Date().toISOString(),
          isSystem: false,
          estimatedRows: 0 // This would come from your actual DB in production
        };
      });
      
      const tableEntity = {
        name: "Table",
        description: "Represents a table in a database schema",
        values: tableEntities
      };
      
      // Generate column entities
      const columnEntities: any[] = [];
      
      selectedTables.filter(t => t.type === "Table").forEach(table => {
        const tableId = `${table.schema.toLowerCase()}.${table.name.toLowerCase()}`;
        
        // Only include selected columns
        const selectedColumns = table.columns.filter(c => c.selected);
        
        selectedColumns.forEach((column, index) => {
          columnEntities.push({
            id: `${tableId}.${column.name.toLowerCase()}`,
            tableId: tableId,
            name: column.name,
            description: `Column '${column.name}' in table '${table.schema}.${table.name}'`,
            ordinalPosition: index + 1,
            dataType: column.dataType,
            maxLength: column.maxLength,
            isNullable: column.isNullable,
            defaultValue: column.defaultValue,
            isIdentity: column.isIdentity,
            isPrimaryKey: column.isPrimaryKey
          });
        });
      });
      
      const columnEntity = {
        name: "Column",
        description: "Represents a column in a database table",
        values: columnEntities
      };
      
      // Generate view entities
      const viewEntities = selectedTables.filter(t => t.type === "View").map(view => {
        const viewId = `${view.schema.toLowerCase()}.${view.name.toLowerCase()}`;
        return {
          id: viewId,
          schemaId: view.schema.toLowerCase(),
          name: view.name,
          description: `View '${view.schema}.${view.name}' in database '${databaseName}'`,
          definition: "SELECT * FROM ...", // In production, this would be the actual view definition
          created: new Date().toISOString(),
          lastModified: new Date().toISOString()
        };
      });
      
      const viewEntity = {
        name: "View",
        description: "Represents a view in a database schema",
        values: viewEntities
      };
      
      // Add entities to schema
      metaSchema.entities.push(databaseEntity);
      metaSchema.entities.push(schemaEntity);
      metaSchema.entities.push(tableEntity);
      metaSchema.entities.push(columnEntity);
      
      if (viewEntities.length > 0) {
        metaSchema.entities.push(viewEntity);
      }
      
      // Create basic relationships
      metaSchema.relationships = [
        {
          name: "DatabaseToSchema",
          description: "Relationship between a database and its schemas",
          source: "Database",
          target: "Schema",
          cardinality: "one-to-many",
          sourceProperty: "id",
          targetProperty: "databaseId"
        },
        {
          name: "SchemaToTable",
          description: "Relationship between a schema and its tables",
          source: "Schema",
          target: "Table",
          cardinality: "one-to-many",
          sourceProperty: "id",
          targetProperty: "schemaId"
        },
        {
          name: "TableToColumn",
          description: "Relationship between a table and its columns",
          source: "Table",
          target: "Column",
          cardinality: "one-to-many",
          sourceProperty: "id",
          targetProperty: "tableId"
        }
      ];
      
      // If we have views, add a relationship for them
      if (viewEntities.length > 0) {
        metaSchema.relationships.push({
          name: "SchemaToView",
          description: "Relationship between a schema and its views",
          source: "Schema",
          target: "View",
          cardinality: "one-to-many",
          sourceProperty: "id",
          targetProperty: "schemaId"
        });
      }
      
      // Convert the schema to pretty-printed JSON
      const json = JSON.stringify(metaSchema, null, 2);
      
      // Call the callback with the generated JSON
      callbacksRef.current.onGenerateJson?.(json);
      
      // Update form data if in wizard
      if (updateFormData) {
        updateFormData({ 
          generatedJson: json,
          selectedTables: selectedTables.map(t => `${t.schema}.${t.name}`)
        });
      }
      
      return json;
    } catch (error: any) {
      console.error('Error generating meta-schema JSON:', error);
      setError(error.message || 'Failed to generate schema JSON');
      return '';
    }
  // Remove databaseName from dependency array to prevent infinite updates
  }, [normalizedSchema, callbacksRef]);
  
  // Render table columns with checkboxes
  const renderColumns = (tableIndex: number, columns: Column[]) => {
    if (!columns || columns.length === 0) {
      return <Typography color="text.secondary" sx={{ py: 2, textAlign: 'center' }}>No column information available</Typography>;
    }
    
    return (
      <>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1 }}>
          <FormControlLabel
            control={
              <Checkbox 
                checked={columns.every(col => col.selected)} 
                indeterminate={columns.some(col => col.selected) && !columns.every(col => col.selected)}
                onChange={(e) => handleSelectAllColumns(tableIndex, e.target.checked)}
                size="small"
              />
            }
            label={<Typography variant="body2">Select All Columns</Typography>}
          />
        </Box>
        <TableContainer component={Paper} variant="outlined">
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell padding="checkbox">
                  <Typography variant="subtitle2">Select</Typography>
                </TableCell>
                <TableCell>Name</TableCell>
                <TableCell>Data Type</TableCell>
                <TableCell align="center">Nullable</TableCell>
                <TableCell align="center">PK</TableCell>
                <TableCell align="center">Identity</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {columns.map((column, columnIndex) => (
                <TableRow 
                  key={column.name || `column-${columnIndex}`}
                  sx={{ 
                    backgroundColor: column.isPrimaryKey ? 'rgba(25, 118, 210, 0.08)' : 'inherit'
                  }}
                >
                  <TableCell padding="checkbox">
                    <Checkbox 
                      checked={column.selected === true}
                      onChange={(e) => handleColumnSelectionChange(tableIndex, columnIndex, e.target.checked)}
                      size="small"
                    />
                  </TableCell>
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
      </>
    );
  };
  
  // Render tables and views with checkboxes
  const renderSchemaItems = (items: SchemaItem[]) => {
    if (items.length === 0) {
      return <Typography color="text.secondary" sx={{ py: 2, textAlign: 'center' }}>No items found</Typography>;
    }
    
    return items.map((item, index) => {
      // Find the index in the original array
      const originalIndex = normalizedSchema.findIndex(
        i => i.schema === item.schema && i.name === item.name
      );
      
      return (
        <Accordion key={`${item.schema}.${item.name}` || `item-${index}`} sx={{ mb: 1 }}>
          <AccordionSummary
            expandIcon={<ExpandMoreIcon />}
            aria-controls={`${item.schema}-${item.name}-content`}
            id={`${item.schema}-${item.name}-header`}
          >
            <Box display="flex" alignItems="center" width="100%">
              <Box>
                <Checkbox 
                  checked={item.selected === true}
                  onChange={(e) => handleTableSelectionChange(originalIndex, e.target.checked)}
                  onClick={(e) => e.stopPropagation()}
                  size="small"
                />
              </Box>
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
            {renderColumns(originalIndex, item.columns)}
          </AccordionDetails>
        </Accordion>
      );
    });
  };

  return (
    <Box sx={{ width: '100%' }}>
      {error && (
        <Alert 
          severity="error" 
          sx={{ mb: 2 }}
          action={
            <Button color="inherit" size="small" onClick={fetchDatabaseSchema}>
              Retry
            </Button>
          }
        >
          {error}
        </Alert>
      )}
      
      <Box sx={{ mb: 2 }}>
        <Typography variant="h6" gutterBottom>
          Database: {databaseName}
        </Typography>
        <Typography variant="body2" color="text.secondary">
          Select the tables and columns you want to include in the schema mapping.
        </Typography>
      </Box>
      
      {isLoading ? (
        <Box sx={{ display: 'flex', justifyContent: 'center', my: 4 }}>
          <CircularProgress />
        </Box>
      ) : !normalizedSchema || normalizedSchema.length === 0 ? (
        <Box sx={{ textAlign: 'center', my: 4 }}>
          <Typography color="text.secondary" sx={{ mb: 2 }}>No schema information available</Typography>
          <Button 
            variant="outlined" 
            startIcon={<RefreshIcon />} 
            onClick={fetchDatabaseSchema}
          >
            Load Schema
          </Button>
        </Box>
      ) : (
        <Box sx={{ width: '100%', mt: 1 }}>
          <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
            <TextField
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
              sx={{ width: '50%' }}
            />
            
            <Box>
              <Button
                variant="outlined"
                color="primary"
                onClick={() => generateMetaSchemaJson()}
                sx={{ mr: 1 }}
                disabled={normalizedSchema.filter(t => t.selected).length === 0}
              >
                Generate Schema
              </Button>
              
              <Tooltip title="Select All">
                <IconButton onClick={() => handleSelectAllTables(true)} size="small" sx={{ mr: 1 }}>
                  <SelectAllIcon />
                </IconButton>
              </Tooltip>
              <Tooltip title="Deselect All">
                <IconButton onClick={() => handleSelectAllTables(false)} size="small">
                  <ClearAllIcon />
                </IconButton>
              </Tooltip>
            </Box>
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
    </Box>
  );
}

export default DatabaseSchemaSelector;