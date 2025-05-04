import React, { useState } from 'react';
import { Link as RouterLink } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Box, Button, Typography, Link, Alert } from '@mui/material';
import { z } from 'zod';
import { TextFormField } from '@/components';
import { useErrorHandler } from '@/hooks';

const forgotPasswordSchema = z.object({
  email: z.string().email('Invalid email address').min(1, 'Email is required'),
});

type ForgotPasswordFormData = {
  email: string;
};

const ForgotPasswordPage: React.FC = () => {
  const { handleError } = useErrorHandler();
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const {
    control,
    handleSubmit,
    formState: { errors },
  } = useForm<ForgotPasswordFormData>({
    resolver: zodResolver(forgotPasswordSchema),
    defaultValues: {
      email: '',
    },
  });

  const onSubmit = async (data: ForgotPasswordFormData) => {
    setIsLoading(true);
    setError(null);
    setSuccess(null);

    try {
      // TODO: Implement password reset API call
      // await authApi.resetPassword(data.email);
      
      // For now, just simulate a successful request
      setTimeout(() => {
        setSuccess('Password reset instructions have been sent to your email.');
        setIsLoading(false);
      }, 1500);
    } catch (error: any) {
      setError(
        error.response?.data?.message || 
        'Failed to send password reset email. Please try again.'
      );
      handleError(error, 'Password reset request failed');
      setIsLoading(false);
    }
  };

  return (
    <Box component="form" onSubmit={handleSubmit(onSubmit)} noValidate>
      <Typography variant="h6" gutterBottom>
        Reset Your Password
      </Typography>
      
      <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
        Enter your email address and we'll send you instructions to reset your password.
      </Typography>

      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}

      {success && (
        <Alert severity="success" sx={{ mb: 2 }}>
          {success}
        </Alert>
      )}

      <TextFormField
        name="email"
        control={control}
        label="Email Address"
        type="email"
        error={errors.email}
        required
        disabled={isLoading || !!success}
      />

      <Button
        type="submit"
        fullWidth
        variant="contained"
        color="primary"
        disabled={isLoading || !!success}
        sx={{ mt: 3, mb: 2 }}
      >
        {isLoading ? 'Sending...' : 'Reset Password'}
      </Button>

      <Box sx={{ display: 'flex', justifyContent: 'center', mt: 2 }}>
        <Link component={RouterLink} to="/login" variant="body2">
          {"Back to Sign In"}
        </Link>
      </Box>
    </Box>
  );
};

export default ForgotPasswordPage;
