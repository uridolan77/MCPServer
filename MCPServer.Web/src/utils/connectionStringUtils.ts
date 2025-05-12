// Utility functions for working with database connection strings

interface ConnectionDetails {
  server: string;
  port: number | null;
  database: string;
  username: string;
  password: string;
  additionalParameters: string;
  encrypt?: boolean | null;
  trustServerCertificate?: boolean | null;
  timeout?: number | null;
  minPoolSize?: number | null;
  maxPoolSize?: number | null;
}

/**
 * Parse a connection string into its component parts
 */
export const parseConnectionString = (connectionString: string): ConnectionDetails => {
  const details: ConnectionDetails = {
    server: '',
    port: null,
    database: '',
    username: '',
    password: '',
    additionalParameters: '',
    encrypt: null,
    trustServerCertificate: null,
    timeout: null
  };

  try {
    const params = connectionString.split(';');
    let additionalParams: string[] = [];

    params.forEach(param => {
      const parts = param.split('=');
      if (parts.length < 2) return;
      
      const key = parts[0].trim().toLowerCase();
      const value = parts.slice(1).join('=').trim(); // Handle values that might contain =

      if (key === 'server' || key === 'data source') {
        // Check if the server includes a port number (server,port format)
        const serverParts = value.split(',');
        details.server = serverParts[0];
        if (serverParts.length > 1 && serverParts[1]) {
          details.port = parseInt(serverParts[1], 10);
        }
      } else if (key === 'database' || key === 'initial catalog') {
        details.database = value;
      } else if (key === 'user id' || key === 'uid') {
        details.username = value;
      } else if (key === 'password' || key === 'pwd') {
        details.password = value;
      } else if (key === 'port') {
        details.port = parseInt(value, 10);
      } else if (key === 'encrypt') {
        details.encrypt = value.toLowerCase() === 'true';
      } else if (key === 'trustservercertificate') {
        details.trustServerCertificate = value.toLowerCase() === 'true';
      } else if (key === 'connection timeout' || key === 'timeout') {
        details.timeout = parseInt(value, 10);
      } else if (key === 'min pool size') {
        details.minPoolSize = parseInt(value, 10);
      } else if (key === 'max pool size') {
        details.maxPoolSize = parseInt(value, 10);
      } else {
        // Save any other parameters
        additionalParams.push(`${parts[0]}=${value}`);
      }
    });

    if (additionalParams.length > 0) {
      details.additionalParameters = additionalParams.join(';');
    }
  } catch (error) {
    console.error('Error parsing connection string:', error);
  }

  return details;
};

/**
 * Build a connection string from component parts
 */
export const buildConnectionString = (
  details: ConnectionDetails,
  connectionType: 'sqlServer' | 'mysql' = 'sqlServer'
): string => {
  const { server, database, username, password, port, additionalParameters } = details;
  let connectionString = '';

  if (connectionType === 'sqlServer') {
    // Build SQL Server connection string
    connectionString = `Server=${server}${port ? ',' + port : ''};Database=${database};User ID=${username};Password=${password};`;

    if (details.encrypt) {
      connectionString += 'Encrypt=True;';
    }

    if (details.trustServerCertificate) {
      connectionString += 'TrustServerCertificate=True;';
    }

    if (details.timeout) {
      connectionString += `Connection Timeout=${details.timeout};`;
    }

    if (details.minPoolSize) {
      connectionString += `Min Pool Size=${details.minPoolSize};`;
    }

    if (details.maxPoolSize) {
      connectionString += `Max Pool Size=${details.maxPoolSize};`;
    }

    if (additionalParameters) {
      connectionString += additionalParameters;
    }
  } else if (connectionType === 'mysql') {
    // Build MySQL connection string
    connectionString = `Server=${server};Database=${database};User ID=${username};Password=${password};`;

    if (port) {
      connectionString += `Port=${port};`;
    }

    if (details.timeout) {
      connectionString += `Connection Timeout=${details.timeout};`;
    }

    if (additionalParameters) {
      connectionString += additionalParameters;
    }
  }

  return connectionString;
};

/**
 * Ensure that a connection string has username and password included
 */
export const ensureCredentialsInConnectionString = (
  connectionString: string,
  username: string,
  password: string
): string => {
  if (!connectionString) return '';
  let updatedString = connectionString;

  // Make sure username and password are in the string
  if (username && password) {
    // Check if they're already in the string
    const hasUserId = /User ID=/i.test(connectionString);
    const hasPassword = /Password=/i.test(connectionString);

    // If not, add them
    if (!hasUserId) {
      updatedString += `;User ID=${username}`;
    }
    if (!hasPassword) {
      updatedString += `;Password=${password}`;
    }
  }

  return updatedString;
};