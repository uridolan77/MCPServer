import axios from 'axios';

// Define the Credential interface
export interface Credential {
  id: string;
  name: string;
  provider: string;
  apiKey: string;
  apiSecret?: string;
  description?: string;
  createdAt: string;
  updatedAt: string;
}

// Define the Provider interface
export interface Provider {
  id: string;
  name: string;
  description?: string;
}

// API base URL
const API_URL = 'http://localhost:2000/api';

// Get all credentials
export const fetchCredentials = async (): Promise<Credential[]> => {
  try {
    // For now, return mock data
    // In the future, this will be replaced with a real API call
    // const response = await axios.get(`${API_URL}/credentials`);
    // return response.data;
    
    return mockCredentials;
  } catch (error) {
    console.error('Error fetching credentials:', error);
    throw error;
  }
};

// Get credential by ID
export const fetchCredentialById = async (id: string): Promise<Credential> => {
  try {
    // For now, return mock data
    // In the future, this will be replaced with a real API call
    // const response = await axios.get(`${API_URL}/credentials/${id}`);
    // return response.data;
    
    const credential = mockCredentials.find(cred => cred.id === id);
    if (!credential) {
      throw new Error('Credential not found');
    }
    return credential;
  } catch (error) {
    console.error(`Error fetching credential with ID ${id}:`, error);
    throw error;
  }
};

// Get all providers
export const fetchProviders = async (): Promise<Provider[]> => {
  try {
    // For now, return mock data
    // In the future, this will be replaced with a real API call
    // const response = await axios.get(`${API_URL}/providers`);
    // return response.data;
    
    return mockProviders;
  } catch (error) {
    console.error('Error fetching providers:', error);
    throw error;
  }
};

// Create a new credential
export const createCredential = async (credential: Omit<Credential, 'id' | 'createdAt' | 'updatedAt'>): Promise<Credential> => {
  try {
    // For now, return mock data
    // In the future, this will be replaced with a real API call
    // const response = await axios.post(`${API_URL}/credentials`, credential);
    // return response.data;
    
    return {
      ...credential,
      id: `cred-${Math.floor(Math.random() * 1000)}`,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    };
  } catch (error) {
    console.error('Error creating credential:', error);
    throw error;
  }
};

// Update an existing credential
export const updateCredential = async (id: string, credential: Partial<Credential>): Promise<Credential> => {
  try {
    // For now, return mock data
    // In the future, this will be replaced with a real API call
    // const response = await axios.put(`${API_URL}/credentials/${id}`, credential);
    // return response.data;
    
    const existingCredential = mockCredentials.find(cred => cred.id === id);
    if (!existingCredential) {
      throw new Error('Credential not found');
    }
    
    return {
      ...existingCredential,
      ...credential,
      updatedAt: new Date().toISOString(),
    };
  } catch (error) {
    console.error(`Error updating credential with ID ${id}:`, error);
    throw error;
  }
};

// Delete a credential
export const deleteCredential = async (id: string): Promise<void> => {
  try {
    // For now, just log the deletion
    // In the future, this will be replaced with a real API call
    // await axios.delete(`${API_URL}/credentials/${id}`);
    console.log(`Credential with ID ${id} deleted`);
  } catch (error) {
    console.error(`Error deleting credential with ID ${id}:`, error);
    throw error;
  }
};

// Mock data for development
const mockCredentials: Credential[] = [
  {
    id: 'cred-1',
    name: 'OpenAI API Key',
    provider: 'provider-1',
    apiKey: 'sk-1234567890abcdef1234567890abcdef',
    description: 'OpenAI API key for GPT-4 and other models',
    createdAt: '2023-05-01T10:00:00Z',
    updatedAt: '2023-05-02T15:30:00Z',
  },
  {
    id: 'cred-2',
    name: 'Azure OpenAI Key',
    provider: 'provider-2',
    apiKey: 'azure-1234567890abcdef1234567890abcdef',
    apiSecret: 'secret-1234567890abcdef1234567890abcdef',
    description: 'Azure OpenAI API key for hosted models',
    createdAt: '2023-05-03T09:15:00Z',
    updatedAt: '2023-05-03T14:45:00Z',
  },
  {
    id: 'cred-3',
    name: 'Anthropic API Key',
    provider: 'provider-3',
    apiKey: 'sk-ant-1234567890abcdef1234567890abcdef',
    description: 'Anthropic API key for Claude models',
    createdAt: '2023-05-04T11:30:00Z',
    updatedAt: '2023-05-05T16:20:00Z',
  },
];

// Mock providers for development
const mockProviders: Provider[] = [
  {
    id: 'provider-1',
    name: 'OpenAI',
    description: 'OpenAI API provider for GPT models',
  },
  {
    id: 'provider-2',
    name: 'Azure OpenAI',
    description: 'Microsoft Azure hosted OpenAI models',
  },
  {
    id: 'provider-3',
    name: 'Anthropic',
    description: 'Anthropic API provider for Claude models',
  },
  {
    id: 'provider-4',
    name: 'Google AI',
    description: 'Google AI API provider for Gemini models',
  },
];
