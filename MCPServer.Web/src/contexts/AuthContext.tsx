import React, { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import { authApi, User, AuthResponse } from '@/api';
import { getUserFromToken, isTokenValid } from '@/api/apiClient';

interface AuthContextType {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (username: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  register: (username: string, email: string, password: string, firstName?: string, lastName?: string) => Promise<void>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<User | null>(null);
  const [isAuthenticated, setIsAuthenticated] = useState<boolean>(false);
  const [isLoading, setIsLoading] = useState<boolean>(true);

  useEffect(() => {
    const initAuth = async () => {
      try {
        // Check if token is valid
        if (isTokenValid()) {
          // Get user info from token
          const tokenUser = getUserFromToken();

          if (tokenUser) {
            try {
              // Get full user details from API
              const userDetails = await authApi.getCurrentUser();
              setUser(userDetails);
              setIsAuthenticated(true);
            } catch (error) {
              console.error('Failed to get user details:', error);
              // Clear invalid tokens
              await logout();
            }
          } else {
            // Clear invalid tokens
            await logout();
          }
        } else {
          // Clear invalid tokens
          await logout();
        }
      } catch (error) {
        console.error('Auth initialization error:', error);
        await logout();
      } finally {
        setIsLoading(false);
      }
    };

    initAuth();
  }, []);

  const handleAuthResponse = (response: AuthResponse) => {
    console.log('Handling auth response:', response);

    // Store tokens
    localStorage.setItem('token', response.accessToken);
    localStorage.setItem('refreshToken', response.refreshToken);

    // Extract roles from response
    const roles = Array.isArray(response.roles)
      ? response.roles
      : (response.roles?.$values || []);

    console.log('Extracted roles:', roles);

    // Set user info
    setUser({
      id: '', // Will be filled when we get full user details
      username: response.username,
      email: response.email,
      roles: roles,
      isActive: true,
      createdAt: new Date().toISOString(),
      lastLoginAt: new Date().toISOString()
    });

    setIsAuthenticated(true);
    console.log('User authenticated successfully');
  };

  const login = async (username: string, password: string) => {
    setIsLoading(true);
    try {
      console.log('AuthContext: Attempting login for user:', username);

      const response = await authApi.login({ username, password });
      console.log('AuthContext: Login successful, response:', response);

      handleAuthResponse(response);

      try {
        // Get full user details
        console.log('AuthContext: Fetching user details');
        const userDetails = await authApi.getCurrentUser();
        console.log('AuthContext: User details received:', userDetails);
        setUser(userDetails);
      } catch (userDetailsError) {
        console.error('AuthContext: Failed to get user details, but login was successful:', userDetailsError);
        // Continue with basic user info from the login response
      }
    } catch (error: any) {
      console.error('AuthContext: Login failed:', error);

      // Clear any existing auth data on login failure
      localStorage.removeItem('token');
      localStorage.removeItem('refreshToken');
      localStorage.removeItem('user');
      setUser(null);
      setIsAuthenticated(false);

      throw error;
    } finally {
      setIsLoading(false);
    }
  };

  const logout = async () => {
    setIsLoading(true);
    try {
      await authApi.logout();
      setUser(null);
      setIsAuthenticated(false);
    } catch (error) {
      console.error('Logout failed:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const register = async (username: string, email: string, password: string, firstName?: string, lastName?: string) => {
    setIsLoading(true);
    try {
      const response = await authApi.register({ username, email, password, firstName, lastName });
      handleAuthResponse(response);

      // Get full user details
      const userDetails = await authApi.getCurrentUser();
      setUser(userDetails);
    } catch (error) {
      console.error('Registration failed:', error);
      throw error;
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <AuthContext.Provider value={{ user, isAuthenticated, isLoading, login, logout, register }}>
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = (): AuthContextType => {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};
