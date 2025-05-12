/**
 * Connection interface definitions
 * Single source of truth for connection-related types
 * Field names exactly match the server-side DataTransferConnection.cs model
 */

/**
 * Main Connection interface matching the server DataTransferConnection model
 * This unified interface replaces ConnectionFormData and ConnectionDetails
 */
export interface Connection {
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

/**
 * Connection test result interface
 */
export interface ConnectionTestResult {
  success: boolean;
  message: string;
  detailedError?: string;
  server?: string;
  database?: string;
  errorCode?: number;
  errorType?: string;
  innerException?: string;
}

/**
 * Connection access level enum (matching server-side enum)
 */
export enum ConnectionAccessLevel {
  ReadOnly = 'ReadOnly',
  WriteOnly = 'WriteOnly',
  ReadWrite = 'ReadWrite'
}