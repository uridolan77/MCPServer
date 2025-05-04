import axios, { AxiosError, AxiosInstance, AxiosRequestConfig, AxiosResponse } from 'axios';
import { jwtDecode } from 'jwt-decode';

// Define token interface
interface DecodedToken {
  exp: number;
  sub: string;
  role: string | string[];
  name: string;
  email: string;
}

// Create axios instance
const apiClient: AxiosInstance = axios.create({
  baseURL: '/api', // Use relative URL to avoid CORS issues with the proxy
  headers: {
    'Content-Type': 'application/json',
  },
});

// Add request interceptor to add auth token
apiClient.interceptors.request.use(
  async (config) => {
    const token = localStorage.getItem('token');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }

    // Log requests in development
    if (process.env.NODE_ENV === 'development') {
      console.log(`API Request [${config.method?.toUpperCase()}] ${config.url}`, config.params || '');
    }

    return config;
  },
  (error) => {
    console.error('Request error:', error);
    return Promise.reject(error);
  }
);

// Add response interceptor to handle token refresh
apiClient.interceptors.response.use(
  (response) => {
    // Log successful responses in development
    if (process.env.NODE_ENV === 'development') {
      console.log(`API Response [${response.config.method?.toUpperCase()}] ${response.config.url}:`, response.data);
    }
    return response;
  },
  async (error: AxiosError) => {
    // Log API errors in development
    if (process.env.NODE_ENV === 'development') {
      console.error('API Error:', {
        status: error.response?.status,
        statusText: error.response?.statusText,
        url: error.config?.url,
        method: error.config?.method?.toUpperCase(),
        data: error.response?.data
      });
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
