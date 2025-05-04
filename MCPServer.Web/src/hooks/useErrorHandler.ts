import { useCallback } from 'react';
import { AxiosError } from 'axios';
import { useNotification } from '@/contexts/NotificationContext';

interface ApiError {
  message: string;
  errors?: Record<string, string[]>;
  stackTrace?: string;
}

const useErrorHandler = () => {
  const { addNotification } = useNotification();

  const handleError = useCallback(
    (error: unknown, fallbackMessage: string = 'An unexpected error occurred') => {
      console.error('Error:', error);

      if (error instanceof AxiosError) {
        const status = error.response?.status;
        const data = error.response?.data as ApiError | undefined;

        // Handle specific HTTP status codes
        if (status === 401) {
          addNotification('You are not authorized to perform this action. Please log in again.', 'error');
          return;
        }

        if (status === 403) {
          addNotification('You do not have permission to perform this action.', 'error');
          return;
        }

        if (status === 404) {
          addNotification('The requested resource was not found.', 'error');
          return;
        }

        if (status === 422 && data?.errors) {
          // Handle validation errors
          const validationErrors = Object.entries(data.errors)
            .map(([field, errors]) => `${field}: ${errors.join(', ')}`)
            .join('; ');
          
          addNotification(`Validation error: ${validationErrors}`, 'error');
          return;
        }

        // Use the error message from the API if available
        if (data?.message) {
          addNotification(data.message, 'error');
          return;
        }

        // Fallback to a generic error message based on status code
        if (status) {
          addNotification(`Error ${status}: ${error.message || fallbackMessage}`, 'error');
          return;
        }
      }

      // Handle non-Axios errors or when no specific handling is available
      if (error instanceof Error) {
        addNotification(error.message || fallbackMessage, 'error');
        return;
      }

      // Fallback for unknown error types
      addNotification(fallbackMessage, 'error');
    },
    [addNotification]
  );

  return { handleError };
};

export default useErrorHandler;
