import apiClient from './apiClient';

export interface ApiResponse<T> {
  success: boolean;
  data: T;
  message?: string;
  errors?: string[];
}

export interface LlmProvider {
  id: number;
  name: string;
  displayName: string;
  apiEndpoint: string;
  description: string;
  isEnabled: boolean;
  authType: string;
  configSchema: string;
  createdAt: string;
  updatedAt?: string;
}

export interface LlmModel {
  id: number;
  providerId: number;
  name: string;
  modelId: string;
  description: string;
  maxTokens: number;
  contextWindow: number;
  supportsStreaming: boolean;
  supportsVision: boolean;
  supportsTools: boolean;
  costPer1KInputTokens: number;
  costPer1KOutputTokens: number;
  isEnabled: boolean;
  createdAt: string;
  updatedAt?: string;
  provider?: LlmProvider;
}

export interface LlmProviderCredential {
  id: number;
  providerId: number;
  userId?: string;
  name: string;
  apiKey: string;
  isDefault: boolean;
  isEnabled: boolean;
  createdAt: string;
  updatedAt?: string;
  lastUsedAt?: string;
  provider?: LlmProvider;
}

export interface CredentialRequest {
  providerId: number;
  userId?: string;
  name: string;
  isDefault?: boolean;
  credentials: any;
}

export interface LlmUsageLog {
  id: number;
  modelId: number;
  credentialId?: number;
  userId?: string;
  requestTimestamp: string;
  responseTimestamp?: string;
  inputTokens: number;
  outputTokens: number;
  totalTokens: number;
  estimatedCost: number;
  status: string;
  errorMessage?: string;
  model?: LlmModel;
  credential?: LlmProviderCredential;
}

