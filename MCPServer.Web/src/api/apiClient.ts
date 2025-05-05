import axios, { AxiosInstance, AxiosRequestConfig, AxiosResponse } from 'axios';
import { jwtDecode } from 'jwt-decode';
import { API_BASE_URL } from '@/config';

// Define token interface
interface DecodedToken {
  exp: number;
  sub: string;
  role: string | string[];
  name: string;
  email: string;
}

// Create a customized axios instance
const apiClient: AxiosInstance = axios.create({
  baseURL: API_BASE_URL,
  timeout: 30000, // 30 seconds
  headers: {
    'Content-Type': 'application/json',
  },
});

// Log all requests in development mode
apiClient.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('token');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }

    if (import.meta.env.DEV) {
      console.log(`API Request: ${config.method?.toUpperCase()} ${config.baseURL}${config.url}`, 
        config.params ? `params: ${JSON.stringify(config.params)}` : '',
        config.data ? `data: ${JSON.stringify(config.data)}` : '');
    }
    return config;
  },
  (error) => {
    console.error('Request error:', error);
    return Promise.reject(error);
  }
);

// Log responses and handle errors
apiClient.interceptors.response.use(
  (response: AxiosResponse) => {
    if (import.meta.env.DEV) {
      console.log(`API Response from ${response.config.url}:`, 
        response.data ? JSON.stringify(response.data).substring(0, 1000) + '...' : 'No data');

      // Special debugging for chat logs endpoint
      if (response.config.url?.includes('chat-usage/logs') || response.config.url?.includes('usage/logs')) {
        console.log('DETAILED CHAT LOGS RESPONSE:', response.data);
        
        // Check different data structures
        if (response.data && response.data.data && response.data.data.$values) {
          console.log('Found nested $values array with', response.data.data.$values.length, 'items');
        } else if (response.data && response.data.$values) {
          console.log('Found direct $values array with', response.data.$values.length, 'items');
        } else if (Array.isArray(response.data)) {
          console.log('Response data is a direct array with', response.data.length, 'items');
        }
      }
    }
    return response;
  },
  async (error) => {
    // Log the error
    if (error.response) {
      console.error('API Error Response:', error.response.status, error.response.data);
    } else if (error.request) {
      console.error('API No Response:', error.request);
    } else {
      console.error('API Request Error:', error.message);
    }

    const originalRequest = error.config as AxiosRequestConfig & { _retry?: boolean };

    // If error is 401 and we haven't retried yet
    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true;

      try {
        // Try to refresh the token
        const refreshToken = localStorage.getItem('refreshToken');
        if (!refreshToken) {
          throw new Error('No refresh token available');
        }

        console.log('Attempting to refresh token');
        const response = await axios.post('/auth/refresh', { refreshToken });
        console.log('Token refresh response:', response);

        // Handle different response formats
        let accessToken, newRefreshToken;

        if (response.data && response.data.data) {
          // Wrapped in ApiResponse
          accessToken = response.data.data.accessToken;
          newRefreshToken = response.data.data.refreshToken;
        } else if (response.data && response.data.accessToken) {
          // Direct response
          accessToken = response.data.accessToken;
          newRefreshToken = response.data.refreshToken;
        } else {
          console.error('Unexpected refresh token response format:', response.data);
          throw new Error('Invalid refresh token response format');
        }

        // Store new tokens
        localStorage.setItem('token', accessToken);
        localStorage.setItem('refreshToken', newRefreshToken);

        // Update auth header and retry
        apiClient.defaults.headers.common.Authorization = `Bearer ${accessToken}`;
        return apiClient(originalRequest);
      } catch (refreshError) {
        // If refresh fails, logout
        localStorage.removeItem('token');
        localStorage.removeItem('refreshToken');
        localStorage.removeItem('user');

        // Redirect to login
        window.location.href = '/login';
        return Promise.reject(refreshError);
      }
    }
    
    // For 404 errors, log a clearer message to help with debugging
    if (error.response?.status === 404) {
      console.warn(`Resource not found (404): ${error.config?.url}`);
      // Don't suppress the error, still let it propagate
    }

    return Promise.reject(error);
  }
);

// Helper functions
export const isTokenValid = (): boolean => {
  const token = localStorage.getItem('token');
  if (!token) return false;

  try {
    const decoded = jwtDecode<DecodedToken>(token);
    const currentTime = Date.now() / 1000;
    return decoded.exp > currentTime;
  } catch (error) {
    return false;
  }
};

export const getUserFromToken = (): { id: string; username: string; email: string; roles: string[] } | null => {
  const token = localStorage.getItem('token');
  if (!token) return null;

  try {
    const decoded = jwtDecode<DecodedToken>(token);
    return {
      id: decoded.sub,
      username: decoded.name,
      email: decoded.email,
      roles: Array.isArray(decoded.role) ? decoded.role : [decoded.role],
    };
  } catch (error) {
    return null;
  }
};

export default apiClient;
