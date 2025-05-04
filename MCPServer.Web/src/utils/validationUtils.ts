import { z } from 'zod';

// Common validation schemas
export const usernameSchema = z
  .string()
  .min(3, 'Username must be at least 3 characters')
  .max(50, 'Username must be at most 50 characters')
  .regex(/^[a-zA-Z0-9_]+$/, 'Username can only contain letters, numbers, and underscores');

export const emailSchema = z
  .string()
  .email('Invalid email address')
  .max(100, 'Email must be at most 100 characters');

export const passwordSchema = z
  .string()
  .min(8, 'Password must be at least 8 characters')
  .max(100, 'Password must be at most 100 characters')
  .regex(/[A-Z]/, 'Password must contain at least one uppercase letter')
  .regex(/[a-z]/, 'Password must contain at least one lowercase letter')
  .regex(/[0-9]/, 'Password must contain at least one number')
  .regex(/[^A-Za-z0-9]/, 'Password must contain at least one special character');

export const nameSchema = z
  .string()
  .min(1, 'Name is required')
  .max(100, 'Name must be at most 100 characters');

export const urlSchema = z
  .string()
  .url('Invalid URL')
  .max(500, 'URL must be at most 500 characters');

// Login form schema
export const loginSchema = z.object({
  username: usernameSchema,
  password: z.string().min(1, 'Password is required'),
});

// Registration form schema
export const registerSchema = z.object({
  username: usernameSchema,
  email: emailSchema,
  password: passwordSchema,
  confirmPassword: z.string(),
  firstName: z.string().optional(),
  lastName: z.string().optional(),
}).refine(data => data.password === data.confirmPassword, {
  message: 'Passwords do not match',
  path: ['confirmPassword'],
});

// Provider form schema
export const providerSchema = z.object({
  name: nameSchema,
  displayName: z.string().min(1, 'Display name is required').max(100, 'Display name must be at most 100 characters'),
  apiEndpoint: urlSchema,
  description: z.string().max(1000, 'Description must be at most 1000 characters').optional(),
  isEnabled: z.boolean(),
  authType: z.string().min(1, 'Auth type is required'),
  configSchema: z.string().min(2, 'Config schema is required'),
});

// Model form schema
export const modelSchema = z.object({
  providerId: z.number().min(1, 'Provider is required'),
  name: nameSchema,
  modelId: z.string().min(1, 'Model ID is required').max(100, 'Model ID must be at most 100 characters'),
  description: z.string().max(1000, 'Description must be at most 1000 characters').optional(),
  maxTokens: z.number().min(1, 'Max tokens must be at least 1'),
  contextWindow: z.number().min(1, 'Context window must be at least 1'),
  supportsStreaming: z.boolean(),
  supportsVision: z.boolean(),
  supportsTools: z.boolean(),
  costPer1KInputTokens: z.number().min(0, 'Cost must be at least 0'),
  costPer1KOutputTokens: z.number().min(0, 'Cost must be at least 0'),
  isEnabled: z.boolean(),
});

// Credential form schema
export const credentialSchema = z.object({
  providerId: z.union([
    z.string().min(1, 'Provider is required'),
    z.number().min(1, 'Provider is required')
  ]),
  name: nameSchema,
  credentials: z.record(z.string(), z.any()).optional(),
  isDefault: z.boolean(),
  apiKey: z.string().optional(),
  organizationId: z.string().optional(),
}).transform((data) => {
  // Convert providerId to number if it's a string and can be parsed as a number
  if (typeof data.providerId === 'string' && /^\d+$/.test(data.providerId)) {
    return {
      ...data,
      providerId: parseInt(data.providerId, 10)
    };
  }
  return data;
});

// Document form schema
export const documentSchema = z.object({
  title: z.string().min(1, 'Title is required').max(200, 'Title must be at most 200 characters'),
  content: z.string().min(1, 'Content is required'),
  source: z.string().max(200, 'Source must be at most 200 characters').optional(),
  url: z.string().max(500, 'URL must be at most 500 characters').optional(),
  tags: z.array(z.string()),
  metadata: z.record(z.string(), z.string()),
});
