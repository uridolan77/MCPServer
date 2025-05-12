import { useState } from 'react';
import { useConnectionForm } from './useConnectionForm';
import { Connection } from '../types/Connection';

interface UseConnectionDialogStateProps {
  connection: Connection | null;
  onSave: (connection: Connection) => void;
  onClose: () => void;
}

export function useConnectionDialogState({ 
  connection, 
  onSave, 
  onClose 
}: UseConnectionDialogStateProps) {
  // Use the existing connection form hook
  const connectionForm = useConnectionForm({ 
    initialConnection: connection 
  });
  
  // Dialog-specific states
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [currentTab, setCurrentTab] = useState(0);
  const [validationErrors, setValidationErrors] = useState<Record<string, string>>({});
  
  // Handle form submission
  const handleSubmit = async () => {
    // Validate the form
    const errors = validateForm();
    if (Object.keys(errors).length > 0) {
      setValidationErrors(errors);
      return;
    }
    
    setIsSubmitting(true);
    try {
      // Prepare the connection data for saving
      const connectionData = connectionForm.prepareConnectionData();
      
      // Call the onSave callback
      await onSave(connectionData);
      
      // Close the dialog
      onClose();
    } catch (error) {
      console.error('Error saving connection:', error);
    } finally {
      setIsSubmitting(false);
    }
  };
  
  // Validate the form data
  const validateForm = (): Record<string, string> => {
    const errors: Record<string, string> = {};
    const { formData } = connectionForm;
    
    if (!formData.connectionName.trim()) {
      errors.connectionName = 'Connection name is required';
    }
    
    if (connectionForm.connectionMode === 'string') {
      if (!formData.connectionString.trim()) {
        errors.connectionString = 'Connection string is required';
      }
    } else {
      // Validate connection details
      const { connectionDetails } = connectionForm;
      
      if (!connectionDetails.server.trim()) {
        errors.server = 'Server is required';
      }
      
      if (!connectionDetails.database.trim()) {
        errors.database = 'Database is required';
      }
    }
    
    return errors;
  };
  
  // Reset the form
  const resetForm = () => {
    // Reset validation errors
    setValidationErrors({});
    
    // Reset to initial tab
    setCurrentTab(0);
    
    // Close the dialog
    onClose();
  };
  
  // Handle tab changes
  const handleTabChange = (event: React.SyntheticEvent, newValue: number) => {
    setCurrentTab(newValue);
  };
  
  // Check if a field has an error
  const hasError = (fieldName: string): boolean => {
    return !!validationErrors[fieldName];
  };
  
  // Get error message for a field
  const getErrorMessage = (fieldName: string): string => {
    return validationErrors[fieldName] || '';
  };
  
  // Clear error for a field
  const clearError = (fieldName: string) => {
    if (validationErrors[fieldName]) {
      const { [fieldName]: _, ...rest } = validationErrors;
      setValidationErrors(rest);
    }
  };
  
  return {
    // Include all props from useConnectionForm
    ...connectionForm,
    
    // Dialog-specific state and handlers
    isSubmitting,
    currentTab,
    validationErrors,
    handleSubmit,
    resetForm,
    handleTabChange,
    hasError,
    getErrorMessage,
    clearError
  };
}

export default useConnectionDialogState;