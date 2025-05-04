import React, { useEffect, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import {
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  SelectChangeEvent,
  FormHelperText,
  CircularProgress,
  Box
} from '@mui/material';
import { llmProviderApi, LlmProvider } from '@/api';

interface ProviderSelectorProps {
  value: string | number; // Current selected provider ID
  onChange: (providerId: string | number) => void; // Handler for selection change
  label?: string; // Optional custom label
  helperText?: string; // Optional helper text
  error?: boolean; // Optional error state
  required?: boolean; // Optional required state
  fullWidth?: boolean; // Optional fullWidth prop
  size?: 'small' | 'medium'; // Optional size prop
  disabled?: boolean; // Optional disabled state
  sx?: any; // Optional sx prop for styling
  showAllOption?: boolean; // Optional flag to show "All Providers" option
  allOptionLabel?: string; // Optional custom label for "All Providers" option
}

const ProviderSelector: React.FC<ProviderSelectorProps> = ({
  value,
  onChange,
  label = 'Provider',
  helperText,
  error = false,
  required = false,
  fullWidth = false,
  size = 'medium',
  disabled = false,
  sx = {},
  showAllOption = true,
  allOptionLabel = 'All Providers'
}) => {
  const [loadError, setLoadError] = useState<string | null>(null);

  // Fetch providers
  const { data: providersData, isLoading } = useQuery({
    queryKey: ['providers'],
    queryFn: async () => {
      try {
        // Make the API call using fetch to get more control over the response
        const response = await fetch('/api/llm/providers');
        if (!response.ok) {
          throw new Error(`API returned ${response.status}: ${response.statusText}`);
        }
        
        const result = await response.json();
        console.log('Provider API response:', result);
        
        // Check for the specific format seen in the error log: {$id: '1', success: true, data: {...}, message: '...'}
        if (result && result.success === true && result.data) {
          // If data is directly an array
          if (Array.isArray(result.data)) {
            return result.data;
          }
          
          // If data contains $values array (common in .NET serialization)
          if (result.data.$values && Array.isArray(result.data.$values)) {
            return result.data.$values;
          }
          
          // If data is an object with array properties
          if (typeof result.data === 'object' && result.data !== null) {
            for (const key in result.data) {
              if (Array.isArray(result.data[key])) {
                console.log(`Found providers array in data.${key}`, result.data[key]);
                return result.data[key];
              }
            }
          }
        }
        
        // Continue with other checks from before
        if (Array.isArray(result)) {
          return result;
        }
        
        if (result && result.$values && Array.isArray(result.$values)) {
          return result.$values;
        }
        
        // Try to find any array in the response
        if (typeof result === 'object' && result !== null) {
          for (const key in result) {
            if (Array.isArray(result[key])) {
              console.log(`Found providers array in property "${key}"`, result[key]);
              return result[key];
            }
          }
        }
        
        // Log the full response to help debugging
        console.error('Could not extract providers from response:', JSON.stringify(result, null, 2));
        throw new Error('Invalid response format from providers API');
      } catch (error) {
        console.error('Error fetching providers:', error);
        setLoadError(error instanceof Error ? error.message : 'Failed to load providers');
        return [];
      }
    },
    staleTime: 5 * 60 * 1000, // Cache for 5 minutes
    retry: 2
  });

  // Generate a unique ID for the select input
  const labelId = `provider-selector-label-${React.useId()}`;

  // Handle provider selection change
  const handleChange = (event: SelectChangeEvent<typeof value>) => {
    onChange(event.target.value);
  };

  // Ensure providers is always an array
  const providers = Array.isArray(providersData) ? providersData : [];

  return (
    <FormControl 
      fullWidth={fullWidth}
      size={size}
      error={error || !!loadError}
      required={required}
      disabled={disabled || isLoading}
      sx={sx}
    >
      <InputLabel id={labelId}>{label}</InputLabel>
      <Select
        labelId={labelId}
        label={label}
        value={value}
        onChange={handleChange}
        MenuProps={{ 
          PaperProps: { sx: { maxHeight: 300 } }
        }}
        startAdornment={
          isLoading ? (
            <Box sx={{ display: 'flex', alignItems: 'center', pl: 1 }}>
              <CircularProgress size={16} color="inherit" />
            </Box>
          ) : null
        }
      >
        {showAllOption && (
          <MenuItem value="">
            <em>{allOptionLabel}</em>
          </MenuItem>
        )}
        
        {isLoading && (
          <MenuItem disabled value="">
            Loading providers...
          </MenuItem>
        )}
        
        {!isLoading && providers.length === 0 && (
          <MenuItem disabled value="">
            {loadError || 'No providers available'}
          </MenuItem>
        )}
        
        {providers.map((provider: LlmProvider) => (
          <MenuItem key={provider.id} value={provider.id}>
            {provider.displayName || provider.name}
          </MenuItem>
        ))}
      </Select>
      {(helperText || loadError) && (
        <FormHelperText>{loadError || helperText}</FormHelperText>
      )}
    </FormControl>
  );
};

export default ProviderSelector;