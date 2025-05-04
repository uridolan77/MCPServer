import apiClient from './apiClient';

export interface LoginRequest {
  username: string;
  password: string;
}

export interface RegisterRequest {
  username: string;
  email: string;
  password: string;
  firstName?: string;
  lastName?: string;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
  username: string;
  email: string;
  roles: string[] | { $id: string; $values: string[] };
}

export interface User {
  id: string;
  username: string;
  email: string;
  firstName?: string;
  lastName?: string;
  roles: string[];
  isActive: boolean;
  createdAt: string;
  lastLoginAt: string;
}

// Mock user data for development
const mockUsers: User[] = [
  {
    id: '1',
    username: 'admin',
    email: 'admin@example.com',
    firstName: 'Admin',
    lastName: 'User',
    roles: ['Admin'],
    isActive: true,
    createdAt: '2023-01-01T00:00:00Z',
    lastLoginAt: '2023-05-01T00:00:00Z'
  },
  {
    id: '2',
    username: 'user',
    email: 'user@example.com',
    firstName: 'Regular',
    lastName: 'User',
    roles: ['User'],
    isActive: true,
    createdAt: '2023-01-02T00:00:00Z',
    lastLoginAt: '2023-05-02T00:00:00Z'
  }
];

// Helper function to generate a JWT-like token (not a real JWT)
const generateMockToken = (user: User): string => {
  const payload = {
    sub: user.id,
    name: user.username,
    email: user.email,
    role: user.roles,
    exp: Math.floor(Date.now() / 1000) + 3600 // Expires in 1 hour
  };
  return btoa(JSON.stringify(payload));
};

export const authApi = {
  login: async (data: LoginRequest): Promise<AuthResponse> => {
    try {
      console.log('Sending login request to API:', { username: data.username });

      // Make the API call
      const response = await apiClient.post<any>('/auth/login', data);
      console.log('Login API response:', response);

      // Check if the response has the expected structure
      if (response.data && response.data.data) {
        // The API is returning data wrapped in an ApiResponse object
        console.log('Unwrapping API response data:', response.data.data);
        return response.data.data;
      } else if (response.data && response.data.accessToken) {
        // Direct response with auth data
        console.log('Using direct API response data');
        return response.data;
      } else {
        console.error('Unexpected API response format:', response.data);
        throw new Error('Unexpected API response format');
      }
    } catch (error: any) {
      console.error('Login API error:', error);
      console.error('Error details:', {
        message: error.message,
        response: error.response,
        data: error.response?.data
      });

      if (error.response?.status === 401) {
        throw new Error('Invalid username or password');
      } else if (error.response?.data?.message) {
        throw new Error(error.response.data.message);
      } else if (error.response?.data?.errors && error.response.data.errors.length > 0) {
        throw new Error(error.response.data.errors.join(', '));
      }
      throw error;
    }
  },

  register: async (data: RegisterRequest): Promise<AuthResponse> => {
    try {
      const response = await apiClient.post<AuthResponse>('/auth/register', data);
      return response.data;
    } catch (error: any) {
      if (error.response?.status === 400) {
        throw new Error(error.response.data.message || 'Username or email already exists');
      }
      throw error;
    }
  },

  refreshToken: async (refreshToken: string): Promise<AuthResponse> => {
    try {
      const response = await apiClient.post<AuthResponse>('/auth/refresh', { refreshToken });
      return response.data;
    } catch (error: any) {
      if (error.response?.status === 401) {
        throw new Error('Invalid refresh token');
      }
      throw error;
    }
  },

  getCurrentUser: async (): Promise<User> => {
    try {
      console.log('Fetching current user data');
      const response = await apiClient.get<any>('/auth/me');
      console.log('Current user API response:', response);

      // Handle different response formats
      let userData;

      if (response.data && response.data.data) {
        // Wrapped in ApiResponse
        console.log('Unwrapping user data from ApiResponse');
        userData = response.data.data;
      } else {
        // Direct response
        console.log('Using direct user data response');
        userData = response.data;
      }

      // Handle roles format
      if (userData.roles && !Array.isArray(userData.roles) && userData.roles.$values) {
        console.log('Converting roles from $values format');
        userData.roles = userData.roles.$values;
      }

      console.log('Processed user data:', userData);
      return userData as User;
    } catch (error: any) {
      console.error('Error fetching current user:', error);
      console.error('Error details:', {
        message: error.message,
        response: error.response,
        data: error.response?.data
      });

      if (error.response?.status === 401) {
        throw new Error('Not authenticated');
      } else if (error.response?.data?.message) {
        throw new Error(error.response.data.message);
      }
      throw error;
    }
  },

  logout: async (): Promise<void> => {
    try {
      const refreshToken = localStorage.getItem('refreshToken');
      if (refreshToken) {
        await apiClient.post('/auth/revoke', { refreshToken });
      }
    } catch (error) {
      console.error('Error revoking token:', error);
    } finally {
      localStorage.removeItem('token');
      localStorage.removeItem('refreshToken');
      localStorage.removeItem('user');
    }
  }
};
