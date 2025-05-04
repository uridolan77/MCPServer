import apiClient from './apiClient';
import { User } from './authApi';

export interface UserCreateRequest {
  username: string;
  email: string;
  password: string;
  firstName?: string;
  lastName?: string;
  roles: string[];
  isActive: boolean;
}

export interface UserUpdateRequest {
  email?: string;
  firstName?: string;
  lastName?: string;
  roles?: string[];
  isActive?: boolean;
}

export const userApi = {
  getAllUsers: async (): Promise<User[]> => {
    const response = await apiClient.get<User[]>('/users');
    return response.data;
  },
  
  getUserById: async (id: string): Promise<User> => {
    const response = await apiClient.get<User>(`/users/${id}`);
    return response.data;
  },
  
  createUser: async (user: UserCreateRequest): Promise<User> => {
    const response = await apiClient.post<User>('/users', user);
    return response.data;
  },
  
  updateUser: async (id: string, user: UserUpdateRequest): Promise<void> => {
    await apiClient.put(`/users/${id}`, user);
  },
  
  deleteUser: async (id: string): Promise<void> => {
    await apiClient.delete(`/users/${id}`);
  },
  
  changePassword: async (id: string, currentPassword: string, newPassword: string): Promise<void> => {
    await apiClient.post(`/users/${id}/change-password`, {
      currentPassword,
      newPassword
    });
  },
  
  resetPassword: async (id: string): Promise<{ temporaryPassword: string }> => {
    const response = await apiClient.post<{ temporaryPassword: string }>(`/users/${id}/reset-password`);
    return response.data;
  }
};
