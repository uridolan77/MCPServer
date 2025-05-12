import React from 'react';
import { TextField, Grid, Typography } from '@mui/material';

interface ConnectionStringFormProps {
  connectionString: string;
  connectionType: string;
  onChange: (e: React.ChangeEvent<HTMLInputElement | { name?: string; value: unknown }>) => void;
}

const ConnectionStringForm: React.FC<ConnectionStringFormProps> = ({
  connectionString,
  connectionType,
  onChange
}) => {
  return (
    <Grid container spacing={3}>
      <Grid item xs={12}>
        <Typography variant="h6">Connection String</Typography>
      </Grid>
      
      <Grid item xs={12}>
        <TextField
          fullWidth
          required
          multiline
          rows={4}
          label="Connection String"
          name="connectionString"
          value={connectionString}
          onChange={onChange}
          variant="outlined"
          placeholder={
            connectionType === 'sqlServer' 
              ? "Server=localhost;Database=mydb;User ID=username;Password=password;" 
              : "Server=localhost;Port=3306;Database=mydb;User ID=username;Password=password;"
          }
        />
      </Grid>
    </Grid>
  );
};

export default ConnectionStringForm;