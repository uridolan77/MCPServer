import React, { useState } from 'react';
import { useNavigate, Link as RouterLink } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Box, Button, Typography, Link, Alert, Grid } from '@mui/material';
import { TextFormField } from '@/components';
import { useAuth } from '@/contexts/AuthContext';
import { validationUtils } from '@/utils';
import { useErrorHandler } from '@/hooks';

type RegisterFormData = {
  username: string;
  email: string;
  password: string;
  confirmPassword: string;
  firstName?: string;
  lastName?: string;
};

const RegisterPage: React.FC = () => {
  const { register } = useAuth();
  const navigate = useNavigate();
  const { handleError } = useErrorHandler();
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const {
    control,
    handleSubmit,
    formState: { errors },
  } = useForm<RegisterFormData>({
    resolver: zodResolver(validationUtils.registerSchema),
    defaultValues: {
      username: '',
      email: '',
      password: '',
      confirmPassword: '',
      firstName: '',
      lastName: '',
    },
  });

  const onSubmit = async (data: RegisterFormData) => {
    setIsLoading(true);
    setError(null);

    try {
      await register(
        data.username,
        data.email,
        data.password,
        data.firstName,
        data.lastName
      );
      navigate('/dashboard');
    } catch (error: any) {
      setError(
        error.response?.data?.message || 
        'Registration failed. Please try again with different credentials.'
      );
      handleError(error, 'Registration failed');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <Box component="form" onSubmit={handleSubmit(onSubmit)} noValidate>
      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}

      <TextFormField
        name="username"
        control={control}
        label="Username"
        error={errors.username}
        required
        disabled={isLoading}
      />

      <TextFormField
        name="email"
        control={control}
        label="Email Address"
        type="email"
        error={errors.email}
        required
        disabled={isLoading}
      />

      <Grid container spacing={2}>
        <Grid item xs={12} sm={6}>
          <TextFormField
            name="firstName"
            control={control}
            label="First Name"
            error={errors.firstName}
            disabled={isLoading}
          />
        </Grid>
        <Grid item xs={12} sm={6}>
          <TextFormField
            name="lastName"
            control={control}
            label="Last Name"
            error={errors.lastName}
            disabled={isLoading}
          />
        </Grid>
      </Grid>

      <TextFormField
        name="password"
        control={control}
        label="Password"
        type="password"
        error={errors.password}
        required
        disabled={isLoading}
        helperText="Password must be at least 8 characters and include uppercase, lowercase, number, and special character"
      />

      <TextFormField
        name="confirmPassword"
        control={control}
        label="Confirm Password"
        type="password"
        error={errors.confirmPassword}
        required
        disabled={isLoading}
      />

      <Button
        type="submit"
        fullWidth
        variant="contained"
        color="primary"
        disabled={isLoading}
        sx={{ mt: 3, mb: 2 }}
      >
        {isLoading ? 'Signing up...' : 'Sign Up'}
      </Button>

      <Box sx={{ display: 'flex', justifyContent: 'center', mt: 2 }}>
        <Link component={RouterLink} to="/login" variant="body2">
          {"Already have an account? Sign In"}
        </Link>
      </Box>
    </Box>
  );
};

export default RegisterPage;
