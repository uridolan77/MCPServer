# MCP Server API Usage Examples

This document provides examples of common API operations for the MCP Server API.

## Authentication

### Register a New User

```http
POST /api/auth/register
Content-Type: application/json

{
  "username": "newuser",
  "email": "user@example.com",
  "password": "SecurePassword123!"
}
```

#### Response

```json
{
  "success": true,
  "message": "User registered successfully",
  "data": {
    "username": "newuser",
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "c9f7e3b5-7a1c-4b5e-8d3a-1f2b3c4d5e6f"
  }
}
```

### Login

```http
POST /api/auth/login
Content-Type: application/json

{
  "username": "existinguser",
  "password": "SecurePassword123!"
}
```

#### Response

```json
{
  "success": true,
  "message": "Login successful",
  "data": {
    "username": "existinguser",
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d"
  }
}
```

### Refresh Token

```http
POST /api/auth/refresh
Content-Type: application/json

{
  "refreshToken": "a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d"
}
```

#### Response

```json
{
  "success": true,
  "message": "Token refreshed successfully",
  "data": {
    "username": "existinguser",
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "f6e5d4c3-b2a1-0c9b-8a7f-6e5d4c3b2a10"
  }
}
```

## LLM Providers

### Get All Providers

```http
GET /api/llm/providers
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

#### Response

```json
{
  "success": true,
  "message": "Providers retrieved successfully",
  "data": [
    {
      "id": 1,
      "name": "OpenAI",
      "displayName": "OpenAI",
      "description": "OpenAI API provider",
      "isEnabled": true
    },
    {
      "id": 2,
      "name": "Anthropic",
      "displayName": "Anthropic",
      "description": "Anthropic API provider",
      "isEnabled": true
    }
  ]
}
```

### Get Provider by ID

```http
GET /api/llm/providers/1
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

#### Response

```json
{
  "success": true,
  "message": "Provider retrieved successfully",
  "data": {
    "id": 1,
    "name": "OpenAI",
    "displayName": "OpenAI",
    "description": "OpenAI API provider",
    "isEnabled": true
  }
}
```

### Add New Provider (Admin Only)

```http
POST /api/llm/providers
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json

{
  "name": "Cohere",
  "displayName": "Cohere",
  "description": "Cohere API provider",
  "isEnabled": true
}
```

#### Response

```json
{
  "success": true,
  "message": "Provider added successfully",
  "data": {
    "id": 3,
    "name": "Cohere",
    "displayName": "Cohere",
    "description": "Cohere API provider",
    "isEnabled": true
  }
}
```

## LLM Models

### Get All Models

```http
GET /api/llm/models
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

#### Response

```json
{
  "success": true,
  "message": "Models retrieved successfully",
  "data": [
    {
      "id": 1,
      "name": "gpt-4",
      "modelId": "gpt-4",
      "providerId": 1,
      "provider": {
        "id": 1,
        "name": "OpenAI",
        "displayName": "OpenAI"
      },
      "costPer1KInputTokens": 0.03,
      "costPer1KOutputTokens": 0.06,
      "isEnabled": true
    },
    {
      "id": 2,
      "name": "claude-3-opus",
      "modelId": "claude-3-opus-20240229",
      "providerId": 2,
      "provider": {
        "id": 2,
        "name": "Anthropic",
        "displayName": "Anthropic"
      },
      "costPer1KInputTokens": 0.015,
      "costPer1KOutputTokens": 0.075,
      "isEnabled": true
    }
  ]
}
```

### Get Models by Provider ID

```http
GET /api/llm/models/provider/1
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

#### Response

```json
{
  "success": true,
  "message": "Models retrieved successfully",
  "data": [
    {
      "id": 1,
      "name": "gpt-4",
      "modelId": "gpt-4",
      "providerId": 1,
      "provider": {
        "id": 1,
        "name": "OpenAI",
        "displayName": "OpenAI"
      },
      "costPer1KInputTokens": 0.03,
      "costPer1KOutputTokens": 0.06,
      "isEnabled": true
    },
    {
      "id": 3,
      "name": "gpt-3.5-turbo",
      "modelId": "gpt-3.5-turbo",
      "providerId": 1,
      "provider": {
        "id": 1,
        "name": "OpenAI",
        "displayName": "OpenAI"
      },
      "costPer1KInputTokens": 0.0015,
      "costPer1KOutputTokens": 0.002,
      "isEnabled": true
    }
  ]
}
```

## Chat

### Send Message

```http
POST /api/chat/send
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json

{
  "message": "What is the capital of France?",
  "sessionId": "session-123",
  "modelId": 1
}
```

#### Response

```json
{
  "success": true,
  "message": "Message sent successfully",
  "data": {
    "message": "The capital of France is Paris. Paris is located in the north-central part of the country on the Seine River.",
    "sessionId": "session-123"
  }
}
```

### Stream Message

First, initiate the streaming request:

```http
POST /api/chat/stream
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json

{
  "message": "Tell me about artificial intelligence",
  "sessionId": "session-456",
  "modelId": 2
}
```

#### Response

```json
{
  "success": true,
  "message": "Streaming request stored successfully",
  "data": true
}
```

Then, connect to the streaming endpoint:

```http
GET /api/chat/stream?sessionId=session-456&token=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

This will establish a Server-Sent Events (SSE) connection that streams the response chunks.

## RAG (Retrieval-Augmented Generation)

### Upload Document

```http
POST /api/rag/documents
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: multipart/form-data

file: [binary file data]
title: "Sample Document"
description: "A sample document for testing"
```

#### Response

```json
{
  "success": true,
  "message": "Document uploaded and processed successfully",
  "data": {
    "id": 1,
    "title": "Sample Document",
    "description": "A sample document for testing",
    "fileName": "sample.pdf",
    "contentType": "application/pdf",
    "uploadDate": "2023-05-01T12:00:00Z",
    "status": "Processed",
    "chunkCount": 10
  }
}
```

### Query RAG

```http
POST /api/rag/query
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json

{
  "query": "What are the key points in the sample document?",
  "documentIds": [1],
  "maxResults": 5
}
```

#### Response

```json
{
  "success": true,
  "message": "Query processed successfully",
  "data": {
    "answer": "The key points in the sample document are...",
    "sources": [
      {
        "documentId": 1,
        "documentTitle": "Sample Document",
        "chunkId": 3,
        "content": "...",
        "relevanceScore": 0.92
      },
      {
        "documentId": 1,
        "documentTitle": "Sample Document",
        "chunkId": 7,
        "content": "...",
        "relevanceScore": 0.85
      }
    ]
  }
}
```

## Error Handling

All API endpoints return standardized error responses:

```json
{
  "success": false,
  "message": "Error message describing what went wrong",
  "errors": ["Detailed error 1", "Detailed error 2"],
  "data": null
}
```

Common HTTP status codes:
- 200 OK: Request succeeded
- 201 Created: Resource created successfully
- 400 Bad Request: Invalid input
- 401 Unauthorized: Authentication required
- 403 Forbidden: Insufficient permissions
- 404 Not Found: Resource not found
- 500 Internal Server Error: Server-side error
