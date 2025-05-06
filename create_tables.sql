-- Create DataTransferConnections table if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DataTransferConnections]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[DataTransferConnections](
        [ConnectionId] [int] IDENTITY(1,1) NOT NULL,
        [ConnectionName] [nvarchar](100) NOT NULL,
        [ConnectionString] [nvarchar](500) NOT NULL,
        [Description] [nvarchar](500) NULL,
        [IsSource] [bit] NOT NULL DEFAULT(1),
        [IsDestination] [bit] NOT NULL DEFAULT(1),
        [IsActive] [bit] NOT NULL DEFAULT(1),
        [CreatedBy] [nvarchar](100) NOT NULL,
        [CreatedOn] [datetime2](7) NOT NULL,
        [LastModifiedBy] [nvarchar](100) NULL,
        [LastModifiedOn] [datetime2](7) NULL,
        CONSTRAINT [PK_DataTransferConnections] PRIMARY KEY CLUSTERED 
        (
            [ConnectionId] ASC
        ),
        CONSTRAINT [UQ_DataTransferConnections_Name] UNIQUE NONCLUSTERED 
        (
            [ConnectionName] ASC
        )
    )
    
    PRINT 'DataTransferConnections table created.'
END
ELSE
BEGIN
    PRINT 'DataTransferConnections table already exists.'
END

-- Create DataTransferConfigurations table if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DataTransferConfigurations]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[DataTransferConfigurations](
        [ConfigurationId] [int] IDENTITY(1,1) NOT NULL,
        [ConfigurationName] [nvarchar](100) NOT NULL,
        [Description] [nvarchar](500) NULL,
        [SourceConnectionId] [int] NOT NULL,
        [DestinationConnectionId] [int] NOT NULL,
        [BatchSize] [int] NOT NULL DEFAULT(5000),
        [ReportingFrequency] [int] NOT NULL DEFAULT(10),
        [IsActive] [bit] NOT NULL DEFAULT(1),
        [CreatedBy] [nvarchar](100) NOT NULL,
        [CreatedOn] [datetime2](7) NOT NULL,
        [LastModifiedBy] [nvarchar](100) NULL,
        [LastModifiedOn] [datetime2](7) NULL,
        CONSTRAINT [PK_DataTransferConfigurations] PRIMARY KEY CLUSTERED 
        (
            [ConfigurationId] ASC
        ),
        CONSTRAINT [UQ_DataTransferConfigurations_Name] UNIQUE NONCLUSTERED 
        (
            [ConfigurationName] ASC
        ),
        CONSTRAINT [FK_DataTransferConfigurations_SourceConnection] FOREIGN KEY([SourceConnectionId])
            REFERENCES [dbo].[DataTransferConnections] ([ConnectionId]),
        CONSTRAINT [FK_DataTransferConfigurations_DestinationConnection] FOREIGN KEY([DestinationConnectionId])
            REFERENCES [dbo].[DataTransferConnections] ([ConnectionId])
    )
    
    PRINT 'DataTransferConfigurations table created.'
END
ELSE
BEGIN
    PRINT 'DataTransferConfigurations table already exists.'
END

-- Create DataTransferTableMappings table if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DataTransferTableMappings]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[DataTransferTableMappings](
        [MappingId] [int] IDENTITY(1,1) NOT NULL,
        [ConfigurationId] [int] NOT NULL,
        [SchemaName] [nvarchar](100) NOT NULL,
        [TableName] [nvarchar](100) NOT NULL,
        [TimestampColumnName] [nvarchar](100) NOT NULL,
        [OrderByColumn] [nvarchar](100) NULL,
        [CustomWhereClause] [nvarchar](500) NULL,
        [IsActive] [bit] NOT NULL DEFAULT(1),
        [Priority] [int] NOT NULL DEFAULT(100),
        [CreatedBy] [nvarchar](100) NOT NULL,
        [CreatedOn] [datetime2](7) NOT NULL,
        [LastModifiedBy] [nvarchar](100) NULL,
        [LastModifiedOn] [datetime2](7) NULL,
        CONSTRAINT [PK_DataTransferTableMappings] PRIMARY KEY CLUSTERED 
        (
            [MappingId] ASC
        ),
        CONSTRAINT [FK_DataTransferTableMappings_Configuration] FOREIGN KEY([ConfigurationId])
            REFERENCES [dbo].[DataTransferConfigurations] ([ConfigurationId])
    )
    
    PRINT 'DataTransferTableMappings table created.'
END
ELSE
BEGIN
    PRINT 'DataTransferTableMappings table already exists.'
END
