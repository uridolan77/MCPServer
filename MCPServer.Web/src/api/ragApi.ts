import apiClient from './apiClient';

export interface Document {
  id: string;
  title: string;
  content: string;
  source: string;
  url: string;
  createdAt: string;
  tags: string[];
  metadata: Record<string, string>;
}

export interface Chunk {
  id: string;
  documentId: string;
  content: string;
  chunkIndex: number;
  embedding: number[];
  metadata: Record<string, string>;
}

export interface SearchRequest {
  query: string;
  topK: number;
  minScore: number;
  tags?: string[];
  metadata?: Record<string, string>;
}

export interface SearchResult {
  chunks: Chunk[];
  documents: Document[];
  scores: number[];
}

export const ragApi = {
  // Document endpoints
  getAllDocuments: async (): Promise<Document[]> => {
    const response = await apiClient.get<Document[]>('/rag/documents');
    return response.data;
  },
  
  getDocumentById: async (id: string): Promise<Document> => {
    const response = await apiClient.get<Document>(`/rag/documents/${id}`);
    return response.data;
  },
  
  createDocument: async (document: Omit<Document, 'id' | 'createdAt'>): Promise<Document> => {
    const response = await apiClient.post<Document>('/rag/documents', document);
    return response.data;
  },
  
  updateDocument: async (id: string, document: Partial<Document>): Promise<void> => {
    await apiClient.put(`/rag/documents/${id}`, document);
  },
  
  deleteDocument: async (id: string): Promise<void> => {
    await apiClient.delete(`/rag/documents/${id}`);
  },
  
  // Search endpoints
  search: async (searchRequest: SearchRequest): Promise<SearchResult> => {
    const response = await apiClient.post<SearchResult>('/rag/search', searchRequest);
    return response.data;
  },
  
  // Chunk endpoints
  getChunksByDocumentId: async (documentId: string): Promise<Chunk[]> => {
    const response = await apiClient.get<Chunk[]>(`/rag/documents/${documentId}/chunks`);
    return response.data;
  }
};
