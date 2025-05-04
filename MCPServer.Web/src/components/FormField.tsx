import React from 'react';
import {
  TextField,
  TextFieldProps,
  FormControl,
  FormHelperText,
  InputLabel,
  Select,
  MenuItem,
  Checkbox,
  FormControlLabel,
  Switch,
  FormGroup,
  SelectProps,
  FormLabel,
  RadioGroup,
  Radio,
  Autocomplete,
  AutocompleteProps,
  TextField as MuiTextField
} from '@mui/material';
import { Controller, Control, FieldValues, Path, FieldError } from 'react-hook-form';

interface Option {
  value: string | number;
  label: string;
}

interface BaseFormFieldProps<T extends FieldValues> {
  name: Path<T>;
  control: Control<T>;
  label?: string;
  error?: FieldError;
  helperText?: string;
  required?: boolean;
  disabled?: boolean;
}

interface TextFormFieldProps<T extends FieldValues> extends BaseFormFieldProps<T> {
  type?: TextFieldProps['type'];
  multiline?: boolean;
  rows?: number;
  placeholder?: string;
}

interface SelectFormFieldProps<T extends FieldValues> extends BaseFormFieldProps<T> {
  options: Option[];
  multiple?: boolean;
  native?: boolean;
}

interface CheckboxFormFieldProps<T extends FieldValues> extends BaseFormFieldProps<T> {
  checkboxLabel?: string;
}

interface SwitchFormFieldProps<T extends FieldValues> extends BaseFormFieldProps<T> {
  switchLabel?: string;
}

interface RadioFormFieldProps<T extends FieldValues> extends BaseFormFieldProps<T> {
  options: Option[];
  row?: boolean;
}

interface AutocompleteFormFieldProps<T extends FieldValues, U> extends BaseFormFieldProps<T> {
  options: U[];
  getOptionLabel: (option: U) => string;
  isOptionEqualToValue?: (option: U, value: U) => boolean;
  multiple?: boolean;
  placeholder?: string;
  autocompleteProps?: Partial<AutocompleteProps<U, boolean, boolean, boolean>>;
}

export const TextFormField = <T extends FieldValues>({
  name,
  control,
  label,
  error,
  helperText,
  required = false,
  disabled = false,
  type = 'text',
  multiline = false,
  rows = 1,
  placeholder,
}: TextFormFieldProps<T>) => {
  return (
    <Controller
      name={name}
      control={control}
      render={({ field }) => (
        <TextField
          {...field}
          label={label}
          variant="outlined"
          fullWidth
          margin="normal"
          error={!!error}
          helperText={error ? error.message : helperText}
          required={required}
          disabled={disabled}
          type={type}
          multiline={multiline}
          rows={rows}
          placeholder={placeholder}
          value={field.value || ''}
        />
      )}
    />
  );
};

