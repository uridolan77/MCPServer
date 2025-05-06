-- Check if DataTransferConnections table exists
IF OBJECT_ID('dbo.DataTransferConnections', 'U') IS NOT NULL
    SELECT 'DataTransferConnections table exists' AS Result
ELSE
    SELECT 'DataTransferConnections table does not exist' AS Result;

-- Check the schema of DataTransferConnections table if it exists
IF OBJECT_ID('dbo.DataTransferConnections', 'U') IS NOT NULL
BEGIN
    SELECT 
        COLUMN_NAME, 
        DATA_TYPE, 
        IS_NULLABLE
    FROM 
        INFORMATION_SCHEMA.COLUMNS
    WHERE 
        TABLE_NAME = 'DataTransferConnections'
    ORDER BY 
        ORDINAL_POSITION;
END
