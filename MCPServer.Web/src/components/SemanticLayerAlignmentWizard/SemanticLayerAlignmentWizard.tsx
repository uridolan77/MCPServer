import React, { useState, useCallback } from 'react';
import { Dialog, DialogTitle, DialogContent, IconButton } from '@mui/material';
import { Close as CloseIcon } from '@mui/icons-material';
import Wizard, { WizardStep } from '@/components/Wizard/Wizard'; // Assuming path
import { useSnackbar } from '@/hooks/useSnackbar';

import {
  SemanticLayerAlignmentWizardData,
  ConnectionSelectionStepProps,
  DatabaseSchemaSelectionStepProps,
  SlodJsonEditorStepProps,
  GAMING_SLOD_JSON_CONTENT
} from './types';
import ConnectionSelectionStep from './ConnectionSelectionStep';
import DatabaseSchemaSelectionStep from './DatabaseSchemaSelectionStep';
import SlodJsonEditorStep from './SlodJsonEditorStep';

interface SemanticLayerAlignmentWizardProps {
  open: boolean;
  onClose: () => void;
  onComplete?: (data: SemanticLayerAlignmentWizardData) => void;
  initialData?: Partial<SemanticLayerAlignmentWizardData>;
}

const SemanticLayerAlignmentWizard: React.FC<SemanticLayerAlignmentWizardProps> = ({
  open,
  onClose,
  onComplete,
  initialData = {},
}) => {
  const { showSnackbar } = useSnackbar();
  const [formData, setFormData] = useState<SemanticLayerAlignmentWizardData>({
    selectedConnection: initialData.selectedConnection,
    connectionTestResult: initialData.connectionTestResult,
    rawSchemaSelection: initialData.rawSchemaSelection,
    generatedDatabaseSchemaJson: initialData.generatedDatabaseSchemaJson,
    slodJsonContent: initialData.slodJsonContent || GAMING_SLOD_JSON_CONTENT, // Ensure default SLOD content
    isValidSlodJson: initialData.isValidSlodJson === undefined ? true : initialData.isValidSlodJson,
  });

  // Step validation states
  const [isStep1Valid, setIsStep1Valid] = useState(false);
  const [isStep2Valid, setIsStep2Valid] = useState(false);
  const [isStep3Valid, setIsStep3Valid] = useState(true); // SLOD JSON is initially valid or becomes valid

  const handleFormDataChange = useCallback((newData: Partial<SemanticLayerAlignmentWizardData>) => {
    setFormData(prev => {
      const updatedData = { ...prev, ...newData };
      // Debug log to check what's happening with the data
      console.log('Form data updated in wizard:', updatedData);
      return updatedData;
    });
  }, []);

  const steps: WizardStep[] = [
    {
      label: 'Select Connection',
      content: (
        <ConnectionSelectionStep
          formData={formData}
          onFormDataChange={handleFormDataChange}
          setStepValidated={setIsStep1Valid}
        />
      ),
      validation: () => isStep1Valid,
    },
    {
      label: 'Select Schema Elements',
      content: (
        <DatabaseSchemaSelectionStep
          formData={formData}
          onFormDataChange={handleFormDataChange}
          setStepValidated={setIsStep2Valid}
        />
      ),
      validation: () => isStep2Valid,
    },
    {
      label: 'Align SLOD JSON',
      content: (
        <SlodJsonEditorStep
          formData={formData}
          onFormDataChange={handleFormDataChange}
          setStepValidated={setIsStep3Valid}
        />
      ),
      validation: () => isStep3Valid && formData.isValidSlodJson === true,
    },
  ];

  const handleWizardComplete = (finalData: SemanticLayerAlignmentWizardData) => {
    console.log('Semantic Layer Alignment Wizard completed with data:', finalData);

    // Here you would typically save the generatedDatabaseSchemaJson and slodJsonContent
    // For example, by calling a service or API endpoint.
    // e.g., await SemanticLayerService.saveAlignment(finalData.generatedDatabaseSchemaJson, finalData.slodJsonContent);

    if (finalData.generatedDatabaseSchemaJson) {
        // Simulate saving the generated DB schema JSON
        const blob = new Blob([JSON.stringify(finalData.generatedDatabaseSchemaJson, null, 2)], { type: 'application/json' });
        const link = document.createElement('a');
        link.href = URL.createObjectURL(blob);
        link.download = `db-schema-${finalData.selectedConnection?.connectionName || 'export'}-${new Date().toISOString().split('T')[0]}.json`;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        URL.revokeObjectURL(link.href);
        showSnackbar('Generated Database Schema JSON has been downloaded.', 'success');
    }

    if (finalData.slodJsonContent && finalData.isValidSlodJson) {
        // Simulate saving the SLOD JSON
        const blob = new Blob([finalData.slodJsonContent], { type: 'application/json' });
        const link = document.createElement('a');
        link.href = URL.createObjectURL(blob);
        link.download = `slod-aligned-${finalData.selectedConnection?.connectionName || 'export'}-${new Date().toISOString().split('T')[0]}.json`;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        URL.revokeObjectURL(link.href);
        showSnackbar('Aligned SLOD JSON has been downloaded.', 'success');
    }


    if (onComplete) {
      onComplete(finalData);
    }
    showSnackbar('Semantic Layer Alignment process completed!', 'success');
    onClose(); // Close the wizard dialog
  };

  const handleWizardCancel = () => {
    showSnackbar('Semantic Layer Alignment Wizard cancelled', 'info');
    onClose();
  };

  return (
    <Dialog
      open={open}
      onClose={onClose} // Prevent closing on backdrop click if needed via Dialog props
      fullWidth
      maxWidth="lg" // Consider 'xl' or custom width for more space
      PaperProps={{
        sx: {
          height: '90vh', // Adjust as needed
          maxHeight: '950px', // Adjust as needed
          display: 'flex',
          flexDirection: 'column',
        },
      }}
    >
      <DialogTitle sx={{ m: 0, p: 2, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        Semantic Layer Alignment Wizard {/* Removed Typography component, text is now a direct child */}
        <IconButton
          aria-label="close"
          onClick={onClose}
          sx={{
            // position: 'absolute',
            // right: 8,
            // top: 8,
            color: (theme) => theme.palette.grey[500],
          }}
        >
          <CloseIcon />
        </IconButton>
      </DialogTitle>
      <DialogContent dividers sx={{ flexGrow: 1, p:0, overflow: 'hidden' }}> {/* Ensure content can grow and scroll if necessary */}
        <Wizard
          steps={steps}
          onComplete={handleWizardComplete}
          onCancel={handleWizardCancel}
          initialData={formData} // Pass current full formData as initialData to Wizard
          title="" // Title is handled by DialogTitle
          showStepNumbers={true}
          key={`wizard-${JSON.stringify(formData.selectedConnection?.connectionId || 'none')}`} // Force re-render when connection changes
        />
      </DialogContent>
    </Dialog>
  );
};

export default SemanticLayerAlignmentWizard;
