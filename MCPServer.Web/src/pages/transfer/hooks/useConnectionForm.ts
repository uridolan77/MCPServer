import { useState, useEffect } from 'react';
import DataTransferService from '@/services/dataTransfer.service';

// Define the Connection interface to match our database model
interface Connection {
  connectionId: number;
  connectionName: string;
  connectionString: string;
  connectionAccessLevel: string;
  description: string;
  server: string;
  port: number | null;
  database: string;
  username: string;
  password: string;
  additionalParameters: string;
  isActive: boolean;
  isConnectionValid: boolean | null;
  minPoolSize: number | null;
  maxPoolSize: number | null;
  timeout: number | null;
  trustServerCertificate: boolean | null;
  encrypt: boolean | null;
  createdBy: string;
  createdOn: string;
  lastModifiedBy: string;
  lastModifiedOn: string | null;
  lastTestedOn: string | null;
  
  // Computed properties that exist in the C# model but not in DB
  isSource?: boolean;
  isDestination?: boolean;
}

interface TestResult {
  success: boolean;
  message: string;
  detailedError?: string;
  server?: string;
  database?: string;
  errorCode?: number;
  errorType?: string;
  innerException?: string;
}

interface UseConnectionFormProps {
  initialConnection: Connection | null;
}

export function useConnectionForm({ initialConnection }: UseConnectionFormProps) {
  // Base form data state
  const [formData, setFormData] = useState({
    connectionId: 0,
    connectionName: '',
    connectionString: '',
    description: '',
    isSource: true,
    isDestination: false,
    isActive: true,
    connectionAccessLevel: 'ReadOnly',
    lastTestedOn: null as Date | null,
    connectionType: 'sqlServer',
    timeout: 30,
    maxPoolSize: 100,
    minPoolSize: 5,
    encrypt: true,
    trustServerCertificate: true,
    createdBy: "System",
    lastModifiedBy: "System",
    createdOn: new Date().toISOString(),
    lastModifiedOn: new Date().toISOString()
  });

  // Connection mode (string vs details)
  const [connectionMode, setConnectionMode] = useState<'string' | 'details'>('string');
  
  // Detailed connection parameters
  const [connectionDetails, setConnectionDetails] = useState<Connection>({
    connectionId: 0,
    connectionName: '',
    connectionString: '',
    connectionAccessLevel: 'ReadOnly',
    description: '',
    server: '',
    port: null,
    database: '',
    username: '',
    password: '',
    additionalParameters: '',
    isActive: true,
    isConnectionValid: null,
    minPoolSize: null,
    maxPoolSize: null,
    timeout: null,
    trustServerCertificate: null,
    encrypt: null,
    createdBy: "System",
    createdOn: new Date().toISOString(),
    lastModifiedBy: "System",
    lastModifiedOn: new Date().toISOString(),
    lastTestedOn: null
  });

  // Connection testing states
  const [isTesting, setIsTesting] = useState(false);
  const [testResult, setTestResult] = useState<TestResult | null>(null);
  const [connectionTested, setConnectionTested] = useState(false);
  
  // Schema states
  const [isLoadingSchema, setIsLoadingSchema] = useState(false);
  const [dbSchema, setDbSchema] = useState<any[]>([]);
  const [schemaDialogOpen, setSchemaDialogOpen] = useState(false);

  // Initialize form data from connection prop
  useEffect(() => {
    if (initialConnection) {
      console.log('Loading connection for edit:', initialConnection);
      
      let accessLevel = initialConnection.connectionAccessLevel || 'ReadOnly';
      if (!initialConnection.connectionAccessLevel) {
        if (initialConnection.isSource && initialConnection.isDestination) {
          accessLevel = 'ReadWrite';
        } else if (initialConnection.isSource) {
          accessLevel = 'ReadOnly';
        } else if (initialConnection.isDestination) {
          accessLevel = 'WriteOnly';
        }
      }

      setFormData({
        connectionId: initialConnection.connectionId || 0,
        connectionName: initialConnection.connectionName || '',
        connectionString: initialConnection.connectionString || '',
        description: initialConnection.description || '',
        isSource: initialConnection.isSource !== undefined ? initialConnection.isSource : true,
        isDestination: initialConnection.isDestination !== undefined ? initialConnection.isDestination : false,
        isActive: initialConnection.isActive !== undefined ? initialConnection.isActive : true,
        connectionAccessLevel: accessLevel,
        lastTestedOn: initialConnection.lastTestedOn ? new Date(initialConnection.lastTestedOn) : null,
        connectionType: 'sqlServer', // Default for now
        timeout: initialConnection.timeout || 30,
        maxPoolSize: initialConnection.maxPoolSize || 100,
        minPoolSize: initialConnection.minPoolSize || 5,
        encrypt: initialConnection.encrypt !== null ? initialConnection.encrypt : true,
        trustServerCertificate: initialConnection.trustServerCertificate !== null ? initialConnection.trustServerCertificate : true,
        createdBy: initialConnection.createdBy || "System",
        lastModifiedBy: initialConnection.lastModifiedBy || "System",
        createdOn: initialConnection.createdOn || new Date().toISOString(),
        lastModifiedOn: initialConnection.lastModifiedOn || new Date().toISOString()
      });

      // Try to parse connection string if provided
      if (initialConnection.connectionString) {
        try {
          const details = parseConnectionString(initialConnection.connectionString);
          setConnectionDetails({
            ...initialConnection,
            ...details
          });
        } catch (error) {
          console.error('Failed to parse connection string', error);
          // Set details directly from the initialConnection
          setConnectionDetails(initialConnection);
        }
      } else {
        // Set details directly from the initialConnection
        setConnectionDetails(initialConnection);
      }

      // If we have connection details set as the mode
      if (initialConnection.server && initialConnection.database) {
        setConnectionMode('details');
      }
    }
  }, [initialConnection]);

  // Parse connection string to extract parameters
  const parseConnectionString = (connectionString: string) => {
    const details: any = {
      server: '',
      port: null,
      database: '',
      username: '',
      password: '',
      additionalParameters: ''
    };
    
    try {
      // Basic parsing logic - split by semicolons and extract key=value pairs
      const parts = connectionString.split(';');
      let additionalParams: string[] = [];
      
      parts.forEach(part => {
        const [key, value] = part.split('=');
        if (!key || !value) return;
        
        const keyLower = key.trim().toLowerCase();
        
        if (keyLower === 'server' || keyLower === 'data source') {
          // Handle server and optional port
          const serverParts = value.split(',');
          details.server = serverParts[0].trim();
          if (serverParts.length > 1 && serverParts[1].trim()) {
            details.port = parseInt(serverParts[1].trim(), 10);
          }
        } else if (keyLower === 'database' || keyLower === 'initial catalog') {
          details.database = value.trim();
        } else if (keyLower === 'user id' || keyLower === 'uid') {
          details.username = value.trim();
        } else if (keyLower === 'password' || keyLower === 'pwd') {
          details.password = value.trim();
        } else if (keyLower === 'min pool size') {
          details.minPoolSize = parseInt(value.trim(), 10);
        } else if (keyLower === 'max pool size') {
          details.maxPoolSize = parseInt(value.trim(), 10);
        } else if (keyLower === 'connection timeout' || keyLower === 'timeout') {
          details.timeout = parseInt(value.trim(), 10);
        } else if (keyLower === 'encrypt') {
          details.encrypt = value.trim().toLowerCase() === 'true';
        } else if (keyLower === 'trustservercertificate') {
          details.trustServerCertificate = value.trim().toLowerCase() === 'true';
        } else {
          // Collect any other parameters
          additionalParams.push(`${key}=${value}`);
        }
      });
      
      details.additionalParameters = additionalParams.join(';');
      return details;
    } catch (error) {
      console.error('Error parsing connection string', error);
      return details;
    }
  };

  // Build connection string from details
  const buildConnectionString = () => {
    if (connectionMode === 'string') {
      return formData.connectionString;
    }
    
    const { server, port, database, username, password, minPoolSize, maxPoolSize, timeout, encrypt, trustServerCertificate } = connectionDetails;
    
    let connectionStr = `Server=${server}`;
    if (port) {
      connectionStr += `,${port}`;
    }
    
    if (database) {
      connectionStr += `;Database=${database}`;
    }
    
    if (username) {
      connectionStr += `;User ID=${username}`;
    }
    
    if (password) {
      connectionStr += `;Password=${password}`;
    }
    
    if (minPoolSize !== null) {
      connectionStr += `;Min Pool Size=${minPoolSize}`;
    }
    
    if (maxPoolSize !== null) {
      connectionStr += `;Max Pool Size=${maxPoolSize}`;
    }
    
    if (timeout !== null) {
      connectionStr += `;Connection Timeout=${timeout}`;
    }
    
    if (encrypt !== null) {
      connectionStr += `;Encrypt=${encrypt}`;
    }
    
    if (trustServerCertificate !== null) {
      connectionStr += `;TrustServerCertificate=${trustServerCertificate}`;
    }
    
    if (connectionDetails.additionalParameters) {
      connectionStr += `;${connectionDetails.additionalParameters}`;
    }
    
    return connectionStr;
  };

  // Handle form field changes
  const handleChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value, type, checked } = event.target;
    
    setFormData(prev => ({
      ...prev,
      [name]: type === 'checkbox' ? checked : value
    }));
  };

  // Handle connection details field changes
  const handleDetailsChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value, type, checked } = event.target;
    let parsedValue: any = value;
    
    // Parse numeric values
    if (name === 'port' || name === 'minPoolSize' || name === 'maxPoolSize' || name === 'timeout') {
      parsedValue = value === '' ? null : parseInt(value, 10);
    }
    
    setConnectionDetails(prev => ({
      ...prev,
      [name]: type === 'checkbox' ? checked : parsedValue
    }));
  };

  // Handle connection string field changes
  const handleConnectionStringChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const connectionString = event.target.value;
    
    setFormData(prev => ({
      ...prev,
      connectionString
    }));
    
    // Try to parse and update details
    try {
      if (connectionString.trim()) {
        const details = parseConnectionString(connectionString);
        setConnectionDetails(prev => ({
          ...prev,
          ...details
        }));
      }
    } catch (error) {
      console.error('Failed to parse connection string', error);
    }
  };

  // Handle connection mode changes
  const handleConnectionModeChange = (event: React.SyntheticEvent, newMode: 'string' | 'details') => {
    setConnectionMode(newMode);
    
    if (newMode === 'string') {
      // Update connection string based on details
      const connectionString = buildConnectionString();
      setFormData(prev => ({
        ...prev,
        connectionString
      }));
    } else {
      // Try to parse connection string to details
      try {
        const details = parseConnectionString(formData.connectionString);
        setConnectionDetails(prev => ({
          ...prev,
          ...details
        }));
      } catch (error) {
        console.error('Failed to parse connection string', error);
      }
    }
  };

  // Handle access level changes
  const handleAccessLevelChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const accessLevel = event.target.value;
    
    setFormData(prev => ({
      ...prev,
      connectionAccessLevel: accessLevel,
      isSource: accessLevel === 'ReadOnly' || accessLevel === 'ReadWrite',
      isDestination: accessLevel === 'WriteOnly' || accessLevel === 'ReadWrite'
    }));
  };

  // Test the connection
  const testConnection = async () => {
    setIsTesting(true);
    setTestResult(null);
    
    try {
      // Get final connection string based on mode
      const connectionString = connectionMode === 'string' 
        ? formData.connectionString 
        : buildConnectionString();
      
      const response = await DataTransferService.testConnection(connectionString);
      
      setTestResult(response);
      setConnectionTested(true);
      console.log('Test connection response:', response);
    } catch (error) {
      console.error('Test connection error:', error);
      setTestResult({
        success: false,
        message: 'Connection test failed',
        detailedError: error instanceof Error ? error.message : String(error)
      });
    } finally {
      setIsTesting(false);
    }
  };

  // Load database schema
  const loadDatabaseSchema = async () => {
    if (!connectionTested || !testResult?.success) {
      return;
    }
    
    setIsLoadingSchema(true);
    
    try {
      // Get final connection string based on mode
      const connectionString = connectionMode === 'string' 
        ? formData.connectionString 
        : buildConnectionString();
      
      const schema = await DataTransferService.getDatabaseSchema(connectionString);
      setDbSchema(schema);
      setSchemaDialogOpen(true);
    } catch (error) {
      console.error('Failed to load schema:', error);
    } finally {
      setIsLoadingSchema(false);
    }
  };

  // Prepare final connection data for saving
  const prepareConnectionData = (): Connection => {
    // Get final connection string based on mode
    const connectionString = connectionMode === 'string' 
      ? formData.connectionString 
      : buildConnectionString();
    
    // Combine all data
    const finalData: Connection = {
      connectionId: formData.connectionId,
      connectionName: formData.connectionName,
      connectionString: connectionString,
      connectionAccessLevel: formData.connectionAccessLevel,
      description: formData.description,
      server: connectionDetails.server,
      port: connectionDetails.port,
      database: connectionDetails.database,
      username: connectionDetails.username,
      password: connectionDetails.password,
      additionalParameters: connectionDetails.additionalParameters,
      isActive: formData.isActive,
      isConnectionValid: testResult?.success || null,
      minPoolSize: formData.minPoolSize,
      maxPoolSize: formData.maxPoolSize,
      timeout: formData.timeout,
      trustServerCertificate: formData.trustServerCertificate,
      encrypt: formData.encrypt,
      createdBy: formData.createdBy,
      createdOn: formData.createdOn,
      lastModifiedBy: formData.lastModifiedBy,
      lastModifiedOn: new Date().toISOString(),
      lastTestedOn: testResult ? new Date().toISOString() : null,
      isSource: formData.isSource,
      isDestination: formData.isDestination
    };
    
    return finalData;
  };

  return {
    formData,
    connectionDetails,
    connectionMode,
    isTesting,
    testResult,
    connectionTested,
    isLoadingSchema,
    dbSchema,
    schemaDialogOpen,
    setFormData,
    setConnectionDetails,
    setSchemaDialogOpen,
    handleChange,
    handleDetailsChange,
    handleConnectionStringChange,
    handleConnectionModeChange,
    handleAccessLevelChange,
    testConnection,
    loadDatabaseSchema,
    prepareConnectionData,
    parseConnectionString,
    buildConnectionString
  };
}

export default useConnectionForm;