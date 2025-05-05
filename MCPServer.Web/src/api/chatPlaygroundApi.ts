import apiClient from './apiClient';
import { ApiResponse, LlmModel, LlmProvider } from './llmProviderApi';

export interface Message {
  role: string;
  content: string;
  timestamp?: string;
  tokenCount?: number;
}

export interface ChatRequest {
  message: string;
  sessionId: string;
  history?: Message[];
  modelId?: number;
  temperature?: number;
  maxTokens?: number;
  systemPrompt?: string;
}

export interface ChatResponse {
  message: string;
  sessionId: string;
}

export interface StreamChunkResponse {
  chunk: string;
  isComplete: boolean;
  sessionId: string;
}

export interface ErrorResponse {
  error: string;
  details?: string;
  stackTrace?: string;
}

// New interfaces for chat usage statistics
export interface ChatUsageStats {
  totalMessages: number;
  totalTokensUsed: number;
  totalCost: number;
  modelStats: ModelUsageStat[];
  providerStats: ProviderUsageStat[];
}

export interface ModelUsageStat {
  modelId: number;
  modelName: string;
  messagesCount: number;
  inputTokens: number;
  outputTokens: number;
  totalTokens: number;
  estimatedCost: number;
}

export interface ProviderUsageStat {
  providerId: number;
  providerName: string;
  messagesCount: number;
  totalTokens: number;
  estimatedCost: number;
}

export interface ChatUsageLog {
  id: number;
  sessionId: string;
  userId?: string;
  modelId: number;
  modelName: string;
  providerId: number;
  providerName: string;
  message: string;
  response: string;
  inputTokenCount: number;
  outputTokenCount: number;
  estimatedCost: number;
  duration: number;
  success: boolean;
  errorMessage?: string;
  timestamp: string;
}

