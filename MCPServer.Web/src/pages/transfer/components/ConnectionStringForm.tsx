import React from 'react';
import {
  TextField
} from '@mui/material';

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
  const getHelperText = () => {
    if (connectionType === 'sqlServer') {
      return "Example: Server=myserver;Database=mydatabase;User ID=myuser;Password=mypassword;";
    } else if (connectionType === 'mysql') {
      return "Example: Server=myserver;Database=mydatabase;User ID=myuser;Password=mypassword;Port=3306;";
    }
    return "Enter connection string for your database";
  };

  return (
    <TextField
      label="Connection String"
      name="connectionString"
      value={connectionString}
      onChange={onChange}
      fullWidth
      required
      multiline
      rows={3}
      helperText={getHelperText()}
    />
  );
};

export default ConnectionStringForm;