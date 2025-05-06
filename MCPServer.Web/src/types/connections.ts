export interface DatabaseConnection {
  connectionId?: number;
  connectionName: string;
  connectionString?: string;
  server: string;
  database: string;
  username?: string;
  password?: string;
  port?: string;
  connectionType: 'SQL Server' | 'MySQL' | 'PostgreSQL';
  authType?: 'SQL' | 'Windows';
  maxPoolSize?: number;
  minPoolSize?: number;
  timeout?: number;
  encrypt?: boolean;
  trustServerCertificate?: boolean;
  isActive?: boolean;
  connectionAccessLevel?: 'ReadOnly' | 'WriteOnly' | 'ReadWrite';
  description?: string;
}