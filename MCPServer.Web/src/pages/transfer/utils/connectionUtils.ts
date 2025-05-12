import { Connection } from '../types/ConnectionTypes';

/**
 * Parses a connection string into a Connection object
 */
export const parseConnectionString = (connectionString: string): Connection => {
  try {
    const details: Connection = {
      connectionId: 0,
      connectionName: '',
      connectionString: connectionString,
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
    };

    const params = connectionString.split(';');
    params.forEach(param => {
      const [key, value] = param.split('=');
      if (!key || !value) return;

      const keyLower = key.trim().toLowerCase();
      const valueClean = value.trim();

      if (keyLower === 'server' || keyLower === 'data source') {
        details.server = valueClean;
      } else if (keyLower === 'database' || keyLower === 'initial catalog') {
        details.database = valueClean;
      } else if (keyLower === 'user id' || keyLower === 'uid') {
        details.username = valueClean;
      } else if (keyLower === 'password' || keyLower === 'pwd') {
        details.password = valueClean;
      } else if (keyLower === 'port') {
        details.port = Number(valueClean);
      } else if (keyLower === 'min pool size') {
        details.minPoolSize = Number(valueClean);
      } else if (keyLower === 'max pool size') {
        details.maxPoolSize = Number(valueClean);
      } else if (keyLower === 'connection timeout') {
        details.timeout = Number(valueClean);
      } else if (keyLower === 'encrypt') {
        details.encrypt = valueClean.toLowerCase() === 'true';
      } else if (keyLower === 'trustservercertificate') {
        details.trustServerCertificate = valueClean.toLowerCase() === 'true';
      }
    });

    return details;
  } catch (error) {
    console.error('Error parsing connection string:', error);
    return {
      connectionId: 0,
      connectionName: '',
      connectionString: connectionString,
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
    };
  }
};

/**
 * Builds a connection string from a Connection object
 */
export const buildConnectionString = (
  connectionDetails: Connection, 
  connectionMode: 'string' | 'details', 
  connectionType: string = 'sqlServer'
): string => {
  if (connectionMode === 'string') {
    // Just return the connection string as is
    return connectionDetails.connectionString;
  }

  // For details mode, build the connection string with all parameters
  const { server, database, username, password, port, additionalParameters } = connectionDetails;
  let connectionString = '';

  if (connectionType === 'sqlServer') {
    // Always include username and password
    connectionString = `Server=${server}${port ? ',' + port : ''};Database=${database};User ID=${username};Password=${password};`;

    if (connectionDetails.encrypt) {
      connectionString += 'Encrypt=True;';
    }

    if (connectionDetails.trustServerCertificate) {
      connectionString += 'TrustServerCertificate=True;';
    }

    if (connectionDetails.timeout) {
      connectionString += `Connection Timeout=${connectionDetails.timeout};`;
    }

    if (additionalParameters) {
      connectionString += additionalParameters;
    }
  } else if (connectionType === 'mysql') {
    // Always include username and password
    connectionString = `Server=${server};Database=${database};User ID=${username};Password=${password};`;

    if (port) {
      connectionString += `Port=${port};`;
    }

    if (connectionDetails.timeout) {
      connectionString += `Connection Timeout=${connectionDetails.timeout};`;
    }

    if (additionalParameters) {
      connectionString += additionalParameters;
    }
  }

  return connectionString;
};