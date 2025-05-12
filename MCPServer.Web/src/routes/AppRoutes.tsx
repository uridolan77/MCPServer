import React, { lazy, Suspense } from 'react';
import { Routes, Route, Navigate } from 'react-router-dom';
import { Box, CircularProgress, Typography } from '@mui/material';
import { MainLayout, AuthLayout } from '@/layouts';
import ProtectedRoute from './ProtectedRoute';

// Lazy load pages
const LoginPage = lazy(() => import('@/pages/auth/LoginPage'));
// Temporarily comment out pages that don't exist yet
// const RegisterPage = lazy(() => import('@/pages/auth/RegisterPage'));
// const ForgotPasswordPage = lazy(() => import('@/pages/auth/ForgotPasswordPage'));
const DashboardPage = lazy(() => import('@/pages/dashboard/DashboardPage'));
const ProvidersListPage = lazy(() => import('@/pages/providers/ProvidersListPage'));
const ProviderDetailsPage = lazy(() => import('@/pages/providers/ProviderDetailsPage'));
const ModelsListPage = lazy(() => import('@/pages/models/ModelsListPage'));
const ModelDetailsPage = lazy(() => import('@/pages/models/ModelDetailsPage'));
const CredentialsListPage = lazy(() => import('@/pages/credentials/CredentialsListPage'));
const CredentialDetailsPage = lazy(() => import('@/pages/credentials/CredentialDetailsPage'));
const DocumentsListPage = lazy(() => import('@/pages/documents/DocumentsListPage'));
const DocumentDetailsPage = lazy(() => import('@/pages/documents/DocumentDetailsPage'));
const UsageStatsPage = lazy(() => import('@/pages/usage/UsageStatsPage'));
const UsersListPage = lazy(() => import('@/pages/users/UsersListPage'));
const UserDetailsPage = lazy(() => import('@/pages/users/UserDetailsPage'));
const ChatPlaygroundPage = lazy(() => import('@/pages/playground/ChatPlaygroundPage'));
const DataTransferPage = lazy(() => import('@/pages/transfer/DataTransferPage'));
const DatabaseSchemaMapperPage = lazy(() => import('@/pages/transfer/DatabaseSchemaMapperPage'));
const SemanticLayerAlignmentPage = lazy(() => import('@/pages/semantic-alignment/SemanticLayerAlignmentPage'));
// const ProfilePage = lazy(() => import('@/pages/profile/ProfilePage'));
// const NotFoundPage = lazy(() => import('@/pages/errors/NotFoundPage'));
// const UnauthorizedPage = lazy(() => import('@/pages/errors/UnauthorizedPage'));

// Loading component for suspense fallback
const LoadingFallback = () => (
  <Box
    sx={{
      display: 'flex',
      flexDirection: 'column',
      alignItems: 'center',
      justifyContent: 'center',
      height: '100vh',
    }}
  >
    <CircularProgress size={60} />
    <Typography variant="h6" sx={{ mt: 2 }}>
      Loading...
    </Typography>
  </Box>
);

export const AppRoutes: React.FC = () => {
  return (
    <Suspense fallback={<LoadingFallback />}>
      <Routes>
        {/* Auth routes */}
        <Route element={<AuthLayout />}>
          <Route path="/login" element={<LoginPage />} />
          {/* <Route path="/register" element={<RegisterPage />} />
          <Route path="/forgot-password" element={<ForgotPasswordPage />} /> */}
        </Route>

        {/* Protected routes */}
        <Route
          element={
            <ProtectedRoute>
              <MainLayout />
            </ProtectedRoute>
          }
        >
          <Route path="/" element={<Navigate to="/dashboard" replace />} />
          <Route path="/dashboard" element={<DashboardPage />} />

          {/* Provider routes */}
          <Route path="/providers" element={<ProvidersListPage />} />
          <Route path="/providers/new" element={<ProviderDetailsPage />} />
          <Route path="/providers/:id" element={<ProviderDetailsPage />} />

          {/* Model routes */}
          <Route path="/models" element={<ModelsListPage />} />
          <Route path="/models/new" element={<ModelDetailsPage />} />
          <Route path="/models/:id" element={<ModelDetailsPage />} />

          {/* Credential routes */}
          <Route path="/credentials" element={<CredentialsListPage />} />
          <Route path="/credentials/new" element={<CredentialDetailsPage />} />
          <Route path="/credentials/:id" element={<CredentialDetailsPage />} />

          {/* Document routes */}
          <Route path="/documents" element={<DocumentsListPage />} />
          <Route path="/documents/new" element={<DocumentDetailsPage />} />
          <Route path="/documents/:id" element={<DocumentDetailsPage />} />

          {/* Usage routes */}
          <Route path="/usage" element={<UsageStatsPage />} />

          {/* Data Transfer routes */}
          <Route path="/data-transfer" element={<DataTransferPage />} />
          <Route path="/transfer/schema-mapper" element={<DatabaseSchemaMapperPage />} />
          <Route path="/semantic-alignment" element={<SemanticLayerAlignmentPage />} />

          {/* Chat Playground */}
          <Route path="/playground" element={<ChatPlaygroundPage />} />

          {/* User routes (admin only) */}
          <Route
            path="/users"
            element={
              <ProtectedRoute requiredRoles={['Admin']}>
                <UsersListPage />
              </ProtectedRoute>
            }
          />
          <Route
            path="/users/new"
            element={
              <ProtectedRoute requiredRoles={['Admin']}>
                <UserDetailsPage />
              </ProtectedRoute>
            }
          />
          <Route
            path="/users/:id"
            element={
              <ProtectedRoute requiredRoles={['Admin']}>
                <UserDetailsPage />
              </ProtectedRoute>
            }
          />

          {/* Profile route */}
          {/* <Route path="/profile" element={<ProfilePage />} /> */}

          {/* Error routes */}
          {/* <Route path="/unauthorized" element={<UnauthorizedPage />} />
          <Route path="*" element={<NotFoundPage />} /> */}
        </Route>
      </Routes>
    </Suspense>
  );
};


