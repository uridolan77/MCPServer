import React, { createContext, useContext, useState, ReactNode } from 'react';
import { LlmModel, LlmProvider, LlmUsageLog } from '@/api/llmProviderApi';
import {
  ChatUsageLog,
  OverallStats,
  ModelUsageStat,
  ProviderUsageStat,
  DashboardStats
} from '@/api/analyticsApi';

// Define the shape of our context
interface UsageContextType {
  // Date filters
  startDate: Date | null;
  setStartDate: (date: Date | null) => void;
  endDate: Date | null;
  setEndDate: (date: Date | null) => void;
  
  // Model and provider filters
  selectedModelId: string | number;
  setSelectedModelId: (id: string | number) => void;
  selectedProviderId: string | number;
  setSelectedProviderId: (id: string | number) => void;
  
  // Data
  models: LlmModel[];
  setModels: (models: LlmModel[]) => void;
  providers: LlmProvider[];
  setProviders: (providers: LlmProvider[]) => void;
  usageLogs: LlmUsageLog[];
  setUsageLogs: (logs: LlmUsageLog[]) => void;
  
  // Analytics data
  chatUsageLogs: ChatUsageLog[];
  setChatUsageLogs: (logs: ChatUsageLog[]) => void;
  overallStats: OverallStats | null;
  setOverallStats: (stats: OverallStats | null) => void;
  dashboardStats: DashboardStats | null;
  setDashboardStats: (stats: DashboardStats | null) => void;
  
  // Filtered data
  filteredUsageLogs: LlmUsageLog[];
  setFilteredUsageLogs: (logs: LlmUsageLog[]) => void;
  filteredChatLogs: ChatUsageLog[];
  setFilteredChatLogs: (logs: ChatUsageLog[]) => void;
  
  // UI state
  isLoading: boolean;
  setIsLoading: (loading: boolean) => void;
  activeTabIndex: number;
  setActiveTabIndex: (index: number) => void;
  
  // Actions
  refreshData: () => void;
}

// Create the context with default values
const UsageContext = createContext<UsageContextType>({
  startDate: null,
  setStartDate: () => {},
  endDate: null,
  setEndDate: () => {},
  selectedModelId: '',
  setSelectedModelId: () => {},
  selectedProviderId: '',
  setSelectedProviderId: () => {},
  models: [],
  setModels: () => {},
  providers: [],
  setProviders: () => {},
  usageLogs: [],
  setUsageLogs: () => {},
  chatUsageLogs: [],
  setChatUsageLogs: () => {},
  overallStats: null,
  setOverallStats: () => {},
  dashboardStats: null,
  setDashboardStats: () => {},
  filteredUsageLogs: [],
  setFilteredUsageLogs: () => {},
  filteredChatLogs: [],
  setFilteredChatLogs: () => {},
  isLoading: false,
  setIsLoading: () => {},
  activeTabIndex: 0,
  setActiveTabIndex: () => {},
  refreshData: () => {},
});

// Provider component
export const UsageContextProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  // Initialize state
  const [startDate, setStartDate] = useState<Date | null>(
    new Date(new Date().setMonth(new Date().getMonth() - 1))
  );
  const [endDate, setEndDate] = useState<Date | null>(new Date());
  const [selectedModelId, setSelectedModelId] = useState<string | number>('');
  const [selectedProviderId, setSelectedProviderId] = useState<string | number>('');
  const [models, setModels] = useState<LlmModel[]>([]);
  const [providers, setProviders] = useState<LlmProvider[]>([]);
  const [usageLogs, setUsageLogs] = useState<LlmUsageLog[]>([]);
  const [chatUsageLogs, setChatUsageLogs] = useState<ChatUsageLog[]>([]);
  const [overallStats, setOverallStats] = useState<OverallStats | null>(null);
  const [dashboardStats, setDashboardStats] = useState<DashboardStats | null>(null);
  const [filteredUsageLogs, setFilteredUsageLogs] = useState<LlmUsageLog[]>([]);
  const [filteredChatLogs, setFilteredChatLogs] = useState<ChatUsageLog[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [activeTabIndex, setActiveTabIndex] = useState(0);
  
  // Function to refresh data (will be implemented by consuming components)
  const refreshData = () => {
    // This will be overridden by actual implementation in the components
    console.log('Refresh data called');
  };
  
  const value = {
    startDate,
    setStartDate,
    endDate,
    setEndDate,
    selectedModelId,
    setSelectedModelId,
    selectedProviderId,
    setSelectedProviderId,
    models,
    setModels,
    providers,
    setProviders,
    usageLogs,
    setUsageLogs,
    chatUsageLogs,
    setChatUsageLogs,
    overallStats,
    setOverallStats,
    dashboardStats,
    setDashboardStats,
    filteredUsageLogs,
    setFilteredUsageLogs,
    filteredChatLogs,
    setFilteredChatLogs,
    isLoading,
    setIsLoading,
    activeTabIndex,
    setActiveTabIndex,
    refreshData,
  };
  
  return <UsageContext.Provider value={value}>{children}</UsageContext.Provider>;
};

// Custom hook to use the usage context
export const useUsageContext = () => useContext(UsageContext);

export default UsageContextProvider;