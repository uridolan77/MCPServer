-- Script to update DataTransferConnections table
-- 1. Remove IsSource and IsDestination fields
-- 2. Add ConnectionAccessLevel field (enum as string)
-- 3. Add LastTestedOn field
-- 4. Modify ConnectionString to store encrypted data

-- First, check if the table exists
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DataTransferConnections]') AND type in (N'U'))
BEGIN
    PRINT 'Modifying DataTransferConnections table...';
    
    -- Create a backup of the table first
    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DataTransferConnections_Backup]') AND type in (N'U'))
    BEGIN
        SELECT * INTO [dbo].[DataTransferConnections_Backup] FROM [dbo].[DataTransferConnections];
        PRINT 'Backup created as DataTransferConnections_Backup';
    END
    
    -- Step 1: Add new columns
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[DataTransferConnections]') AND name = 'ConnectionAccessLevel')
    BEGIN
        ALTER TABLE [dbo].[DataTransferConnections] 
        ADD [ConnectionAccessLevel] [nvarchar](20) NOT NULL DEFAULT('ReadWrite');
        PRINT 'Added ConnectionAccessLevel column';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[DataTransferConnections]') AND name = 'LastTestedOn')
    BEGIN
        ALTER TABLE [dbo].[DataTransferConnections] 
        ADD [LastTestedOn] [datetime2](7) NULL;
        PRINT 'Added LastTestedOn column';
    END
    
    -- Step 2: Migrate data - set ConnectionAccessLevel based on IsSource and IsDestination
    UPDATE [dbo].[DataTransferConnections]
    SET [ConnectionAccessLevel] = 
        CASE 
            WHEN [IsSource] = 1 AND [IsDestination] = 1 THEN 'ReadWrite'
            WHEN [IsSource] = 1 AND [IsDestination] = 0 THEN 'ReadOnly'
            WHEN [IsSource] = 0 AND [IsDestination] = 1 THEN 'WriteOnly'
            ELSE 'ReadWrite' -- Default
        END;
    PRINT 'Updated ConnectionAccessLevel values based on IsSource and IsDestination';
    
    -- Step 3: Encrypt ConnectionString (this is a placeholder - actual encryption would depend on your encryption method)
    -- For now, we'll just add a new column to store the encrypted string
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[DataTransferConnections]') AND name = 'EncryptedConnectionString')
    BEGIN
        ALTER TABLE [dbo].[DataTransferConnections] 
        ADD [EncryptedConnectionString] [nvarchar](1000) NULL;
        PRINT 'Added EncryptedConnectionString column';
        
        -- In a real implementation, you would encrypt the connection string here
        -- For now, just copy the value (in production, you'd use proper encryption)
        UPDATE [dbo].[DataTransferConnections]
        SET [EncryptedConnectionString] = [ConnectionString];
        PRINT 'Copied connection strings to EncryptedConnectionString column (placeholder for encryption)';
    END
    
    -- Step 4: Create a new table with the desired schema
    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DataTransferConnections_New]') AND type in (N'U'))
    BEGIN
        CREATE TABLE [dbo].[DataTransferConnections_New](
            [ConnectionId] [int] IDENTITY(1,1) NOT NULL,
            [ConnectionName] [nvarchar](100) NOT NULL,
            [ConnectionString] [nvarchar](1000) NOT NULL, -- Increased size for encrypted string
            [Description] [nvarchar](500) NULL,
            [ConnectionAccessLevel] [nvarchar](20) NOT NULL,
            [IsActive] [bit] NOT NULL DEFAULT(1),
            [LastTestedOn] [datetime2](7) NULL,
            [CreatedBy] [nvarchar](100) NOT NULL,
            [CreatedOn] [datetime2](7) NOT NULL,
            [LastModifiedBy] [nvarchar](100) NULL,
            [LastModifiedOn] [datetime2](7) NULL,
            CONSTRAINT [PK_DataTransferConnections_New] PRIMARY KEY CLUSTERED ([ConnectionId] ASC),
            CONSTRAINT [UQ_DataTransferConnections_New_Name] UNIQUE NONCLUSTERED ([ConnectionName] ASC)
        );
        PRINT 'Created new table DataTransferConnections_New with updated schema';
        
        -- Copy data from old table to new table
        SET IDENTITY_INSERT [dbo].[DataTransferConnections_New] ON;
        
        INSERT INTO [dbo].[DataTransferConnections_New]
            ([ConnectionId], [ConnectionName], [ConnectionString], [Description], 
             [ConnectionAccessLevel], [IsActive], [LastTestedOn],
             [CreatedBy], [CreatedOn], [LastModifiedBy], [LastModifiedOn])
        SELECT 
            [ConnectionId], [ConnectionName], 
            CASE WHEN [EncryptedConnectionString] IS NOT NULL THEN [EncryptedConnectionString] ELSE [ConnectionString] END,
            [Description], [ConnectionAccessLevel], [IsActive], NULL,
            [CreatedBy], [CreatedOn], [LastModifiedBy], [LastModifiedOn]
        FROM [dbo].[DataTransferConnections];
        
        SET IDENTITY_INSERT [dbo].[DataTransferConnections_New] OFF;
        PRINT 'Data copied from old table to new table';
    END
    
    -- Step 5: Update foreign key constraints to point to the new table
    -- First, get all foreign key constraints that reference the old table
    DECLARE @ConstraintName nvarchar(256)
    DECLARE @TableName nvarchar(256)
    DECLARE @SQL nvarchar(max)
    
    DECLARE constraint_cursor CURSOR FOR
    SELECT 
        fk.name AS ConstraintName,
        OBJECT_NAME(fk.parent_object_id) AS TableName
    FROM 
        sys.foreign_keys AS fk
    WHERE 
        OBJECT_NAME(fk.referenced_object_id) = 'DataTransferConnections';
    
    OPEN constraint_cursor
    FETCH NEXT FROM constraint_cursor INTO @ConstraintName, @TableName
    
    WHILE @@FETCH_STATUS = 0
    BEGIN
        -- Drop the constraint
        SET @SQL = 'ALTER TABLE [dbo].[' + @TableName + '] DROP CONSTRAINT [' + @ConstraintName + ']';
        EXEC sp_executesql @SQL;
        PRINT 'Dropped foreign key constraint: ' + @ConstraintName + ' from table: ' + @TableName;
        
        FETCH NEXT FROM constraint_cursor INTO @ConstraintName, @TableName
    END
    
    CLOSE constraint_cursor
    DEALLOCATE constraint_cursor
    
    -- Step 6: Swap the tables
    EXEC sp_rename 'DataTransferConnections', 'DataTransferConnections_Old';
    EXEC sp_rename 'DataTransferConnections_New', 'DataTransferConnections';
    PRINT 'Renamed tables to swap old and new';
    
    -- Step 7: Recreate foreign key constraints to point to the new table
    -- Add foreign key for DataTransferConfigurations.SourceConnectionId
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DataTransferConfigurations]') AND type in (N'U'))
    BEGIN
        ALTER TABLE [dbo].[DataTransferConfigurations]
        ADD CONSTRAINT [FK_DataTransferConfigurations_SourceConnection] 
        FOREIGN KEY([SourceConnectionId]) REFERENCES [dbo].[DataTransferConnections] ([ConnectionId]);
        
        ALTER TABLE [dbo].[DataTransferConfigurations]
        ADD CONSTRAINT [FK_DataTransferConfigurations_DestinationConnection] 
        FOREIGN KEY([DestinationConnectionId]) REFERENCES [dbo].[DataTransferConnections] ([ConnectionId]);
        
        PRINT 'Recreated foreign key constraints for DataTransferConfigurations';
    END
    
    -- Step 8: Drop the old table if everything is successful
    -- In a production environment, you might want to keep this as a backup for a while
    -- DROP TABLE [dbo].[DataTransferConnections_Old];
    -- PRINT 'Dropped old table';
    
    PRINT 'DataTransferConnections table update completed successfully';
END
ELSE
BEGIN
    PRINT 'DataTransferConnections table does not exist';
END
