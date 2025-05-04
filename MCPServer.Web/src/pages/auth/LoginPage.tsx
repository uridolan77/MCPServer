import React, { useState } from 'react';
import { useNavigate, Link as RouterLink } from 'react-router-dom';
import {
  Box,
  Button,
  TextField,
  Link,
  Alert,
  CircularProgress
} from '@mui/material';
import { useAuth } from '@/contexts/AuthContext';
import logoImage from '@/assets/logo.png';

const LoginPage: React.FC = () => {
  const { login } = useAuth();
  const navigate = useNavigate();
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [formData, setFormData] = useState({
    username: '',
    password: ''
  });

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    setFormData(prev => ({
      ...prev,
      [name]: value
    }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsLoading(true);
    setError(null);

    try {
      // Log the request for debugging
      console.log('Login attempt with:', { username: formData.username });

      await login(formData.username, formData.password);
      console.log('Login successful, navigating to credentials');
      navigate('/credentials');
    } catch (error: any) {
      // Enhanced error logging
      console.error('Login error:', error);
      console.error('Error details:', {
        message: error.message,
        response: error.response,
        data: error.response?.data,
        status: error.response?.status
      });

      // Set a more descriptive error message
      if (error.response?.data?.message) {
        setError(`Server error: ${error.response.data.message}`);
      } else if (error.response?.data?.errors && error.response.data.errors.length > 0) {
        setError(`Error: ${error.response.data.errors.join(', ')}`);
      } else if (error.message) {
        setError(`Error: ${error.message}`);
      } else {
        setError('Login failed. Please check your credentials and try again.');
      }
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <Box sx={{
      display: 'flex',
      flexDirection: 'column',
      alignItems: 'center',
      mt: 4
    }}>
      <Box sx={{ mb: 3, textAlign: 'center' }}>
        <img
          src={logoImage}
          alt="Logo"
          style={{ width: '1200px', marginBottom: '16px' }}
        />
      </Box>

      <Box component="form" onSubmit={handleSubmit} noValidate sx={{ mt: 1, width: '100%', maxWidth: '400px' }}>
        {error && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {error}
          </Alert>
        )}

        <TextField
          margin="normal"
          required
          fullWidth
          id="username"
          label="Username"
          name="username"
          autoComplete="username"
          autoFocus
          value={formData.username}
          onChange={handleChange}
          disabled={isLoading}
        />

        <TextField
          margin="normal"
          required
          fullWidth
          name="password"
          label="Password"
          type="password"
          id="password"
          autoComplete="current-password"
          value={formData.password}
          onChange={handleChange}
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
          {isLoading ? (
            <CircularProgress size={24} color="inherit" />
          ) : (
            'Sign In'
          )}
        </Button>

        <Box sx={{ display: 'flex', justifyContent: 'space-between', mt: 2 }}>
          <Link component={RouterLink} to="/forgot-password" variant="body2">
            Forgot password?
          </Link>
          <Link component={RouterLink} to="/register" variant="body2">
            {"Don't have an account? Sign Up"}
          </Link>
        </Box>
      </Box>
    </Box>
  );
};

export default LoginPage;
