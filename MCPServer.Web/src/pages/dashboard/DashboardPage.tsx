import React from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Box,
  Grid,
  Paper,
  Typography,
  Card,
  CardContent,
  CardActions,
  Button,
  Divider,
  List,
  ListItem,
  ListItemText,
  ListItemIcon
} from '@mui/material';
import {
  SmartToy as SmartToyIcon,
  Storage as StorageIcon,
  VpnKey as VpnKeyIcon,
  Description as DescriptionIcon,
  BarChart as BarChartIcon,
  People as PeopleIcon,
  Chat as ChatIcon
} from '@mui/icons-material';
import { PageHeader } from '@/components';
import { useAuth } from '@/contexts/AuthContext';

const DashboardPage: React.FC = () => {
  const navigate = useNavigate();
  const { user } = useAuth();

  const isAdmin = user?.roles.includes('Admin');

  const cards = [
    {
      title: 'LLM Providers',
      description: 'Manage your LLM providers and their configurations.',
      icon: <SmartToyIcon fontSize="large" color="primary" />,
      path: '/providers',
      color: '#e3f2fd'
    },
    {
      title: 'LLM Models',
      description: 'Configure and manage available language models.',
      icon: <StorageIcon fontSize="large" color="secondary" />,
      path: '/models',
      color: '#f3e5f5'
    },
    {
      title: 'API Keys',
      description: 'Manage API keys and credentials for LLM providers.',
      icon: <VpnKeyIcon fontSize="large" color="success" />,
      path: '/credentials',
      color: '#e8f5e9'
    },
    {
      title: 'RAG Documents',
      description: 'Manage your knowledge base documents for retrieval.',
      icon: <DescriptionIcon fontSize="large" color="info" />,
      path: '/documents',
      color: '#e1f5fe'
    },
    {
      title: 'Chat Playground',
      description: 'Test and interact with your configured LLM providers.',
      icon: <ChatIcon fontSize="large" color="warning" />,
      path: '/playground',
      color: '#fff8e1'
    },
    {
      title: 'Usage Statistics',
      description: 'View usage metrics and cost information.',
      icon: <BarChartIcon fontSize="large" color="warning" />,
      path: '/usage',
      color: '#fff8e1'
    }
  ];

  // Add Users card for admins
  if (isAdmin) {
    cards.push({
      title: 'User Management',
      description: 'Manage users and their permissions.',
      icon: <PeopleIcon fontSize="large" color="error" />,
      path: '/users',
      color: '#ffebee'
    });
  }

  return (
    <Box>
      <PageHeader
        title={`Welcome, ${user?.firstName || user?.username}!`}
        subtitle="MCP Server Administration Dashboard"
      />

      <Grid container spacing={3}>
        {cards.map((card, index) => (
          <Grid item xs={12} sm={6} md={4} key={index}>
            <Card
              sx={{
                height: '100%',
                display: 'flex',
                flexDirection: 'column',
                backgroundColor: card.color,
                transition: 'transform 0.2s',
                '&:hover': {
                  transform: 'translateY(-4px)',
                  boxShadow: 4
                }
              }}
            >
              <CardContent sx={{ flexGrow: 1, pt: 2 }}>
                <Box sx={{ display: 'flex', justifyContent: 'center', mb: 2 }}>
                  {card.icon}
                </Box>
                <Typography gutterBottom variant="h5" component="h2" align="center">
                  {card.title}
                </Typography>
                <Typography variant="body2" color="text.secondary" align="center">
                  {card.description}
                </Typography>
              </CardContent>
              <CardActions>
                <Button
                  size="small"
                  fullWidth
                  onClick={() => navigate(card.path)}
                >
                  Manage
                </Button>
              </CardActions>
            </Card>
          </Grid>
        ))}
      </Grid>

      <Grid container spacing={3} sx={{ mt: 2 }}>
        <Grid item xs={12} md={6}>
          <Paper sx={{ p: 2, height: '100%' }}>
            <Typography variant="h6" gutterBottom>
              Quick Links
            </Typography>
            <Divider sx={{ mb: 2 }} />
            <List>
              <ListItem button onClick={() => navigate('/providers/new')}>
                <ListItemIcon>
                  <SmartToyIcon color="primary" />
                </ListItemIcon>
                <ListItemText primary="Add New Provider" />
              </ListItem>
              <ListItem button onClick={() => navigate('/models/new')}>
                <ListItemIcon>
                  <StorageIcon color="secondary" />
                </ListItemIcon>
                <ListItemText primary="Add New Model" />
              </ListItem>
              <ListItem button onClick={() => navigate('/credentials/new')}>
                <ListItemIcon>
                  <VpnKeyIcon color="success" />
                </ListItemIcon>
                <ListItemText primary="Add New API Key" />
              </ListItem>
              <ListItem button onClick={() => navigate('/playground')}>
                <ListItemIcon>
                  <ChatIcon color="warning" />
                </ListItemIcon>
                <ListItemText primary="Open Chat Playground" />
              </ListItem>
              <ListItem button onClick={() => navigate('/documents/new')}>
                <ListItemIcon>
                  <DescriptionIcon color="info" />
                </ListItemIcon>
                <ListItemText primary="Add New Document" />
              </ListItem>
            </List>
          </Paper>
        </Grid>

        <Grid item xs={12} md={6}>
          <Paper sx={{ p: 2, height: '100%' }}>
            <Typography variant="h6" gutterBottom>
              System Status
            </Typography>
            <Divider sx={{ mb: 2 }} />
            <Box sx={{ p: 2 }}>
              <Typography variant="body1" gutterBottom>
                All systems operational
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Last updated: {new Date().toLocaleString()}
              </Typography>
            </Box>
          </Paper>
        </Grid>
      </Grid>
    </Box>
  );
};

export default DashboardPage;
