import axios from '@/lib/axios';

class DataTransferService {
  // Connection management
  async getConnections() {
    const response = await axios.get('/data-transfer/connections');
    return response.data;
  }

  async getConnection(id) {
    const response = await axios.get(`/data-transfer/connections/${id}`);
    return response.data;
  }

  async saveConnection(connection) {
    if (connection.connectionId) {
      const response = await axios.put(`/data-transfer/connections/${connection.connectionId}`, connection);
      return response.data;
    } else {
      const response = await axios.post('/data-transfer/connections', connection);
      return response.data;
    }
  }

  async deleteConnection(id) {
    const response = await axios.delete(`/data-transfer/connections/${id}`);
    return response.data;
  }

  async testConnection(connection) {
    const response = await axios.post('/data-transfer/connections/test', connection);
    return response.data;
  }

  // Configuration management
  async getConfigurations() {
    const response = await axios.get('/data-transfer/configurations');
    return response.data;
  }

  async getConfiguration(id) {
    const response = await axios.get(`/data-transfer/configurations/${id}`);
    return response.data;
  }

  async saveConfiguration(configuration) {
    if (configuration.configurationId) {
      const response = await axios.put(`/data-transfer/configurations/${configuration.configurationId}`, configuration);
      return response.data;
    } else {
      const response = await axios.post('/data-transfer/configurations', configuration);
      return response.data;
    }
  }

  async deleteConfiguration(id) {
    const response = await axios.delete(`/data-transfer/configurations/${id}`);
    return response.data;
  }

  // Run management
  async executeTransfer(configurationId) {
    const response = await axios.post(`/data-transfer/configurations/${configurationId}/execute`);
    return response.data;
  }

  async getRunHistory(configurationId = 0, limit = 50) {
    const response = await axios.get(`/data-transfer/history?configurationId=${configurationId}&limit=${limit}`);
    return response.data;
  }

  async getRunDetails(runId) {
    const response = await axios.get(`/data-transfer/runs/${runId}`);
    return response.data;
  }
}

export default new DataTransferService();