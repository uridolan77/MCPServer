// Configuration file for API settings and other environment variables

// API Base URL - updated to use port 2000 instead of 5000
export const API_BASE_URL = 'http://localhost:2000/api';

// Other global configuration constants can be added here
export const AUTH_STORAGE_KEY = 'auth_token';
export const REFRESH_TOKEN_KEY = 'refresh_token';
export const USER_DATA_KEY = 'user_data';

// Feature flags
export const FEATURES = {
  enableRagSearch: true,
  enableMultiModel: true,
  enableSessionHistory: true,
};

// You can add environment-specific configurations here
const isProduction = import.meta.env.PROD;
export const CONFIG = {
  apiBaseUrl: isProduction ? '/api' : API_BASE_URL,
  apiTimeout: 30000, // 30 seconds
  enableLogging: !isProduction,
  cacheLifetime: 15 * 60 * 1000, // 15 minutes in milliseconds
};