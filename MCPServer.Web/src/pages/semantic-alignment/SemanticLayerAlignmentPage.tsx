import React, { useState, useEffect } from 'react';
import { Box, Typography, Button } from '@mui/material'; // Added Typography and Button
import { PageHeader } from '@/components';
import { SemanticLayerAlignmentWizard } from '@/components/SemanticLayerAlignmentWizard';
import { useSnackbar } from '@/hooks/useSnackbar';
import { useNavigate } from 'react-router-dom';

const SemanticLayerAlignmentPage: React.FC = () => {
  const [isWizardOpen, setIsWizardOpen] = useState(true); // Open wizard by default when page loads
  const { showSnackbar } = useSnackbar();
  const navigate = useNavigate();

  const handleCloseWizard = () => {
    setIsWizardOpen(false);
    // Optionally navigate away or show a message, for now, just closes.
    // navigate('/dashboard'); // Example: navigate to dashboard after closing
    showSnackbar('Semantic Layer Alignment Wizard closed.', 'info');
  };

  const handleWizardComplete = (data: any) => {
    console.log('Semantic Layer Alignment Wizard completed on page', data);
    showSnackbar('Semantic Layer Alignment process completed successfully!', 'success');
    handleCloseWizard();
    // Potentially navigate to a relevant page or refresh data
  };

  // Effect to re-open wizard if user navigates back to this page and it was closed
  useEffect(() => {
    setIsWizardOpen(true);
  }, []);

  return (
    <Box sx={{ width: '100%', height: '100%', display: 'flex', flexDirection: 'column' }}>
      <PageHeader
        title="Semantic Layer Alignment"
        subtitle="Align database schemas with Semantic Layer Ontology Definitions (SLOD)"
      />
      <Box sx={{ p: 3, flexGrow: 1 }}>
        {/* The wizard is a dialog, so it will overlay. 
            You can add other page content here if needed, 
            or this page can solely be for launching the wizard. */}
        <Typography>
          The Semantic Layer Alignment Wizard should be open. If not, there might be an issue or it was closed.
        </Typography>
        {/* This button is for re-opening if it was somehow closed and user is still on page */}
        {!isWizardOpen && (
            <Button variant="contained" onClick={() => setIsWizardOpen(true)}>
                Re-open Alignment Wizard
            </Button>
        )}
      </Box>
      <SemanticLayerAlignmentWizard
        open={isWizardOpen}
        onClose={handleCloseWizard}
        onComplete={handleWizardComplete}
      />
    </Box>
  );
};

export default SemanticLayerAlignmentPage;
