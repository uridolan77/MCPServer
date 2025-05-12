import React, { useState, ReactNode, useEffect } from 'react';
import {
  Box,
  Stepper,
  Step,
  StepLabel,
  Button,
  Typography,
  Paper,
  Divider,
  StepIconProps,
  styled
} from '@mui/material';
import {
  Check as CheckIcon,
  NavigateNext as NextIcon,
  NavigateBefore as BackIcon
} from '@mui/icons-material';

export interface WizardStep {
  label: string;
  content: ReactNode;
  optional?: boolean;
  validation?: (formData?: any) => boolean | Promise<boolean>;
}

interface WizardProps {
  steps: WizardStep[];
  onComplete?: (finalData: any) => void;
  onCancel?: () => void;
  initialData?: any;
  title?: string;
  showStepNumbers?: boolean;
}

// Custom styling for completed steps
const QontoStepIconRoot = styled('div')<{ ownerState: { active?: boolean; completed?: boolean } }>(
  ({ theme, ownerState }) => ({
    color: theme.palette.mode === 'dark' ? theme.palette.grey[700] : '#eaeaf0',
    display: 'flex',
    height: 22,
    alignItems: 'center',
    ...(ownerState.active && {
      color: theme.palette.primary.main,
    }),
    '& .QontoStepIcon-completedIcon': {
      color: theme.palette.primary.main,
      zIndex: 1,
      fontSize: 18,
    },
    '& .QontoStepIcon-circle': {
      width: 8,
      height: 8,
      borderRadius: '50%',
      backgroundColor: 'currentColor',
    },
  }),
);

function QontoStepIcon(props: StepIconProps) {
  const { active, completed, className } = props;

  return (
    <QontoStepIconRoot ownerState={{ active, completed }} className={className}>
      {completed ? (
        <CheckIcon className="QontoStepIcon-completedIcon" />
      ) : (
        <div className="QontoStepIcon-circle" />
      )}
    </QontoStepIconRoot>
  );
}

const Wizard: React.FC<WizardProps> = ({
  steps,
  onComplete,
  onCancel,
  initialData = {},
  title = 'Wizard',
  showStepNumbers = false
}) => {
  const [activeStep, setActiveStep] = useState(0);
  const [formData, setFormData] = useState(initialData);
  const [isNextDisabled, setIsNextDisabled] = useState(false);

  // Update local formData when initialData changes from parent
  useEffect(() => {
    console.log('Wizard received updated initialData:', initialData);
    setFormData(initialData);
  }, [initialData]);

  // Check if current step has validation and update next button state
  useEffect(() => {
    const currentStep = steps[activeStep];
    if (currentStep?.validation) {
      const isValid = currentStep.validation(formData);
      if (isValid instanceof Promise) {
        isValid.then(valid => setIsNextDisabled(!valid));
      } else {
        setIsNextDisabled(!isValid);
      }
    } else {
      setIsNextDisabled(false);
    }
  }, [activeStep, formData, steps]);

  // Function to update form data from child components
  const updateFormData = (newData: any) => {
    setFormData((prevData: any) => ({
      ...prevData,
      ...newData,
    }));
  };

  // Handle moving to the next step
  const handleNext = async () => {
    const currentStep = steps[activeStep];

    // If the current step has validation, run it
    if (currentStep?.validation) {
      const isValid = currentStep.validation(formData);
      let validationResult;

      if (isValid instanceof Promise) {
        validationResult = await isValid;
      } else {
        validationResult = isValid;
      }

      if (!validationResult) {
        return; // Don't proceed if validation fails
      }
    }

    if (activeStep === steps.length - 1) {
      // Final step
      onComplete?.(formData);
    } else {
      // Move to next step
      setActiveStep((prevStep) => prevStep + 1);
    }
  };

  // Handle moving to the previous step
  const handleBack = () => {
    setActiveStep((prevStep) => prevStep - 1);
  };

  // Handle cancellation
  const handleCancel = () => {
    onCancel?.();
  };

  // Clone the current step content with additional props
  const currentStepContent = React.isValidElement(steps[activeStep]?.content)
    ? React.cloneElement(steps[activeStep].content as React.ReactElement, {
        formData,
        updateFormData, // Pass the form update function
        isLastStep: activeStep === steps.length - 1,
      })
    : steps[activeStep]?.content;

  // Log the current form data when the active step changes
  useEffect(() => {
    console.log(`Wizard rendering step ${activeStep + 1} with form data:`, formData);
    
    // Make sure connection data is logged when transitioning to step 2
    if (activeStep === 1) {
      console.log('Passing connection data to step 2:', formData.selectedConnection);
    }
  }, [activeStep, formData]);

  return (
    <Paper elevation={3} sx={{ p: 3 }}>
      <Typography variant="h5" component="h2" gutterBottom>
        {title}
      </Typography>

      <Stepper activeStep={activeStep} alternativeLabel sx={{ mb: 4, mt: 2 }}>
        {steps.map((step, index) => {
          const stepProps: { completed?: boolean } = {};

          return (
            <Step key={step.label} {...stepProps}>
              <StepLabel
                StepIconComponent={QontoStepIcon}
                optional={step.optional ? <Typography variant="caption">Optional</Typography> : undefined}
              >
                {showStepNumbers ? `${index + 1}. ${step.label}` : step.label}
              </StepLabel>
            </Step>
          );
        })}
      </Stepper>

      <Divider sx={{ my: 2 }} />

      <Box sx={{ minHeight: '300px' }}>
        {currentStepContent}
      </Box>

      <Divider sx={{ my: 2 }} />

      <Box sx={{ display: 'flex', justifyContent: 'space-between', pt: 2 }}>
        <Box>
          <Button
            onClick={handleCancel}
            variant="outlined"
            color="inherit"
          >
            Cancel
          </Button>
        </Box>
        <Box>
          <Button
            disabled={activeStep === 0}
            onClick={handleBack}
            startIcon={<BackIcon />}
            sx={{ mr: 1 }}
          >
            Back
          </Button>

          <Button
            variant="contained"
            onClick={handleNext}
            endIcon={activeStep === steps.length - 1 ? <CheckIcon /> : <NextIcon />}
            disabled={isNextDisabled}
          >
            {activeStep === steps.length - 1 ? 'Finish' : 'Next'}
          </Button>
        </Box>
      </Box>
    </Paper>
  );
};

export default Wizard;