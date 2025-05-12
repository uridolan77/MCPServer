import { useState } from 'react';
import DataTransferService from '@/services/dataTransfer.service';
import { Connection, ConnectionTestResult } from '../types/ConnectionTypes';

export function useConnectionTesting() {
  const [isTesting, setIsTesting] = useState(false);
  const [testResult, setTestResult] = useState<ConnectionTestResult | null>(null);
  const [connectionTested, setConnectionTested] = useState(false);

  const testConnection = async (connectionData: Partial<Connection>, connectionString: string) => {
    if (!connectionString) {
      setTestResult({
        success: false,
        message: 'Connection string is required'
      });
      return null;
    }

    setIsTesting(true);
    setTestResult(null);

    try {
      console.log('Testing connection with username:', connectionData.username);
      console.log('Testing connection with password:', connectionData.password);
      console.log('Testing connection string:', connectionString);

      const response = await DataTransferService.testConnection({
        ...connectionData,
        connectionString
      } as Connection);

      if (response && response.success) {
        const result = {
          success: true,
          message: response.message || 'Connection successful',
          server: response.server || '',
          database: response.database || ''
        };
        
        setTestResult(result);
        setConnectionTested(true);
        return result;
      } else {
        const result = {
          success: false,
          message: response?.message || 'Connection test failed with unknown error',
          detailedError: response?.detailedError || response?.error,
          server: response?.server || '',
          database: response?.database || '',
          errorCode: response?.errorCode,
          errorType: response?.errorType,
          innerException: response?.innerException
        };
        
        setTestResult(result);
        setConnectionTested(false);
        return result;
      }
    } catch (error: any) {
      console.error('Connection test error:', error);

      let errorMessage = 'Connection test failed';
      let detailedError = '';

      if (error.response && error.response.data) {
        const errorData = error.response.data;
        errorMessage = errorData.message || errorData.error || 'Connection test failed';
        detailedError = errorData.detailedError || errorData.error || error.message;

        const result = {
          success: false,
          message: errorMessage,
          detailedError: detailedError,
          server: errorData.connectionDetails?.server || errorData.server || '',
          database: errorData.connectionDetails?.database || '',
          errorCode: errorData.errorCode,
          errorType: errorData.exceptionType,
          innerException: errorData.innerException
        };
        
        setTestResult(result);
        setConnectionTested(false);
        return result;
      } else {
        errorMessage = error.message || 'Connection test failed';
        detailedError = error.stack || '';

        const result = {
          success: false,
          message: errorMessage,
          detailedError: detailedError,
          errorType: error.name
        };
        
        setTestResult(result);
        setConnectionTested(false);
        return result;
      }
    } finally {
      setIsTesting(false);
    }
  };

  const resetTestState = () => {
    setTestResult(null);
    setConnectionTested(false);
  };

  return {
    isTesting,
    testResult,
    connectionTested,
    testConnection,
    resetTestState
  };
}