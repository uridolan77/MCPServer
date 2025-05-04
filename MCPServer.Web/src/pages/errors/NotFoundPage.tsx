import React from 'react';
import { Link as RouterLink } from 'react-router-dom';
import { Box, Button, Typography, Paper } from '@mui/material';
import ErrorOutlineIcon from '@mui/icons-material/ErrorOutline';

const NotFoundPage: React.FC = () => {
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
        <ErrorOutlineIcon sx={{ fontSize: 80, color: 'error.main', mb: 2 }} />
        
        <Typography variant="h4" component="h1" gutterBottom>
          404: Page Not Found
        </Typography>
        
        <Typography variant="body1" color="text.secondary" align="center" sx={{ mb: 3 }}>
          The page you are looking for doesn't exist or has been moved.
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

export default NotFoundPage;
