import axios from 'axios';

// Create an instance of axios with default config
const api = axios.create({
  baseURL: (import.meta.env.VITE_API_URL || 'http://localhost:2000') + '/api',
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json'
  }
});

// Request interceptor to add auth token
api.interceptors.request.use(
  (config) => {
    // Get token from localStorage instead of Redux store
    const token = localStorage.getItem('token');

    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }

    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Response interceptor for error handling
api.interceptors.response.use(
  (response) => {
    return response;
  },
  (error) => {
    // Handle 401 Unauthorized errors by redirecting to login
    if (error.response && error.response.status === 401) {
      // Check if we're not already on the login page
      if (window.location.pathname !== '/login') {
        window.location.href = '/login';
      }
    }

    // Handle 403 Forbidden errors
    if (error.response && error.response.status === 403) {
      console.error('Access denied:', error.response.data);
      // Optionally redirect to an access denied page
      // window.location.href = '/unauthorized';
    }

    // Customize error message based on server response
    if (error.response && error.response.data) {
      if (error.response.data.message) {
        error.message = error.response.data.message;
      } else if (error.response.data.error) {
        error.message = error.response.data.error;
      }
    }

    return Promise.reject(error);
  }
);

// Add logging in development mode
if (import.meta.env.DEV) {
  api.interceptors.request.use(request => {
    console.log('API Request:', request.method?.toUpperCase(),
      (request.baseURL || '') + (request.url || ''));
    return request;
  });

  api.interceptors.response.use(response => {
    console.log('API Response:', response.status, response.config?.url);
    return response;
  }, error => {
    console.error('API Error:', error.message, error.response?.status, error.config?.url);
    return Promise.reject(error);
  });
}

export default api;