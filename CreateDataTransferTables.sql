-- Create tables for data transfer management in ProgressPlayDB

-- Tables for Data Transfer Management

-- Table to store connection strings
CREATE TABLE dbo.DataTransferConnections (
    ConnectionId INT IDENTITY(1,1) PRIMARY KEY,
    ConnectionName NVARCHAR(128) NOT NULL,
    ConnectionString NVARCHAR(1024) NOT NULL,
    Description NVARCHAR(255) NULL,
    IsSource BIT NOT NULL DEFAULT 1,
    IsDestination BIT NOT NULL DEFAULT 1,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedOn DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CreatedBy NVARCHAR(128) NULL,
    LastModifiedOn DATETIME2 NULL,
    LastModifiedBy NVARCHAR(128) NULL,
    CONSTRAINT UQ_ConnectionName UNIQUE (ConnectionName)
);

-- Table to store transfer configurations
CREATE TABLE dbo.DataTransferConfigurations (
    ConfigurationId INT IDENTITY(1,1) PRIMARY KEY,
    ConfigurationName NVARCHAR(128) NOT NULL,
    Description NVARCHAR(255) NULL,
    SourceConnectionId INT NOT NULL,
    DestinationConnectionId INT NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    BatchSize INT NOT NULL DEFAULT 5000,
    ReportingFrequency INT NOT NULL DEFAULT 10,
    CreatedOn DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CreatedBy NVARCHAR(128) NULL,
    LastModifiedOn DATETIME2 NULL,
    LastModifiedBy NVARCHAR(128) NULL,
    CONSTRAINT UQ_ConfigurationName UNIQUE (ConfigurationName),
    CONSTRAINT FK_Configuration_SourceConnection FOREIGN KEY (SourceConnectionId) REFERENCES dbo.DataTransferConnections(ConnectionId),
    CONSTRAINT FK_Configuration_DestinationConnection FOREIGN KEY (DestinationConnectionId) REFERENCES dbo.DataTransferConnections(ConnectionId)
);

-- Table to store table mappings for transfer configurations
CREATE TABLE dbo.DataTransferTableMappings (
    MappingId INT IDENTITY(1,1) PRIMARY KEY,
    ConfigurationId INT NOT NULL,
    SchemaName NVARCHAR(128) NOT NULL,
    TableName NVARCHAR(128) NOT NULL,
    TimestampColumnName NVARCHAR(128) NOT NULL,
    OrderByColumn NVARCHAR(128) NULL,
    CustomWhereClause NVARCHAR(1024) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    Priority INT NOT NULL DEFAULT 100,
    CreatedOn DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CreatedBy NVARCHAR(128) NULL,
    LastModifiedOn DATETIME2 NULL,
    LastModifiedBy NVARCHAR(128) NULL,
    CONSTRAINT FK_TableMapping_Configuration FOREIGN KEY (ConfigurationId) REFERENCES dbo.DataTransferConfigurations(ConfigurationId),
    CONSTRAINT UQ_TableMapping UNIQUE (ConfigurationId, SchemaName, TableName)
);

-- Table to store schedule information
CREATE TABLE dbo.DataTransferSchedule (
    ScheduleId INT IDENTITY(1,1) PRIMARY KEY,
    ConfigurationId INT NOT NULL,
    ScheduleType NVARCHAR(50) NOT NULL, -- 'Once', 'Daily', 'Weekly', 'Monthly', 'Custom'
    StartTime TIME NULL,
    Frequency INT NULL, -- e.g., every X days, hours, minutes
    FrequencyUnit NVARCHAR(20) NULL, -- 'Minute', 'Hour', 'Day', 'Week', 'Month'
    WeekDays NVARCHAR(20) NULL, -- e.g., 'Mon,Tue,Wed,Thu,Fri'
    MonthDays NVARCHAR(100) NULL, -- e.g., '1,15' for 1st and 15th of month
    IsActive BIT NOT NULL DEFAULT 1,
    LastRunTime DATETIME2 NULL,
    NextRunTime DATETIME2 NULL,
    CreatedOn DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CreatedBy NVARCHAR(128) NULL,
    LastModifiedOn DATETIME2 NULL,
    LastModifiedBy NVARCHAR(128) NULL,
    CONSTRAINT FK_Schedule_Configuration FOREIGN KEY (ConfigurationId) REFERENCES dbo.DataTransferConfigurations(ConfigurationId)
);

