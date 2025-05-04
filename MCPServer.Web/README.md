# MCP Server Admin Web Interface

This is the web administration interface for the MCP Server, built with modern web technologies.

## Technology Stack

- **React**: UI library for building component-based interfaces
- **TypeScript**: Type-safe JavaScript for improved developer experience
- **Material UI**: Component library for consistent, modern UI design
- **React Query**: Data fetching, caching, and state management
- **React Router**: Client-side routing
- **Zod**: Schema validation
- **Recharts**: Data visualization
- **Vite**: Fast, modern build tool

## Features

- **Authentication**: Secure login and user management
- **LLM Provider Management**: Configure and manage LLM providers
- **LLM Model Management**: Configure and manage language models
- **API Key Management**: Securely store and manage API credentials
- **RAG Document Management**: Manage knowledge base documents
- **Usage Statistics**: Monitor usage and costs
- **User Management**: Admin tools for user administration

## Project Structure

The project follows a feature-based organization:

- `src/api`: API client and endpoints
- `src/components`: Reusable UI components
- `src/contexts`: React contexts (auth, theme, etc.)
- `src/features`: Feature-specific components
- `src/hooks`: Custom React hooks
- `src/layouts`: Layout components
- `src/pages`: Page components
- `src/routes`: Routing configuration
- `src/types`: TypeScript type definitions
- `src/utils`: Utility functions

## Development

### Prerequisites

- Node.js (v18+)
- npm or yarn

### Getting Started

1. Install dependencies:

   ```bash
   npm install
   ```

2. Start the development server:

   ```bash
   npm run dev
   ```

3. Build for production:

   ```bash
   npm run build
   ```
