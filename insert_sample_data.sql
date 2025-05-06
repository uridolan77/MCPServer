-- Insert sample connections if none exist
IF NOT EXISTS (SELECT 1 FROM [dbo].[DataTransferConnections])
BEGIN
    INSERT INTO [dbo].[DataTransferConnections]
        ([ConnectionName], [ConnectionString], [Description], [IsSource], [IsDestination], [IsActive], [CreatedBy], [CreatedOn])
    VALUES
        ('ProgressPlay Source DB', 'Server=tcp:progressplay-server.database.windows.net,1433;Database=ProgressPlayDB;User ID=pp-sa;Password=***;', 'Azure SQL Database for ProgressPlay data', 1, 0, 1, 'System', GETUTCDATE()),
        ('MCP Analytics DB', 'Server=localhost;Database=MCPAnalytics;Integrated Security=true;', 'Local SQL Server for analytics data', 0, 1, 1, 'System', GETUTCDATE());
    
    PRINT 'Sample connections inserted.'
END
ELSE
BEGIN
    PRINT 'Connections already exist in the table.'
END