-- Table to store runs history and metrics
CREATE TABLE dbo.DataTransferRuns (
    RunId INT IDENTITY(1,1) PRIMARY KEY,
    ConfigurationId INT NOT NULL,
    ScheduleId INT NULL, -- NULL if manually triggered
    StartTime DATETIME2 NOT NULL,
    EndTime DATETIME2 NULL,
    Status NVARCHAR(50) NOT NULL, -- 'Running', 'Completed', 'Failed', 'Cancelled'
    TotalTablesProcessed INT NOT NULL DEFAULT 0,
    SuccessfulTablesCount INT NOT NULL DEFAULT 0,
    FailedTablesCount INT NOT NULL DEFAULT 0,
    TotalRowsProcessed INT NOT NULL DEFAULT 0,
    ElapsedMs BIGINT NULL,
    AverageRowsPerSecond FLOAT NULL,
    TriggeredBy NVARCHAR(128) NULL,
    CONSTRAINT FK_Runs_Configuration FOREIGN KEY (ConfigurationId) REFERENCES dbo.DataTransferConfigurations(ConfigurationId),
    CONSTRAINT FK_Runs_Schedule FOREIGN KEY (ScheduleId) REFERENCES dbo.DataTransferSchedule(ScheduleId)
);

-- Table to store detailed metrics per table in a run
CREATE TABLE dbo.DataTransferTableMetrics (
    MetricId INT IDENTITY(1,1) PRIMARY KEY,
    RunId INT NOT NULL,
    MappingId INT NOT NULL,
    SchemaName NVARCHAR(128) NOT NULL,
    TableName NVARCHAR(128) NOT NULL,
    StartTime DATETIME2 NOT NULL,
    EndTime DATETIME2 NULL,
    Status NVARCHAR(50) NOT NULL, -- 'Running', 'Completed', 'Failed', 'Cancelled'
    TotalRowsToProcess INT NOT NULL DEFAULT 0,
    RowsProcessed INT NOT NULL DEFAULT 0,
    ElapsedMs BIGINT NULL,
    RowsPerSecond FLOAT NULL,
    ErrorMessage NVARCHAR(MAX) NULL,
    LastProcessedTimestamp DATETIME2 NULL,
    CONSTRAINT FK_TableMetrics_Run FOREIGN KEY (RunId) REFERENCES dbo.DataTransferRuns(RunId),
    CONSTRAINT FK_TableMetrics_Mapping FOREIGN KEY (MappingId) REFERENCES dbo.DataTransferTableMappings(MappingId)
);

-- Table to store logs from transfer operations
CREATE TABLE dbo.DataTransferLogs (
    LogId INT IDENTITY(1,1) PRIMARY KEY,
    RunId INT NULL,
    MappingId INT NULL,
    LogTime DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    LogLevel NVARCHAR(20) NOT NULL, -- 'Information', 'Warning', 'Error', 'Debug'
    Message NVARCHAR(MAX) NOT NULL,
    Exception NVARCHAR(MAX) NULL,
    CONSTRAINT FK_Logs_Run FOREIGN KEY (RunId) REFERENCES dbo.DataTransferRuns(RunId),
    CONSTRAINT FK_Logs_Mapping FOREIGN KEY (MappingId) REFERENCES dbo.DataTransferTableMappings(MappingId)
);

-- Improve the existing watermarks table by adding more metadata
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'IncrementalLoadWatermarks' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    -- Add new columns if they don't exist
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.IncrementalLoadWatermarks') AND name = 'ConfigurationId')
    BEGIN
        ALTER TABLE dbo.IncrementalLoadWatermarks ADD ConfigurationId INT NULL;
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.IncrementalLoadWatermarks') AND name = 'MappingId')
    BEGIN
        ALTER TABLE dbo.IncrementalLoadWatermarks ADD MappingId INT NULL;
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.IncrementalLoadWatermarks') AND name = 'LastRunId')
    BEGIN
        ALTER TABLE dbo.IncrementalLoadWatermarks ADD LastRunId INT NULL;
    END
END
