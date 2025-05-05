import apiClient from './apiClient';

export interface ApiResponse<T> {
  success: boolean;
  data: T;
  message?: string;
  errors?: string[];
}

// ChatUsageLog interface for detailed log information
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

// Session data interface
export interface SessionData {
  sessionId: string;
  data: string; // JSON serialized message history
  userId?: string;
  createdAt: string;
  lastUpdatedAt: string;
  expiresAt?: string;
}

// Usage metrics interface
export interface UsageMetric {
  id: number;
  userId?: string;
  metricType: string;
  value: number;
  sessionId?: string;
  additionalData?: string; // JSON serialized additional data
  timestamp: string;
}

// Stats interfaces for aggregated data
export interface OverallStats {
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

// Dashboard stats interface for UI components
export interface DashboardStats {
  totalSessions: number;
  totalMessages: number;
  totalTokens: number;
  totalCost: number;
  successRate: number;
  averageResponseTime: number;
  topModels: ModelUsageStat[];
  recentLogs: ChatUsageLog[];
  dailyUsage: DailyUsage[];
}

export interface DailyUsage {
  date: string;
  messages: number;
  tokens: number;
  cost: number;
}

// Helper function to extract the actual logs array from various response structures
const extractLogsFromResponse = (response: any): ChatUsageLog[] => {
  console.log('Response structure:', JSON.stringify(response).substring(0, 200) + '...');
  
  if (!response) {
    return [];
  }
  
  // Case 1: Direct array
  if (Array.isArray(response)) {
    console.log('Found direct array structure');
    return response;
  }
  
  // Case 2: Response with data.$values structure (most likely)
  if (response.data && response.data.$values) {
    console.log('Found nested data.$values structure');
    return response.data.$values;
  }
  
  // Case 3: Response with data array
  if (response.data && Array.isArray(response.data)) {
    console.log('Found data array structure');
    return response.data;
  }
  
  // Case 4: Top-level $values array
  if (response.$values) {
    console.log('Found top-level $values structure');
    return response.$values;
  }
  
  console.warn('Unable to extract logs from response, returning empty array');
  return [];
};

export const analyticsApi = {
  // Get overall usage statistics
  getOverallStats: async (): Promise<OverallStats> => {
    try {
      const response = await apiClient.get<OverallStats>('/chat-usage/stats');
      return response.data;
    } catch (error) {
      console.error('Error fetching overall usage statistics:', error);
      // Return empty stats object if API call fails
      return {
        totalMessages: 0,
        totalTokensUsed: 0,
        totalCost: 0,
        modelStats: [],
        providerStats: []
      };
    }
  },

  // Get chat usage logs with filtering options
  getChatUsageLogs: async (
    startDate?: Date | string,
    endDate?: Date | string,
    modelId?: number,
    providerId?: number,
    sessionId?: string,
    page: number = 1,
    pageSize: number = 20
  ): Promise<ChatUsageLog[]> => {
    try {
      let url = '/chat-usage/logs';
      const params = new URLSearchParams();

      if (startDate) {
        params.append('startDate', typeof startDate === 'string' ? startDate : startDate.toISOString());
      }

      if (endDate) {
        params.append('endDate', typeof endDate === 'string' ? endDate : endDate.toISOString());
      }

      if (modelId) {
        params.append('modelId', modelId.toString());
      }

      if (providerId) {
        params.append('providerId', providerId.toString());
      }

      if (sessionId) {
        params.append('sessionId', sessionId);
      }

      params.append('page', page.toString());
      params.append('pageSize', pageSize.toString());

      if (params.toString()) {
        url += `?${params.toString()}`;
      }

      const response = await apiClient.get(url);
      console.log('Chat logs response received:', response);

      const logs = extractLogsFromResponse(response.data);
      console.log(`Extracted ${logs.length} logs from response`);
      return logs;
    } catch (error) {
      console.error('Error fetching chat usage logs:', error);
      return [];
    }
  },

  // Get model-specific usage statistics
  getModelStats: async (modelId: number): Promise<ModelUsageStat | null> => {
    try {
      const response = await apiClient.get<ModelUsageStat>(`/chat-usage/stats/model/${modelId}`);
      return response.data;
    } catch (error) {
      console.error(`Error fetching model usage statistics for model ID ${modelId}:`, error);
      return null;
    }
  },

  // Get provider-specific usage statistics
  getProviderStats: async (providerId: number): Promise<ProviderUsageStat | null> => {
    try {
      const response = await apiClient.get<ProviderUsageStat>(`/chat-usage/stats/provider/${providerId}`);
      return response.data;
    } catch (error) {
      console.error(`Error fetching provider usage statistics for provider ID ${providerId}:`, error);
      return null;
    }
  },

  // Get session data
  getSessionData: async (sessionId: string): Promise<SessionData | null> => {
    try {
      const response = await apiClient.get<SessionData>(`/sessions/${sessionId}`);
      return response.data;
    } catch (error) {
      console.error(`Error fetching session data for session ID ${sessionId}:`, error);
      return null;
    }
  },

  // Get all sessions
  getAllSessions: async (page: number = 1, pageSize: number = 20): Promise<SessionData[]> => {
    try {
      let url = '/sessions';
      const params = new URLSearchParams();

      params.append('page', page.toString());
      params.append('pageSize', pageSize.toString());

      if (params.toString()) {
        url += `?${params.toString()}`;
      }

      try {
        const response = await apiClient.get<SessionData[]>(url);

        // Handle response format that might have $values property
        if (response.data && response.data.$values) {
          return response.data.$values;
        }

        // If it's already an array, return it
        if (Array.isArray(response.data)) {
          return response.data;
        }

        return [];
      } catch (fetchError: any) {
        // If endpoint doesn't exist (404), just return empty array
        if (fetchError.response?.status === 404) {
          console.warn('Sessions endpoint not available (404)', url);
          return [];
        }
        throw fetchError;
      }
    } catch (error) {
      console.error('Error fetching all sessions:', error);
      return [];
    }
  },

  // Delete a session
  deleteSession: async (sessionId: string): Promise<boolean> => {
    try {
      const response = await apiClient.delete<ApiResponse<boolean>>(`/sessions/${sessionId}`);
      return response.data.success;
    } catch (error) {
      console.error(`Error deleting session with ID ${sessionId}:`, error);
      throw error;
    }
  },

  // Get usage metrics
  getUsageMetrics: async (
    metricType?: string,
    startDate?: Date | string,
    endDate?: Date | string,
    sessionId?: string,
    page: number = 1,
    pageSize: number = 20
  ): Promise<UsageMetric[]> => {
    try {
      let url = '/metrics';
      const params = new URLSearchParams();

      if (metricType) {
        params.append('metricType', metricType);
      }

      if (startDate) {
        params.append('startDate', typeof startDate === 'string' ? startDate : startDate.toISOString());
      }

      if (endDate) {
        params.append('endDate', typeof endDate === 'string' ? endDate : endDate.toISOString());
      }

      if (sessionId) {
        params.append('sessionId', sessionId);
      }

      params.append('page', page.toString());
      params.append('pageSize', pageSize.toString());

      if (params.toString()) {
        url += `?${params.toString()}`;
      }

      const response = await apiClient.get<UsageMetric[]>(url);

      // Handle response format that might have $values property
      if (response.data && response.data.$values) {
        return response.data.$values;
      }

      // If it's already an array, return it
      if (Array.isArray(response.data)) {
        return response.data;
      }

      return [];
    } catch (error) {
      console.error('Error fetching usage metrics:', error);
      return [];
    }
  },

  // Get dashboard statistics (combines multiple endpoints for dashboard UI)
  getDashboardStats: async (
    startDate?: Date | string,
    endDate?: Date | string
  ): Promise<DashboardStats> => {
    try {
      // Get overall stats
      const overallStats = await analyticsApi.getOverallStats();

      // Get recent logs (last 5)
      const recentLogs = await analyticsApi.getChatUsageLogs(startDate, endDate, undefined, undefined, undefined, 1, 5);

      // Calculate daily usage stats
      const allLogs = await analyticsApi.getChatUsageLogs(startDate, endDate);
      const dailyUsage = calculateDailyUsage(allLogs);

      // Calculate average response time
      const avgResponseTime = calculateAverageResponseTime(allLogs);

      // Calculate success rate
      const successRate = calculateSuccessRate(allLogs);

      // Get sessions count
      const sessions = await analyticsApi.getAllSessions();

      // Fix for modelStats not being an array or being undefined
      const topModels = Array.isArray(overallStats?.modelStats)
        ? overallStats.modelStats.sort((a, b) => b.totalTokens - a.totalTokens).slice(0, 5)
        : [];

      return {
        totalSessions: sessions.length,
        totalMessages: overallStats.totalMessages,
        totalTokens: overallStats.totalTokensUsed,
        totalCost: overallStats.totalCost,
        successRate,
        averageResponseTime: avgResponseTime,
        topModels,
        recentLogs,
        dailyUsage
      };
    } catch (error) {
      console.error('Error fetching dashboard statistics:', error);
      return {
        totalSessions: 0,
        totalMessages: 0,
        totalTokens: 0,
        totalCost: 0,
        successRate: 0,
        averageResponseTime: 0,
        topModels: [],
        recentLogs: [],
        dailyUsage: []
      };
    }
  }
};

// Helper functions for data processing
function calculateDailyUsage(logs: ChatUsageLog[]): DailyUsage[] {
  const dailyMap: Record<string, DailyUsage> = {};

  logs.forEach(log => {
    const date = new Date(log.timestamp).toLocaleDateString();

    if (!dailyMap[date]) {
      dailyMap[date] = {
        date,
        messages: 0,
        tokens: 0,
        cost: 0
      };
    }

    dailyMap[date].messages += 1;
    dailyMap[date].tokens += log.inputTokenCount + log.outputTokenCount;
    dailyMap[date].cost += log.estimatedCost;
  });

  return Object.values(dailyMap).sort((a, b) =>
    new Date(a.date).getTime() - new Date(b.date).getTime()
  );
}

function calculateAverageResponseTime(logs: ChatUsageLog[]): number {
  if (logs.length === 0) return 0;

  const totalDuration = logs.reduce((sum, log) => sum + log.duration, 0);
  return totalDuration / logs.length;
}

function calculateSuccessRate(logs: ChatUsageLog[]): number {
  if (logs.length === 0) return 0;

  const successfulLogs = logs.filter(log =>
    log.success &&
    !log.response.includes("Error") &&
    !log.response.includes("error") &&
    !log.errorMessage
  ).length;

  return (successfulLogs / logs.length) * 100;
}