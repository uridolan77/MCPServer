import React, { useState } from 'react';
import {
  Box,
  Typography,
  Button,
  Paper,
  Grid,
  Card,
  CardContent,
  CardActions,
  Divider
} from '@mui/material';
import {
  Storage as StorageIcon,
  Schema as SchemaIcon,
  Compare as CompareIcon,
  Code as CodeIcon,
  ArrowForward as ArrowForwardIcon,
  Storage as DatabaseIcon // Using Storage icon as a replacement for Database
} from '@mui/icons-material';
import { PageHeader } from '@/components';
import { ConnectDataSourceWizard } from '@/components/ConnectionWizard';

const DatabaseSchemaMapperPage: React.FC = () => {
  const [isWizardOpen, setIsWizardOpen] = useState(false);

  const handleOpenWizard = () => {
    setIsWizardOpen(true);
  };

  const handleCloseWizard = () => {
    setIsWizardOpen(false);
  };

  const handleWizardComplete = (data: any) => {
    console.log('Wizard completed with data:', data);
    // You can handle the completed wizard data here
  };

  return (
    <Box sx={{ width: '100%', height: '100%', display: 'flex', flexDirection: 'column' }}>
      <PageHeader
        title="Database Schema Mapper"
        subtitle="Map database schemas and generate schema metadata JSON"
      />

      <Box sx={{ flexGrow: 1, p: 3 }}>
        <Grid container spacing={3}>
          <Grid item xs={12}>
            <Paper sx={{ p: 3, mb: 3 }}>
              <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                <SchemaIcon color="primary" sx={{ fontSize: 28, mr: 2 }} />
                <Typography variant="h5" component="h2">
                  Database Schema Mapping Tool
                </Typography>
              </Box>
              <Typography variant="body1" paragraph>
                This tool helps you connect to a database, explore its schema, select the tables and columns
                you want to include, and generate a standardized schema JSON file according to your project's meta-schema format.
              </Typography>
              <Button
                variant="contained"
                color="primary"
                size="large"
                startIcon={<DatabaseIcon />}
                onClick={handleOpenWizard}
              >
                Connect Data Source
              </Button>
            </Paper>
          </Grid>

          <Grid item xs={12} md={6}>
            <Card sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
              <CardContent sx={{ flexGrow: 1 }}>
                <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                  <StorageIcon color="primary" sx={{ fontSize: 24, mr: 1 }} />
                  <Typography variant="h6" component="h3">
                    Database Connection
                  </Typography>
                </Box>
                <Typography variant="body2" color="text.secondary" paragraph>
                  Connect to various database systems including SQL Server, MySQL, and PostgreSQL.
                  Configure connection settings and test connectivity before proceeding.
                </Typography>
                <Divider sx={{ my: 2 }} />
                <Typography variant="subtitle2" gutterBottom>
                  Supported features:
                </Typography>
                <ul style={{ paddingLeft: '1.5rem' }}>
                  <li>SQL Server, MySQL, PostgreSQL support</li>
                  <li>Connection string building</li>
                  <li>Authentication options</li>
                  <li>Connection pooling configuration</li>
                </ul>
              </CardContent>
              <CardActions>
                <Button size="small" endIcon={<ArrowForwardIcon />}>
                  Learn More
                </Button>
              </CardActions>
            </Card>
          </Grid>

          <Grid item xs={12} md={6}>
            <Card sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
              <CardContent sx={{ flexGrow: 1 }}>
                <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                  <CompareIcon color="primary" sx={{ fontSize: 24, mr: 1 }} />
                  <Typography variant="h6" component="h3">
                    Schema Selection
                  </Typography>
                </Box>
                <Typography variant="body2" color="text.secondary" paragraph>
                  Browse database objects and select specific tables and columns to include in your schema.
                  Filter and search for specific database objects.
                </Typography>
                <Divider sx={{ my: 2 }} />
                <Typography variant="subtitle2" gutterBottom>
                  Key capabilities:
                </Typography>
                <ul style={{ paddingLeft: '1.5rem' }}>
                  <li>Hierarchical object browser</li>
                  <li>Table and column selection</li>
                  <li>Primary key and identity column detection</li>
                  <li>Data type information preservation</li>
                </ul>
              </CardContent>
              <CardActions>
                <Button size="small" endIcon={<ArrowForwardIcon />}>
                  Learn More
                </Button>
              </CardActions>
            </Card>
          </Grid>

          <Grid item xs={12}>
            <Card>
              <CardContent>
                <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                  <CodeIcon color="primary" sx={{ fontSize: 24, mr: 1 }} />
                  <Typography variant="h6" component="h3">
                    JSON Schema Generation
                  </Typography>
                </Box>
                <Typography variant="body2" color="text.secondary" paragraph>
                  Generate a standardized schema JSON file that conforms to the db-meta-schema.json format.
                  This schema can be used for data transfer configurations, documentation, and more.
                </Typography>
                <Paper variant="outlined" sx={{ p: 2, bgcolor: 'grey.50' }}>
                  <Typography variant="caption" component="pre" sx={{ fontFamily: 'monospace', whiteSpace: 'pre-wrap' }}>
{`{
  "metadata": {
    "id": "database-schema-v1",
    "version": "1.0.0",
    "name": "Database Schema",
    "description": "Schema definition for the database"
  },
  "entities": [
    {
      "name": "Database",
      "description": "Represents a database instance",
      "values": [...]
    },
    {
      "name": "Schema",
      "description": "Represents a schema within a database",
      "values": [...]
    },
    // ...more entities
  ],
  "relationships": [
    // Relationship definitions
  ]
}`}
                  </Typography>
                </Paper>
              </CardContent>
              <CardActions>
                <Button size="small" endIcon={<ArrowForwardIcon />}>
                  View Schema Format
                </Button>
              </CardActions>
            </Card>
          </Grid>
        </Grid>
      </Box>

      {/* Database Connection Wizard */}
      <ConnectDataSourceWizard
        open={isWizardOpen}
        onClose={handleCloseWizard}
        onComplete={handleWizardComplete}
      />
    </Box>
  );
};

export default DatabaseSchemaMapperPage;