export const SelectFormField = <T extends FieldValues>({
  name,
  control,
  label,
  error,
  helperText,
  required = false,
  disabled = false,
  options,
  multiple = false,
  native = false,
}: SelectFormFieldProps<T>) => {
  return (
    <Controller
      name={name}
      control={control}
      render={({ field }) => (
        <FormControl
          fullWidth
          margin="normal"
          error={!!error}
          required={required}
          disabled={disabled}
        >
          <InputLabel id={`${name}-label`}>{label}</InputLabel>
          <Select
            labelId={`${name}-label`}
            id={`${name}-select`}
            label={label}
            multiple={multiple}
            native={native}
            value={field.value !== undefined && field.value !== null ? field.value : (multiple ? [] : '')}
            onChange={(e) => field.onChange(e.target.value)}
            onBlur={field.onBlur}
            name={field.name}
            ref={field.ref}
            MenuProps={{
              PaperProps: {
                style: {
                  maxHeight: 300
                }
              }
            }}
            sx={{ minWidth: '200px' }}
          >
            {native ? (
              <>
                <option value="">None</option>
                {options.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </>
            ) : (
              <>
                <MenuItem value="">
                  <em>None</em>
                </MenuItem>
                {options.map((option) => (
                  <MenuItem key={option.value} value={option.value}>
                    {option.label}
                  </MenuItem>
                ))}
              </>
            )}
          </Select>
          {(error || helperText) && (
            <FormHelperText>{error ? error.message : helperText}</FormHelperText>
          )}
        </FormControl>
      )}
    />
  );
};

export const CheckboxFormField = <T extends FieldValues>({
  name,
  control,
  label,
  error,
  helperText,
  required = false,
  disabled = false,
  checkboxLabel,
}: CheckboxFormFieldProps<T>) => {
  return (
    <Controller
      name={name}
      control={control}
      render={({ field }) => (
        <FormControl
          fullWidth
          margin="normal"
          error={!!error}
          required={required}
          disabled={disabled}
        >
          {label && <FormLabel component="legend">{label}</FormLabel>}
          <FormGroup>
            <FormControlLabel
              control={
                <Checkbox
                  {...field}
                  checked={field.value || false}
                  onChange={(e) => field.onChange(e.target.checked)}
                />
              }
              label={checkboxLabel || ''}
            />
          </FormGroup>
          {(error || helperText) && (
            <FormHelperText>{error ? error.message : helperText}</FormHelperText>
          )}
        </FormControl>
      )}
    />
  );
};

export const SwitchFormField = <T extends FieldValues>({
  name,
  control,
  label,
  error,
  helperText,
  required = false,
  disabled = false,
  switchLabel,
}: SwitchFormFieldProps<T>) => {
  return (
    <Controller
      name={name}
      control={control}
      render={({ field }) => (
        <FormControl
          fullWidth
          margin="normal"
          error={!!error}
          required={required}
          disabled={disabled}
        >
          {label && <FormLabel component="legend">{label}</FormLabel>}
          <FormGroup>
            <FormControlLabel
              control={
                <Switch
                  {...field}
                  checked={field.value || false}
                  onChange={(e) => field.onChange(e.target.checked)}
                />
              }
              label={switchLabel || ''}
            />
          </FormGroup>
          {(error || helperText) && (
            <FormHelperText>{error ? error.message : helperText}</FormHelperText>
          )}
        </FormControl>
      )}
    />
  );
};

export const RadioFormField = <T extends FieldValues>({
  name,
  control,
  label,
  error,
  helperText,
  required = false,
  disabled = false,
  options,
  row = false,
}: RadioFormFieldProps<T>) => {
  return (
    <Controller
      name={name}
      control={control}
      render={({ field }) => (
        <FormControl
          fullWidth
          margin="normal"
          error={!!error}
          required={required}
          disabled={disabled}
        >
          {label && <FormLabel component="legend">{label}</FormLabel>}
          <RadioGroup {...field} row={row} value={field.value || ''}>
            {options.map((option) => (
              <FormControlLabel
                key={option.value}
                value={option.value}
                control={<Radio />}
                label={option.label}
              />
            ))}
          </RadioGroup>
          {(error || helperText) && (
            <FormHelperText>{error ? error.message : helperText}</FormHelperText>
          )}
        </FormControl>
      )}
    />
  );
};

export const AutocompleteFormField = <T extends FieldValues, U>({
  name,
  control,
  label,
  error,
  helperText,
  required = false,
  disabled = false,
  options,
  getOptionLabel,
  isOptionEqualToValue,
  multiple = false,
  placeholder,
  autocompleteProps,
}: AutocompleteFormFieldProps<T, U>) => {
  return (
    <Controller
      name={name}
      control={control}
      render={({ field }) => (
        <FormControl
          fullWidth
          margin="normal"
          error={!!error}
          required={required}
          disabled={disabled}
        >
          <Autocomplete
            {...field}
            options={options}
            getOptionLabel={getOptionLabel}
            isOptionEqualToValue={isOptionEqualToValue}
            multiple={multiple}
            value={field.value || (multiple ? [] : null)}
            onChange={(_, newValue) => field.onChange(newValue)}
            renderInput={(params) => (
              <MuiTextField
                {...params}
                label={label}
                placeholder={placeholder}
                error={!!error}
                helperText={error ? error.message : helperText}
                required={required}
              />
            )}
            disabled={disabled}
            {...autocompleteProps}
          />
        </FormControl>
      )}
    />
  );
};
