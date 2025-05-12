import React, { useState, useEffect } from 'react';
import { Box, Typography, Paper, CircularProgress, Alert } from '@mui/material';
import DatabaseSchemaSelector from '@/components/DatabaseSchemaSelector/DatabaseSchemaSelector';
import { DatabaseSchemaSelectionStepProps } from './types';
import { useSnackbar } from '@/hooks/useSnackbar';

const DatabaseSchemaSelectionStep: React.FC<DatabaseSchemaSelectionStepProps> = ({ 
  formData, 
  onFormDataChange, 
  setStepValidated 
}) => {
  const { showSnackbar } = useSnackbar();
  const [isLoading, setIsLoading] = useState<boolean>(false);
  
  // Check if we have a valid connection before continuing
  useEffect(() => {
    if (!formData.selectedConnection) {
      setStepValidated(false);
      return;
    }
    
    // Log the connection information for debugging
    console.log('Connection data in schema selection step:', formData.selectedConnection);
    
    // Initial validation based on existing selections (if any)
    const hasExistingSelections = formData.rawSchemaSelection?.selectedTables?.length > 0;
    setStepValidated(hasExistingSelections);
  }, [formData.selectedConnection, formData.rawSchemaSelection, setStepValidated]);

  // Handle schema loading state changes
  const handleSchemaLoading = (isLoading: boolean) => {
    setIsLoading(isLoading);
  };

  // Handle when schema is loaded
  const handleSchemaLoaded = (success: boolean) => {
    if (!success) {
      showSnackbar('Failed to load database schema. Please check your connection settings.', 'error');
      setStepValidated(false);
    }
  };

  // Handle selection changes from the schema selector
  const handleSelectionChange = (selection: any) => {
    console.log('Schema selection changed:', selection);
    
    // Check if we have valid selections
    const hasValidSelections = selection && selection.length > 0;
    
    if (hasValidSelections) {
      // Prepare the raw schema selection in the format expected by the wizard
      const rawSchemaSelection = {
        selectedTables: selection.map((item: any) => ({
          name: item.name,
          schema: item.schema,
          columns: item.columns
            .filter((col: any) => col.selected)
            .map((col: any, idx: number) => ({
              name: col.name,
              dataType: col.dataType,
              ordinalPosition: idx + 1,
              isNullable: col.isNullable,
              isPrimaryKey: col.isPrimaryKey,
              isIdentity: col.isIdentity,
              maxLength: col.maxLength
            }))
        }))
      };
      
      // Update form data with the new selection
      onFormDataChange({ 
        rawSchemaSelection: rawSchemaSelection
      });
      
      // Set step as valid if we have at least one table with columns
      const isValid = rawSchemaSelection.selectedTables.some(table => table.columns.length > 0);
      setStepValidated(isValid);
      
      if (isValid) {
        showSnackbar('Schema elements selected successfully.', 'success');
      } else {
        showSnackbar('Please select at least one table with columns.', 'warning');
      }
    } else {
      onFormDataChange({ rawSchemaSelection: null });
      setStepValidated(false);
      showSnackbar('Please select at least one table and its columns.', 'info');
    }
  };

  // Handle schema JSON generation
  const handleGenerateJson = (json: string) => {
    if (json) {
      try {
        const parsedJson = JSON.parse(json);
        onFormDataChange({ 
          generatedDatabaseSchemaJson: parsedJson 
        });
        showSnackbar('Database schema JSON generated successfully.', 'success');
      } catch (error) {
        console.error('Error parsing generated JSON:', error);
        showSnackbar('Error generating schema JSON. Please try again.', 'error');
      }
    }
  };

  // Check if we have a valid connection
  if (!formData.selectedConnection || !formData.selectedConnection.connectionId) {
    return (
      <Alert severity="error" sx={{ m: 2 }}>
        No database connection selected. Please go back and select a valid connection.
      </Alert>
    );
  }

  // Check if the connection was successfully tested
  if (!formData.connectionTestResult?.success) {
    return (
      <Alert severity="warning" sx={{ m: 2 }}>
        The selected connection has not been successfully tested. Please go back and test the connection.
      </Alert>
    );
  }

  return (
    <Box sx={{ p: 2, height: '100%', display: 'flex', flexDirection: 'column' }}>
      <Typography variant="h6" gutterBottom>
        Select Database Schema Elements
      </Typography>
      
      {/* Display connection information */}
      {formData.selectedConnection && (
        <Paper sx={{ mb: 2, p: 2, bgcolor: 'background.default' }}>
          <Typography variant="subtitle1" fontWeight="bold" gutterBottom>
            Connection Information
          </Typography>
          <Box sx={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 2 }}>
            <Box>
              <Typography variant="body2" color="text.secondary">Connection Name:</Typography>
              <Typography variant="body1">{formData.selectedConnection.connectionName}</Typography>
            </Box>
            <Box>
              <Typography variant="body2" color="text.secondary">Server:</Typography>
              <Typography variant="body1">{formData.selectedConnection.server || 'N/A'}</Typography>
            </Box>
            <Box>
              <Typography variant="body2" color="text.secondary">Database:</Typography>
              <Typography variant="body1">{formData.selectedConnection.database || 'N/A'}</Typography>
            </Box>
            <Box>
              <Typography variant="body2" color="text.secondary">Connection ID:</Typography>
              <Typography variant="body1">{formData.selectedConnection.connectionId}</Typography>
            </Box>
          </Box>
        </Paper>
      )}
      
      <Typography variant="body2" color="text.secondary" gutterBottom>
        Choose the tables and columns you want to include in the semantic layer alignment.
        The selected elements will be used to generate the database schema JSON.
      </Typography>

      {isLoading && (
        <Box sx={{ display: 'flex', justifyContent: 'center', my: 2 }}>
          <CircularProgress />
        </Box>
      )}

      <Paper sx={{ flexGrow: 1, p: 2, overflowY: 'auto' }}>
        {/* Add debugging for connection information */}
        <pre style={{ display: 'none' }}>
          {JSON.stringify({ 
            connectionId: formData.selectedConnection.connectionId,
            id: formData.selectedConnection.id, 
            connection: formData.selectedConnection 
          }, null, 2)}
        </pre>
        
        <DatabaseSchemaSelector
          // Pass connection information - use both id and connectionId to be safe
          connectionId={Number(formData.selectedConnection.connectionId || formData.selectedConnection.id)}
          connectionString={formData.selectedConnection.connectionString}
          
          // Pass initial selections if any
          initialSelectedTables={
            formData.rawSchemaSelection?.selectedTables?.map(t => `${t.schema || 'dbo'}.${t.name}`) || []
          }
          
          // Event handlers
          onSchemaLoaded={handleSchemaLoaded}
          onSelectionChange={handleSelectionChange}
          onGenerateJson={handleGenerateJson}
          
          // Pass existing form data and update handler
          formData={formData}
          updateFormData={(data) => onFormDataChange(data)}
        />
      </Paper>

      {/* Status alerts */}
      {formData.rawSchemaSelection?.selectedTables?.length > 0 ? (
        <Alert severity="info" sx={{ mt: 2 }}>
          Selected {formData.rawSchemaSelection.selectedTables.length} table(s).
          Make sure all necessary columns are selected before proceeding.
        </Alert>
      ) : (
        <Alert severity="warning" sx={{ mt: 2 }}>
          Please select tables and columns to include in the schema mapping.
        </Alert>
      )}
    </Box>
  );
};

export default DatabaseSchemaSelectionStep;
