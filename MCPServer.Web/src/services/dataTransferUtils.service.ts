import api from './api';

/**
 * Utility functions for data transformation and processing in data transfer operations
 */
class DataTransferUtilsService {
  /**
   * Extracts data from nested $values objects in API responses
   */
  static extractFromNestedValues(data: any): any {
    if (!data) return data;

    // Handle top-level $values
    if (data.$values) {
      // Handle double-nested $values
      if (typeof data.$values === 'object' && !Array.isArray(data.$values) && data.$values.$values) {
        data = data.$values.$values;
      } else {
        data = data.$values;
      }
    }

    // If result is an array, process each item
    if (Array.isArray(data)) {
      // Extract single item from array if needed
      if (data.length === 1) {
        return this.processObjectProperties(data[0]);
      }

      // Process each item in the array
      return data.map(item => this.processObjectProperties(item));
    }

    // Process object properties
    return this.processObjectProperties(data);
  }

  /**
   * Process object properties to extract nested $values
   */
  static processObjectProperties(obj: any): any {
    if (!obj || typeof obj !== 'object') return obj;

    const result = {...obj};

    // Process each property for nested $values
    for (const key of Object.keys(result)) {
      if (result[key] && typeof result[key] === 'object') {
        if (result[key].$values) {
          result[key] = this.extractFromNestedValues(result[key]);
        } else if (Array.isArray(result[key])) {
          result[key] = result[key].map((item: any) =>
            item && typeof item === 'object' && item.$values ?
              this.extractFromNestedValues(item) : item
          );
        }
      }
    }

    return result;
  }
}

export default DataTransferUtilsService;