export const chatPlaygroundApi = {
  getAvailableModels: async (): Promise<LlmModel[]> => {
    try {
      console.log('API call: Getting available models');

      // First, try to get models from the main models endpoint
      try {
        const response = await apiClient.get<any>('/llm/models');
        console.log('Response from models endpoint:', response);

        if (response.data) {
          // Handle different response formats
          let modelsArray: LlmModel[] = [];

          // If it's a direct array
          if (Array.isArray(response.data)) {
            console.log('Response is a direct array with length:', response.data.length);
            modelsArray = response.data;
          }
          // If it's a wrapped response with success/data fields
          else if (response.data.success && response.data.data) {
            if (Array.isArray(response.data.data)) {
              console.log('Found data array property with length:', response.data.data.length);
              modelsArray = response.data.data;
            } else if (response.data.data.$values && Array.isArray(response.data.data.$values)) {
              console.log('Found $values array property with length:', response.data.data.$values.length);
              modelsArray = response.data.data.$values;
            }
          }
          // If it has $values directly (common .NET serialization)
          else if (response.data.$values && Array.isArray(response.data.$values)) {
            console.log('Found $values array property with length:', response.data.$values.length);
            modelsArray = response.data.$values;
          }
          // If it has another common wrapper property
          else if (response.data.items && Array.isArray(response.data.items)) {
            console.log('Found items array property with length:', response.data.items.length);
            modelsArray = response.data.items;
          } else if (response.data.models && Array.isArray(response.data.models)) {
            console.log('Found models array property with length:', response.data.models.length);
            modelsArray = response.data.models;
          }

          if (modelsArray.length > 0) {
            console.log('Models before processing:', modelsArray);

            // Ensure each model has its provider data properly set
            const processedModels = await Promise.all(modelsArray.map(async (model) => {
              // If provider is missing or incomplete
              if (!model.provider || (!model.provider.name && !model.provider.displayName)) {
                console.log(`Model ${model.name} (ID: ${model.id}) has missing provider info, fetching provider...`);
                try {
                  // Fetch the provider using the providerId if we have it
                  if (model.providerId) {
                    const providerResponse = await apiClient.get<any>(`/llm/providers/${model.providerId}`);
                    let providerData = null;

                    if (providerResponse.data) {
                      if (providerResponse.data.data) {
                        providerData = providerResponse.data.data;
                      } else {
                        providerData = providerResponse.data;
                      }

                      if (providerData) {
                        model.provider = providerData;
                        console.log(`Updated provider for model ${model.name}: ${model.provider?.name || model.provider?.displayName || 'Unknown'}`);
                      }
                    }
                  }
                } catch (err) {
                  console.error(`Error fetching provider for model ${model.name}:`, err);
                  // Set a default provider based on model name
                  model.provider = createDefaultProvider(model);
                }
              }
              return model;
            }));

            console.log('Final processed models array:', processedModels);
            return processedModels;
          }
        }
      } catch (error) {
        console.warn('Error fetching models from main endpoint, trying alternatives:', error);
      }

      // Try alternative endpoints if the main one fails
      const alternativeEndpoints = [
        '/chat/models',
        '/llm/models',
        '/models'
      ];

      for (const endpoint of alternativeEndpoints) {
        try {
          console.log(`Trying alternative endpoint: ${endpoint}`);
          const response = await apiClient.get<any>(endpoint);

          if (response.data) {
            // Extract models using the same logic as above
            let modelsArray: LlmModel[] = extractModelsFromResponse(response.data);

            if (modelsArray.length > 0) {
              console.log(`Found ${modelsArray.length} models from endpoint ${endpoint}`);

              // Process models to ensure provider info
              const processedModels = await Promise.all(modelsArray.map(async (model) => {
                if (!model.provider || (!model.provider.name && !model.provider.displayName)) {
                  try {
                    if (model.providerId) {
                      // Try multiple provider endpoint formats
                      const providerEndpoints = [
                        `/llm/providers/${model.providerId}`,
                        `/providers/${model.providerId}`
                      ];

                      for (const providerEndpoint of providerEndpoints) {
                        try {
                          const providerResponse = await apiClient.get<any>(providerEndpoint);
                          if (providerResponse.data) {
                            const providerData = providerResponse.data.data || providerResponse.data;
                            if (providerData) {
                              model.provider = providerData;
                              console.log(`Updated provider for model ${model.name}: ${model.provider?.name || model.provider?.displayName || 'Unknown'}`);
                              break;
                            }
                          }
                        } catch (err) {
                          // Continue to the next endpoint
                        }
                      }
                    }
                  } catch (err) {
                    console.error(`Error fetching provider for model ${model.name}:`, err);
                  }

                  // If still no provider, create a default one
                  if (!model.provider || (!model.provider.name && !model.provider.displayName)) {
                    model.provider = createDefaultProvider(model);
                  }
                }
                return model;
              }));

              return processedModels;
            }
          }
        } catch (err) {
          console.warn(`Error trying alternative endpoint ${endpoint}:`, err);
          // Continue to the next endpoint
        }
      }

      // If we get here, we couldn't find any models
      console.error('Failed to fetch models from any endpoint');
      return [];
    } catch (error) {
      console.error('Error in getAvailableModels:', error);
      return [];
    }
  },

  sendMessage: async (request: ChatRequest): Promise<ChatResponse> => {
    try {
      console.log('Sending message to chat API:', request);

      // Use the new chat endpoint without explicitly setting headers
      const response = await apiClient.post<ApiResponse<ChatResponse>>('/chat/send', request);

      if (response.data && response.data.success && response.data.data) {
        return response.data.data;
      } else {
        console.error('Error response from send message API:', response.data);
        throw new Error('Invalid response format from API');
      }
    } catch (error) {
      console.error('Error sending message:', error);
      throw error;
    }
  },

  streamMessage: async (
    request: ChatRequest,
    onChunkReceived: (chunk: StreamChunkResponse) => void
  ): Promise<void> => {
    try {
      console.log('Initiating streaming chat:', request);

      // Don't set any headers explicitly, rely on apiClient's defaults
      const initResponse = await apiClient.post<ApiResponse<any>>('/chat/stream', request);

      if (!initResponse.data || !initResponse.data.success) {
        console.error('Chat stream endpoint returned unsuccessful response');
        onChunkReceived({
          chunk: 'Error: Failed to initiate streaming chat',
          isComplete: true,
          sessionId: request.sessionId,
        });
        return;
      }

      // Create EventSource for server-sent events
      // Use a relative URL to work properly with the Vite proxy configuration
      const streamEndpoint = '/api/chat/stream';

      console.log(`Creating EventSource with URL: ${streamEndpoint}`);

      const eventSource = new EventSource(
        `${streamEndpoint}?${new URLSearchParams({
          token: localStorage.getItem('token') || '',
          sessionId: request.sessionId,
        })}`
      );

      // Set up event handlers
      eventSource.onmessage = (event) => {
        try {
          const data = JSON.parse(event.data) as StreamChunkResponse;
          onChunkReceived(data);

          // Close the connection when complete
          if (data.isComplete) {
            eventSource.close();
          }
        } catch (error) {
          console.error('Error parsing event data:', error);
          onChunkReceived({
            chunk: 'Error processing response',
            isComplete: true,
            sessionId: request.sessionId,
          });
          eventSource.close();
        }
      };

      eventSource.onerror = (error) => {
        console.error('EventSource error:', error);
        onChunkReceived({
          chunk: 'Connection error',
          isComplete: true,
          sessionId: request.sessionId,
        });
        eventSource.close();
      };
    } catch (error) {
      console.error('Error initiating streaming:', error);
      onChunkReceived({
        chunk: 'Failed to connect to the server',
        isComplete: true,
        sessionId: request.sessionId,
      });
    }
  },
};
