import api from './api';
import DataTransferUtilsService from './dataTransferUtils.service';

/**
 * Service for handling run history operations in the data transfer system
 */
class RunHistoryService {
  /**
   * Get run history for a specific configuration or all configurations
   */
  static async getRunHistory(configurationId: number = 0) {
    const url = configurationId
      ? `/datatransfer/runs?configurationId=${configurationId}`
      : '/datatransfer/runs';

    const response = await api.get(url);
    const runHistory = DataTransferUtilsService.extractFromNestedValues(response.data);
    
    if (!Array.isArray(runHistory)) return [];
    
    return runHistory.map(item => {
      if (!item) return null;

      return {
        runId: item.RunId,
        configurationId: item.ConfigurationId,
        startTime: item.StartTime,
        endTime: item.EndTime,
        status: item.Status,
        totalTablesProcessed: item.TotalTablesProcessed,
        successfulTablesCount: item.SuccessfulTablesCount,
        failedTablesCount: item.FailedTablesCount,
        totalRowsProcessed: item.TotalRowsProcessed,
        elapsedMs: item.ElapsedMs,
        averageRowsPerSecond: item.AverageRowsPerSecond,
        triggeredBy: item.TriggeredBy,
        
        // Additional properties that might be included in API response
        configurationName: item.ConfigurationName
      };
    }).filter(Boolean);
  }

  /**
   * Get details for a specific run
   */
  static async getRunDetails(runId: number) {
    const response = await api.get(`/datatransfer/runs/${runId}`);
    return DataTransferUtilsService.extractFromNestedValues(response.data);
  }
}

export default RunHistoryService;