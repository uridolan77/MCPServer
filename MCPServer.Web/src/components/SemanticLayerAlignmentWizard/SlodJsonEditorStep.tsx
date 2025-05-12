import React, { useState, useEffect } from 'react';
import { Box, Typography, Alert } from '@mui/material';
import { JSONStructureEditor } from '@/components/ConnectionWizard'; // Re-using existing JSON editor
import { SlodJsonEditorStepProps, GAMING_SLOD_JSON_CONTENT } from './types';
import { useSnackbar } from '@/hooks/useSnackbar';

const SlodJsonEditorStep: React.FC<SlodJsonEditorStepProps> = ({ formData, onFormDataChange, setStepValidated }) => {
  const { showSnackbar } = useSnackbar();
  const [currentJson, setCurrentJson] = useState<string>(formData.slodJsonContent || GAMING_SLOD_JSON_CONTENT);
  const [isValidJson, setIsValidJson] = useState<boolean>(formData.isValidSlodJson !== undefined ? formData.isValidSlodJson : true);

  useEffect(() => {
    // Load initial SLOD content from formData or default
    const initialContent = formData.slodJsonContent || GAMING_SLOD_JSON_CONTENT;
    setCurrentJson(initialContent);
    try {
      JSON.parse(initialContent);
      setIsValidJson(true);
      setStepValidated(true);
      onFormDataChange({ slodJsonContent: initialContent, isValidSlodJson: true });
    } catch (e) {
      setIsValidJson(false);
      setStepValidated(false);
      onFormDataChange({ slodJsonContent: initialContent, isValidSlodJson: false });
    }
  }, []); // Empty dependency array to run only on mount and use initial formData

  const handleJsonChange = (newJson: string, isValid: boolean) => {
    setCurrentJson(newJson);
    setIsValidJson(isValid);
    onFormDataChange({ slodJsonContent: newJson, isValidSlodJson: isValid });
    setStepValidated(isValid);
    if (isValid) {
      showSnackbar('SLOD JSON updated.', 'success');
    } else {
      showSnackbar('SLOD JSON has errors.', 'error');
    }
  };

  if (!formData.generatedDatabaseSchemaJson) {
    return (
      <Alert severity="warning" sx={{ m: 2 }}>
        Database schema JSON has not been generated from the previous step. Please complete schema selection first.
      </Alert>
    );
  }

  return (
    <Box sx={{ p: 2, height: '100%', display: 'flex', flexDirection: 'column' }}>
      <Typography variant="h6" gutterBottom>
        Semantic Layer Ontology Definition (SLOD)
      </Typography>
      <Typography variant="body2" color="text.secondary" gutterBottom>
        Review and edit the SLOD JSON. This JSON defines the semantic layer based on your selected schema and allows for further customization.
        The initial content is based on 'gaming-slod.json'.
      </Typography>

      <Box sx={{ flexGrow: 1, display: 'flex', flexDirection: 'column', minHeight: 0 }}>
        <JSONStructureEditor
          initialJson={currentJson} // Pass the current JSON content
          onJsonChange={handleJsonChange} // Callback for when JSON content or validity changes
          // readonly={false} // Default is false, so editable
          // showLineNumbers={true} // Default is true
          // showMiniMap={true} // Default is true
        />
      </Box>

      {!isValidJson && (
        <Alert severity="error" sx={{ mt: 2 }}>
          The SLOD JSON content is not valid. Please correct the errors to proceed.
        </Alert>
      )}
      {isValidJson && (
        <Alert severity="success" sx={{ mt: 2 }}>
          SLOD JSON is valid.
        </Alert>
      )}
    </Box>
  );
};

export default SlodJsonEditorStep;
