import React from 'react';
import { Link as RouterLink } from 'react-router-dom';
import { Box, Button, Typography, Paper } from '@mui/material';
import LockIcon from '@mui/icons-material/Lock';

const UnauthorizedPage: React.FC = () => {
  return (
    <Box
      sx={{
        display: 'flex',
        justifyContent: 'center',
        alignItems: 'center',
        height: '100%',
      }}
    >
      <Paper
        elevation={3}
        sx={{
          p: 4,
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
          maxWidth: 500,
        }}
      >
        <LockIcon sx={{ fontSize: 80, color: 'warning.main', mb: 2 }} />
        
        <Typography variant="h4" component="h1" gutterBottom>
          403: Access Denied
        </Typography>
        
        <Typography variant="body1" color="text.secondary" align="center" sx={{ mb: 3 }}>
          You don't have permission to access this page. Please contact your administrator if you believe this is an error.
        </Typography>
        
        <Button
          component={RouterLink}
          to="/dashboard"
          variant="contained"
          color="primary"
          sx={{ mt: 2 }}
        >
          Go to Dashboard
        </Button>
      </Paper>
    </Box>
  );
};

export default UnauthorizedPage;
