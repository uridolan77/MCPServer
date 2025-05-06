import axios from 'axios';

// Create an Axios instance with custom configuration
const instance = axios.create({
  baseURL: import.meta.env.VITE_API_URL || 'http://localhost:2000/api', // Default API URL with /api prefix
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: 30000, // 30 seconds
});

// Add a request interceptor to include the auth token in all requests
instance.interceptors.request.use(
  (config) => {
    // Get the token from localStorage
    const token = localStorage.getItem('access_token');

    // If token exists, add it to the request headers
    if (token) {
      config.headers['Authorization'] = `Bearer ${token}`;
    }

    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Add a response interceptor to handle common error cases
instance.interceptors.response.use(
  (response) => {
    // Any status code within the range of 2xx causes this function to trigger
    return response;
  },
  (error) => {
    // Handle specific error status codes
    if (error.response) {
      // Server returned an error response
      const { status } = error.response;

      if (status === 401) {
        // Unauthorized - clear token and redirect to login
        localStorage.removeItem('access_token');
        localStorage.removeItem('user');

        // Only redirect if we're not already on the login page
        if (!window.location.pathname.includes('/login')) {
          window.location.href = '/login';
        }
      }

      if (status === 403) {
        // Forbidden - user doesn't have permission
        console.error('Access forbidden. You do not have permission to access this resource.');
      }
    } else if (error.request) {
      // The request was made but no response was received
      console.error('Network error. Please check your connection and try again.');
    }

    // Pass the error through to the calling function
    return Promise.reject(error);
  }
);

export default instance;