export const llmProviderApi = {
  // Provider endpoints
  getAllProviders: async (): Promise<LlmProvider[]> => {
    try {
      console.log('Fetching all providers...');

      // First check if the API is accessible
      try {
        const healthCheck = await apiClient.get<any>('/health');
        console.log('API health check:', healthCheck.data);
      } catch (healthError) {
        console.warn('API health check failed, but will try to fetch providers anyway:', healthError);
      }

      // Use the new providers endpoint
      const response = await apiClient.get<ApiResponse<LlmProvider[]>>('/llm/providers');
      console.log('Providers API response:', response.data);

      if (response.data && response.data.success && response.data.data) {
        return response.data.data;
      }

      // Fallback to legacy endpoint
      const legacyResponse = await apiClient.get<any>('/llm/providers');

      // Handle response format that might have $values property
      if (legacyResponse.data && legacyResponse.data.$values) {
        console.log('Found $values property, returning:', legacyResponse.data.$values);
        return legacyResponse.data.$values;
      }

      // If it's already an array, return it
      if (Array.isArray(legacyResponse.data)) {
        console.log('Response is an array, returning:', legacyResponse.data);
        return legacyResponse.data;
      }

      // If response.data is an object with providers inside
      if (legacyResponse.data && typeof legacyResponse.data === 'object' && !Array.isArray(legacyResponse.data)) {
        // Try to find an array property
        for (const key in legacyResponse.data) {
          if (Array.isArray(legacyResponse.data[key])) {
            console.log(`Found array in property ${key}, returning:`, legacyResponse.data[key]);
            return legacyResponse.data[key];
          }
        }
      }

      // If we get here, return an empty array
      console.warn('Unexpected providers response format:', legacyResponse.data);
      return [];
    } catch (error) {
      console.error('Error fetching providers:', error);
      return [];
    }
  },

  getProviderById: async (id: number): Promise<LlmProvider | null> => {
    try {
      // Use the new providers endpoint
      const response = await apiClient.get<ApiResponse<LlmProvider>>(`/llm/providers/${id}`);

      if (response.data && response.data.success && response.data.data) {
        return response.data.data;
      }

      // Fallback to legacy endpoint
      const legacyResponse = await apiClient.get<LlmProvider>(`/llm/providers/${id}`);
      return legacyResponse.data;
    } catch (error) {
      console.error(`Error fetching provider with ID ${id}:`, error);
      return null;
    }
  },

  createProvider: async (provider: Omit<LlmProvider, 'id' | 'createdAt' | 'updatedAt'>): Promise<LlmProvider | null> => {
    try {
      // Use the new providers endpoint
      const response = await apiClient.post<ApiResponse<LlmProvider>>('/llm/providers', provider);

      if (response.data && response.data.success && response.data.data) {
        return response.data.data;
      }

      // Fallback to legacy endpoint
      const legacyResponse = await apiClient.post<LlmProvider>('/llm/providers', provider);
      return legacyResponse.data;
    } catch (error) {
      console.error('Error creating provider:', error);
      return null;
    }
  },

  updateProvider: async (id: number, provider: Partial<LlmProvider>): Promise<boolean> => {
    try {
      // Use the new providers endpoint
      const response = await apiClient.put<ApiResponse<LlmProvider>>(`/llm/providers/${id}`, provider);

      if (response.data && response.data.success) {
        return true;
      }

      // Fallback to legacy endpoint
      await apiClient.put(`/llm/providers/${id}`, provider);
      return true;
    } catch (error) {
      console.error(`Error updating provider with ID ${id}:`, error);
      return false;
    }
  },

  deleteProvider: async (id: number): Promise<boolean> => {
    try {
      // Use the new providers endpoint
      const response = await apiClient.delete<ApiResponse<boolean>>(`/llm/providers/${id}`);

      if (response.data && response.data.success) {
        return true;
      }

      // Fallback to legacy endpoint
      await apiClient.delete(`/llm/providers/${id}`);
      return true;
    } catch (error) {
      console.error(`Error deleting provider with ID ${id}:`, error);
      return false;
    }
  },

  // Model endpoints
  getAllModels: async (): Promise<LlmModel[]> => {
    try {
      // Use the new models endpoint
      const response = await apiClient.get<ApiResponse<LlmModel[]>>('/llm/models');

      if (response.data && response.data.success && response.data.data) {
        return response.data.data;
      }

      // Fallback to legacy endpoint
      const legacyResponse = await apiClient.get<any>('/llm/models');

      // Handle response format that might have $values property
      if (legacyResponse.data && legacyResponse.data.$values) {
        return legacyResponse.data.$values;
      }

      // If it's already an array, return it
      if (Array.isArray(legacyResponse.data)) {
        return legacyResponse.data;
      }

      // If we get here, return an empty array
      console.warn('Unexpected models response format:', legacyResponse.data);
      return [];
    } catch (error) {
      console.error('Error fetching models:', error);
      return [];
    }
  },

  getModelById: async (id: number): Promise<LlmModel | null> => {
    try {
      // Use the new models endpoint
      const response = await apiClient.get<ApiResponse<LlmModel>>(`/llm/models/${id}`);

      if (response.data && response.data.success && response.data.data) {
        return response.data.data;
      }

      // Fallback to legacy endpoint
      const legacyResponse = await apiClient.get<LlmModel>(`/llm/models/${id}`);
      return legacyResponse.data;
    } catch (error) {
      console.error(`Error fetching model with ID ${id}:`, error);
      return null;
    }
  },

  getModelsByProviderId: async (providerId: number): Promise<LlmModel[]> => {
    try {
      // Use the new models endpoint
      const response = await apiClient.get<ApiResponse<LlmModel[]>>(`/llm/models/provider/${providerId}`);

      if (response.data && response.data.success && response.data.data) {
        return response.data.data;
      }

      // Fallback to legacy endpoint
      const legacyResponse = await apiClient.get<any>(`/llm/providers/${providerId}/models`);

      // Handle response format that might have $values property
      if (legacyResponse.data && legacyResponse.data.$values) {
        return legacyResponse.data.$values;
      }

      // If it's already an array, return it
      if (Array.isArray(legacyResponse.data)) {
        return legacyResponse.data;
      }

      // If we get here, return an empty array
      console.warn('Unexpected models response format:', legacyResponse.data);
      return [];
    } catch (error) {
      console.error('Error fetching models by provider:', error);
      return [];
    }
  },

  createModel: async (model: Omit<LlmModel, 'id' | 'createdAt' | 'updatedAt' | 'provider'>): Promise<LlmModel | null> => {
    try {
      // Use the new models endpoint
      const response = await apiClient.post<ApiResponse<LlmModel>>('/llm/models', model);

      if (response.data && response.data.success && response.data.data) {
        return response.data.data;
      }

      // Fallback to legacy endpoint
      const legacyResponse = await apiClient.post<LlmModel>('/llm/models', model);
      return legacyResponse.data;
    } catch (error) {
      console.error('Error creating model:', error);
      return null;
    }
  },

  updateModel: async (id: number, model: Partial<LlmModel>): Promise<boolean> => {
    try {
      // Use the new models endpoint
      const response = await apiClient.put<ApiResponse<LlmModel>>(`/llm/models/${id}`, model);

      if (response.data && response.data.success) {
        return true;
      }

      // Fallback to legacy endpoint
      await apiClient.put(`/llm/models/${id}`, model);
      return true;
    } catch (error) {
      console.error(`Error updating model with ID ${id}:`, error);
      return false;
    }
  },

  deleteModel: async (id: number): Promise<boolean> => {
    try {
      // Use the new models endpoint
      const response = await apiClient.delete<ApiResponse<boolean>>(`/llm/models/${id}`);

      if (response.data && response.data.success) {
        return true;
      }

      // Fallback to legacy endpoint
      await apiClient.delete(`/llm/models/${id}`);
      return true;
    } catch (error) {
      console.error(`Error deleting model with ID ${id}:`, error);
      return false;
    }
  },

  // Credential endpoints
  getUserCredentials: async (): Promise<LlmProviderCredential[]> => {
    try {
      // Use the new credentials endpoint
      const response = await apiClient.get<ApiResponse<LlmProviderCredential[]>>('/llm/credentials');

      if (response.data && response.data.success && response.data.data) {
        return response.data.data;
      }

      // Fallback to legacy endpoint
      const legacyResponse = await apiClient.get<any>('/llm/credentials');

      // Handle response format that might have $values property
      if (legacyResponse.data && legacyResponse.data.$values) {
        return legacyResponse.data.$values;
      }

      // If it's already an array, return it
      if (Array.isArray(legacyResponse.data)) {
        return legacyResponse.data;
      }

      // If we get here, return an empty array
      console.warn('Unexpected credentials response format:', legacyResponse.data);
      return [];
    } catch (error) {
      console.error('Error fetching user credentials:', error);
      return [];
    }
  },

  getCredentialsByProviderId: async (providerId: number): Promise<LlmProviderCredential[]> => {
    try {
      // Use the new credentials endpoint
      const response = await apiClient.get<ApiResponse<LlmProviderCredential[]>>(`/llm/credentials/provider/${providerId}`);

      if (response.data && response.data.success && response.data.data) {
        return response.data.data;
      }

      // Fallback to legacy endpoint
      const legacyResponse = await apiClient.get<any>(`/llm/providers/${providerId}/credentials`);

      // Handle response format that might have $values property
      if (legacyResponse.data && legacyResponse.data.$values) {
        return legacyResponse.data.$values;
      }

      // If it's already an array, return it
      if (Array.isArray(legacyResponse.data)) {
        return legacyResponse.data;
      }

      // If we get here, return an empty array
      console.warn('Unexpected credentials response format:', legacyResponse.data);
      return [];
    } catch (error) {
      console.error('Error fetching credentials by provider:', error);
      return [];
    }
  },

  getCredentialById: async (id: number): Promise<LlmProviderCredential | null> => {
    try {
      // Use the new credentials endpoint
      const response = await apiClient.get<ApiResponse<LlmProviderCredential>>(`/llm/credentials/${id}`);

      if (response.data && response.data.success && response.data.data) {
        return response.data.data;
      }

      // Fallback to legacy endpoint
      const legacyResponse = await apiClient.get<LlmProviderCredential>(`/llm/credentials/${id}`);
      return legacyResponse.data;
    } catch (error) {
      console.error(`Error fetching credential with ID ${id}:`, error);
      return null;
    }
  },

  createCredential: async (credential: CredentialRequest): Promise<LlmProviderCredential | null> => {
    try {
      // Use the new credentials endpoint
      const response = await apiClient.post<ApiResponse<LlmProviderCredential>>('/llm/credentials', credential);

      if (response.data && response.data.success && response.data.data) {
        return response.data.data;
      }

      // Fallback to legacy endpoint
      const legacyResponse = await apiClient.post<LlmProviderCredential>('/llm/credentials', credential);
      return legacyResponse.data;
    } catch (error) {
      console.error('Error creating credential:', error);
      return null;
    }
  },

  updateCredential: async (id: number, credential: Partial<CredentialRequest>): Promise<boolean> => {
    try {
      // Use the new credentials endpoint
      const response = await apiClient.put<ApiResponse<LlmProviderCredential>>(`/llm/credentials/${id}`, credential);

      if (response.data && response.data.success) {
        return true;
      }

      // Fallback to legacy endpoint
      await apiClient.put(`/llm/credentials/${id}`, credential);
      return true;
    } catch (error) {
      console.error(`Error updating credential with ID ${id}:`, error);
      return false;
    }
  },

  setDefaultCredential: async (id: number): Promise<boolean> => {
    try {
      // Use the new credentials endpoint
      const response = await apiClient.post<ApiResponse<boolean>>(`/llm/credentials/${id}/set-default`);

      if (response.data && response.data.success) {
        return true;
      }

      // Fallback to legacy endpoint
      await apiClient.post(`/llm/credentials/${id}/set-default`);
      return true;
    } catch (error) {
      console.error(`Error setting default credential with ID ${id}:`, error);
      return false;
    }
  },

  deleteCredential: async (id: number): Promise<boolean> => {
    try {
      // Use the new credentials endpoint
      const response = await apiClient.delete<ApiResponse<boolean>>(`/llm/credentials/${id}`);

      if (response.data && response.data.success) {
        return true;
      }

      // Fallback to legacy endpoint
      await apiClient.delete(`/llm/credentials/${id}`);
      return true;
    } catch (error) {
      console.error(`Error deleting credential with ID ${id}:`, error);
      return false;
    }
  },

  // Usage endpoints
  getUsageByModelId: async (modelId: number, startDate?: Date | string, endDate?: Date | string): Promise<LlmUsageLog[]> => {
    try {
      let url = `/llm/models/${modelId}/usage`;
      const params = new URLSearchParams();

      if (startDate) {
        params.append('startDate', typeof startDate === 'string' ? startDate : startDate.toISOString());
      }

      if (endDate) {
        params.append('endDate', typeof endDate === 'string' ? endDate : endDate.toISOString());
      }

      if (params.toString()) {
        url += `?${params.toString()}`;
      }

      const response = await apiClient.get<any>(url);

      // Handle response format that might have $values property
      if (response.data && response.data.$values) {
        return response.data.$values;
      }

      // If it's already an array, return it
      if (Array.isArray(response.data)) {
        return response.data;
      }

      // If we get here, return an empty array
      console.warn('Unexpected usage response format:', response.data);
      return [];
    } catch (error) {
      console.error('Error fetching usage by model:', error);
      return [];
    }
  },

  getUserUsage: async (startDate?: Date | string, endDate?: Date | string): Promise<LlmUsageLog[]> => {
    try {
      // Updated endpoint to use the chat-usage logs endpoint which exists in the backend
      let url = '/chat-usage/logs';
      const params = new URLSearchParams();

      if (startDate) {
        params.append('startDate', typeof startDate === 'string' ? startDate : startDate.toISOString());
      }

      if (endDate) {
        params.append('endDate', typeof endDate === 'string' ? endDate : endDate.toISOString());
      }

      // Add the params to the URL
      if (params.toString()) {
        url += `?${params.toString()}`;
      }

      const response = await apiClient.get(url);
      
      let chatLogs = [];
      
      // Handle response format that might have $values property
      if (response.data && response.data.$values) {
        chatLogs = response.data.$values;
      }
      // If it's already an array, use it
      else if (Array.isArray(response.data)) {
        chatLogs = response.data;
      }
      
      // Convert ChatUsageLog format to LlmUsageLog format
      const mappedLogs: LlmUsageLog[] = chatLogs.map((log: any) => {
        return {
          id: log.id,
          modelId: log.modelId || 0,
          credentialId: null,
          userId: log.userId,
          sessionId: log.sessionId,
          requestTimestamp: log.timestamp,
          responseTimestamp: log.timestamp,
          inputTokens: log.inputTokenCount || 0,
          outputTokens: log.outputTokenCount || 0,
          totalTokens: (log.inputTokenCount || 0) + (log.outputTokenCount || 0),
          estimatedCost: log.estimatedCost || 0,
          status: log.success ? 'Success' : 'Failed',
          errorMessage: log.errorMessage,
          model: {
            id: log.modelId,
            providerId: log.providerId,
            name: log.modelName,
            modelId: log.modelName,
            description: '',
            maxTokens: 0,
            contextWindow: 0,
            supportsStreaming: true,
            supportsVision: false,
            supportsTools: false,
            costPer1KInputTokens: 0,
            costPer1KOutputTokens: 0,
            isEnabled: true,
            createdAt: '',
            provider: {
              id: log.providerId,
              name: log.providerName,
              displayName: log.providerName,
              apiEndpoint: '',
              description: '',
              isEnabled: true,
              authType: 'ApiKey',
              configSchema: '{}',
              createdAt: '',
            }
          }
        };
      });

      return mappedLogs;
    } catch (error) {
      console.error('Error fetching user usage data:', error);
      return [];
    }
  },

  // Additional usage endpoints (fallback)
  getUsageLogs: async (startDate?: Date | string, endDate?: Date | string): Promise<LlmUsageLog[]> => {
    try {
      let url = '/chat/usage';
      const params = new URLSearchParams();

      if (startDate) {
        params.append('startDate', typeof startDate === 'string' ? startDate : startDate.toISOString());
      }

      if (endDate) {
        params.append('endDate', typeof endDate === 'string' ? endDate : endDate.toISOString());
      }

      if (params.toString()) {
        url += `?${params.toString()}`;
      }

      const response = await apiClient.get<any>(url);

      // Handle response format that might have $values property
      if (response.data && response.data.$values) {
        return response.data.$values;
      }

      // If it's already an array, return it
      if (Array.isArray(response.data)) {
        return response.data;
      }

      // If we get here, return an empty array
      console.warn('Unexpected usage logs response format:', response.data);
      return [];
    } catch (error) {
      console.error('Error fetching usage logs:', error);
      return [];
    }
  }
};
