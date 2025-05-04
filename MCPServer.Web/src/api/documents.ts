import axios from 'axios';

// Define the Document interface
export interface Document {
  id: string;
  name: string;
  type: string;
  content: string;
  size: number;
  tags: string[];
  createdAt: string;
  updatedAt: string;
}

// API base URL
const API_URL = 'http://localhost:2000/api';

// Get all documents
export const fetchDocuments = async (): Promise<Document[]> => {
  try {
    // For now, return mock data
    // In the future, this will be replaced with a real API call
    // const response = await axios.get(`${API_URL}/documents`);
    // return response.data;
    
    return mockDocuments;
  } catch (error) {
    console.error('Error fetching documents:', error);
    throw error;
  }
};

// Get document by ID
export const fetchDocumentById = async (id: string): Promise<Document> => {
  try {
    // For now, return mock data
    // In the future, this will be replaced with a real API call
    // const response = await axios.get(`${API_URL}/documents/${id}`);
    // return response.data;
    
    const document = mockDocuments.find(doc => doc.id === id);
    if (!document) {
      throw new Error('Document not found');
    }
    return document;
  } catch (error) {
    console.error(`Error fetching document with ID ${id}:`, error);
    throw error;
  }
};

// Create a new document
export const createDocument = async (document: Omit<Document, 'id' | 'createdAt' | 'updatedAt'>): Promise<Document> => {
  try {
    // For now, return mock data
    // In the future, this will be replaced with a real API call
    // const response = await axios.post(`${API_URL}/documents`, document);
    // return response.data;
    
    return {
      ...document,
      id: `doc-${Math.floor(Math.random() * 1000)}`,
      size: document.content.length / 1024, // Size in KB
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    };
  } catch (error) {
    console.error('Error creating document:', error);
    throw error;
  }
};

// Update an existing document
export const updateDocument = async (id: string, document: Partial<Document>): Promise<Document> => {
  try {
    // For now, return mock data
    // In the future, this will be replaced with a real API call
    // const response = await axios.put(`${API_URL}/documents/${id}`, document);
    // return response.data;
    
    const existingDocument = mockDocuments.find(doc => doc.id === id);
    if (!existingDocument) {
      throw new Error('Document not found');
    }
    
    return {
      ...existingDocument,
      ...document,
      updatedAt: new Date().toISOString(),
    };
  } catch (error) {
    console.error(`Error updating document with ID ${id}:`, error);
    throw error;
  }
};

// Delete a document
export const deleteDocument = async (id: string): Promise<void> => {
  try {
    // For now, just log the deletion
    // In the future, this will be replaced with a real API call
    // await axios.delete(`${API_URL}/documents/${id}`);
    console.log(`Document with ID ${id} deleted`);
  } catch (error) {
    console.error(`Error deleting document with ID ${id}:`, error);
    throw error;
  }
};

// Mock data for development
const mockDocuments: Document[] = [
  {
    id: 'doc-1',
    name: 'Project Requirements',
    type: 'text',
    content: 'This document outlines the requirements for the MCP Server project.',
    size: 2.5,
    tags: ['requirements', 'project', 'documentation'],
    createdAt: '2023-05-01T10:00:00Z',
    updatedAt: '2023-05-02T15:30:00Z',
  },
  {
    id: 'doc-2',
    name: 'API Documentation',
    type: 'text',
    content: 'API endpoints and usage examples for the MCP Server.',
    size: 5.2,
    tags: ['api', 'documentation', 'reference'],
    createdAt: '2023-05-03T09:15:00Z',
    updatedAt: '2023-05-03T14:45:00Z',
  },
  {
    id: 'doc-3',
    name: 'User Guide',
    type: 'pdf',
    content: 'Comprehensive guide for using the MCP Server application.',
    size: 10.7,
    tags: ['user', 'guide', 'documentation'],
    createdAt: '2023-05-04T11:30:00Z',
    updatedAt: '2023-05-05T16:20:00Z',
  },
  {
    id: 'doc-4',
    name: 'System Architecture',
    type: 'word',
    content: 'Detailed architecture of the MCP Server system.',
    size: 7.8,
    tags: ['architecture', 'system', 'design'],
    createdAt: '2023-05-06T08:45:00Z',
    updatedAt: '2023-05-07T13:10:00Z',
  },
  {
    id: 'doc-5',
    name: 'Database Schema',
    type: 'excel',
    content: 'Database schema and entity relationships for the MCP Server.',
    size: 3.4,
    tags: ['database', 'schema', 'reference'],
    createdAt: '2023-05-08T12:20:00Z',
    updatedAt: '2023-05-09T17:05:00Z',
  },
];
