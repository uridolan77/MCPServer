-- Script to update DataTransferConnections table
-- 1. Remove IsSource and IsDestination fields
-- 2. Add ConnectionAccessLevel field (enum as string)
-- 3. Add LastTestedOn field
-- 4. Add MaxPoolSize, MinPoolSize, Timeout, Encrypt, TrustServerCertificate fields

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
    
    -- Step 1: Add new columns if they don't exist
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
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[DataTransferConnections]') AND name = 'MaxPoolSize')
    BEGIN
        ALTER TABLE [dbo].[DataTransferConnections] 
        ADD [MaxPoolSize] [int] NOT NULL DEFAULT(100);
        PRINT 'Added MaxPoolSize column';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[DataTransferConnections]') AND name = 'MinPoolSize')
    BEGIN
        ALTER TABLE [dbo].[DataTransferConnections] 
        ADD [MinPoolSize] [int] NOT NULL DEFAULT(5);
        PRINT 'Added MinPoolSize column';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[DataTransferConnections]') AND name = 'Timeout')
    BEGIN
        ALTER TABLE [dbo].[DataTransferConnections] 
        ADD [Timeout] [int] NOT NULL DEFAULT(30);
        PRINT 'Added Timeout column';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[DataTransferConnections]') AND name = 'Encrypt')
    BEGIN
        ALTER TABLE [dbo].[DataTransferConnections] 
        ADD [Encrypt] [bit] NOT NULL DEFAULT(1);
        PRINT 'Added Encrypt column';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[DataTransferConnections]') AND name = 'TrustServerCertificate')
    BEGIN
        ALTER TABLE [dbo].[DataTransferConnections] 
        ADD [TrustServerCertificate] [bit] NOT NULL DEFAULT(1);
        PRINT 'Added TrustServerCertificate column';
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
    
    -- Step 3: Set all connections to inactive initially
    UPDATE [dbo].[DataTransferConnections]
    SET [IsActive] = 0;
    PRINT 'Set all connections to inactive initially';
    
    -- Step 4: Drop IsSource and IsDestination columns (optional - can be kept for backward compatibility)
    -- Uncomment these lines if you want to remove the columns
    /*
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[DataTransferConnections]') AND name = 'IsSource')
    BEGIN
        ALTER TABLE [dbo].[DataTransferConnections] 
        DROP COLUMN [IsSource];
        PRINT 'Dropped IsSource column';
    END
    
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[DataTransferConnections]') AND name = 'IsDestination')
    BEGIN
        ALTER TABLE [dbo].[DataTransferConnections] 
        DROP COLUMN [IsDestination];
        PRINT 'Dropped IsDestination column';
    END
    */
    
    PRINT 'DataTransferConnections table update completed successfully';
END
ELSE
BEGIN
    PRINT 'DataTransferConnections table does not exist';
END
