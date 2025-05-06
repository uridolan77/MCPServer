import React, { useState, useEffect } from 'react';
import {
  Box,
  Typography,
  Paper,
  TextField,
  Button,
  Stack,
  Snackbar,
  Alert,
  Divider
} from '@mui/material';
import {
  ContentCopy as CopyIcon,
  Download as DownloadIcon,
  ContentPaste as PasteIcon
} from '@mui/icons-material';

interface JsonEditorProps {
  formData?: any;
  updateFormData?: (data: any) => void;
  minHeight?: string | number;
}

const JsonEditor: React.FC<JsonEditorProps> = ({
  formData = {},
  updateFormData = () => {},
  minHeight = '400px'
}) => {
  const [jsonValue, setJsonValue] = useState<string>(
    formData?.generatedJson || '{}'
  );
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [notification, setNotification] = useState<{
    open: boolean;
    message: string;
    severity: 'success' | 'error' | 'info';
  }>({
    open: false,
    message: '',
    severity: 'info',
  });

  // Update json when formData changes
  useEffect(() => {
    if (formData?.generatedJson) {
      try {
        // Try to format the JSON if it's not already formatted
        const formatted = JSON.stringify(
          typeof formData.generatedJson === 'string' 
            ? JSON.parse(formData.generatedJson) 
            : formData.generatedJson, 
          null, 
          2
        );
        setJsonValue(formatted);
      } catch (e) {
        // If it's not valid JSON, just use the string
        setJsonValue(formData.generatedJson);
      }
    }
  }, [formData?.generatedJson]);

  const validateJson = (value: string): boolean => {
    try {
      if (!value.trim()) {
        setErrorMessage('JSON is empty');
        return false;
      }
      
      JSON.parse(value);
      setErrorMessage(null);
      return true;
    } catch (e: any) {
      setErrorMessage(`Invalid JSON: ${e.message}`);
      return false;
    }
  };

  const handleChange = (event: React.ChangeEvent<HTMLTextAreaElement>) => {
    const newValue = event.target.value;
    setJsonValue(newValue);
    
    // Don't validate on every keystroke, but clear error if user is typing
    if (errorMessage) {
      setErrorMessage(null);
    }
    
    updateFormData({
      generatedJson: newValue,
      isValidJson: false // Will be validated on blur
    });
  };

  const handleBlur = () => {
    const isValid = validateJson(jsonValue);
    
    updateFormData({
      generatedJson: jsonValue,
      isValidJson: isValid
    });
  };

  const handleFormatJson = () => {
    try {
      const parsed = JSON.parse(jsonValue);
      const formatted = JSON.stringify(parsed, null, 2);
      setJsonValue(formatted);
      setErrorMessage(null);
      
      updateFormData({
        generatedJson: formatted,
        isValidJson: true
      });
      
      showNotification('JSON formatted successfully', 'success');
    } catch (e: any) {
      setErrorMessage(`Could not format: ${e.message}`);
      showNotification('Invalid JSON - could not format', 'error');
    }
  };

  const handleCopyToClipboard = () => {
    navigator.clipboard.writeText(jsonValue).then(
      () => {
        showNotification('JSON copied to clipboard', 'success');
      },
      () => {
        showNotification('Failed to copy to clipboard', 'error');
      }
    );
  };

  const handlePasteFromClipboard = async () => {
    try {
      const text = await navigator.clipboard.readText();
      
      try {
        // Attempt to parse the clipboard content as JSON
        JSON.parse(text);
        setJsonValue(text);
        setErrorMessage(null);
        
        updateFormData({
          generatedJson: text,
          isValidJson: true
        });
        
        showNotification('JSON pasted from clipboard', 'success');
      } catch (e) {
        showNotification('Clipboard content is not valid JSON', 'error');
      }
    } catch (e) {
      showNotification('Failed to read from clipboard', 'error');
    }
  };

  const handleDownload = () => {
    if (!validateJson(jsonValue)) {
      showNotification('Cannot download invalid JSON', 'error');
      return;
    }
    
    const blob = new Blob([jsonValue], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `db-schema-${new Date().toISOString().split('T')[0]}.json`;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
    
    showNotification('JSON downloaded successfully', 'success');
  };

  const showNotification = (message: string, severity: 'success' | 'error' | 'info') => {
    setNotification({
      open: true,
      message,
      severity
    });
  };

  const closeNotification = () => {
    setNotification({
      ...notification,
      open: false
    });
  };

  return (
    <Box>
      <Typography variant="h6" gutterBottom>
        Schema JSON
      </Typography>
      <Typography variant="body2" color="text.secondary" paragraph>
        Review and edit the generated database schema JSON below.
      </Typography>
      
      <Stack direction="row" spacing={2} sx={{ mb: 2 }}>
        <Button
          startIcon={<CopyIcon />}
          variant="outlined"
          size="small"
          onClick={handleCopyToClipboard}
        >
          Copy
        </Button>
        <Button
          startIcon={<PasteIcon />}
          variant="outlined"
          size="small"
          onClick={handlePasteFromClipboard}
        >
          Paste
        </Button>
        <Button
          startIcon={<DownloadIcon />}
          variant="outlined"
          size="small"
          onClick={handleDownload}
        >
          Download
        </Button>
        <Button
          variant="outlined"
          size="small"
          onClick={handleFormatJson}
        >
          Format JSON
        </Button>
      </Stack>
      
      {errorMessage && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {errorMessage}
        </Alert>
      )}
      
      <Paper 
        variant="outlined" 
        sx={{ 
          p: 0,
          position: 'relative',
          '&:hover': {
            boxShadow: '0 0 0 1px rgba(0, 0, 0, 0.1)'
          }
        }}
      >
        <Box sx={{ position: 'absolute', right: '8px', top: '8px', zIndex: 1 }}>
          <Typography variant="caption" sx={{ color: 'text.secondary' }}>
            JSON Editor
          </Typography>
        </Box>
        <TextField
          multiline
          fullWidth
          variant="outlined"
          value={jsonValue}
          onChange={handleChange}
          onBlur={handleBlur}
          error={!!errorMessage}
          sx={{
            fontFamily: 'monospace',
            '& .MuiOutlinedInput-notchedOutline': {
              border: 'none'
            },
            '& .MuiOutlinedInput-root': {
              fontFamily: 'monospace',
              fontSize: '14px'
            },
            '& .MuiInputBase-input': {
              minHeight,
              padding: 2,
              lineHeight: 1.5,
            }
          }}
          inputProps={{
            style: {
              fontFamily: 'monospace',
            }
          }}
        />
      </Paper>
      
      <Divider sx={{ my: 2 }} />
      
      <Typography variant="body2" color="text.secondary">
        The JSON schema follows the database meta-schema format and can be used for data transfer operations.
      </Typography>
      
      <Snackbar
        open={notification.open}
        autoHideDuration={4000}
        onClose={closeNotification}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
      >
        <Alert 
          onClose={closeNotification} 
          severity={notification.severity} 
          sx={{ width: '100%' }}
        >
          {notification.message}
        </Alert>
      </Snackbar>
    </Box>
  );
};

export default JsonEditor;