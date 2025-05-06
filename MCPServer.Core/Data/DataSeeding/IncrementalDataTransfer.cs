using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

public class IncrementalDataTransfer
{
    // Parameters - could be loaded from configuration, passed as arguments, etc.
    private readonly string _sourceConnectionString;
    private readonly string _destinationConnectionString;
    private readonly int _batchSize;
    private readonly int _reportingFrequency; // Report after this many batches
    private readonly ILogger _logger;
    private readonly bool _testMode; // If true, doesn't actually insert data
    
    // Performance tracking
    private int _totalRowsProcessed = 0;
    private int _totalBatchesProcessed = 0;
    private readonly Stopwatch _overallStopwatch = new Stopwatch();
    private Stopwatch _batchStopwatch = new Stopwatch();
    
    public IncrementalDataTransfer(
        string sourceConnectionString,
        string destinationConnectionString,
        int batchSize = 5000,
        int reportingFrequency = 5,
        ILogger logger = null,
        bool testMode = false)
    {
        _sourceConnectionString = sourceConnectionString;
        _destinationConnectionString = destinationConnectionString;
        _batchSize = batchSize;
        _reportingFrequency = reportingFrequency;
        _logger = logger;
        _testMode = testMode;
    }

    // Main method to transfer data for a table
    public async Task<TransferSummary> TransferTableAsync(
        string schemaName,
        string tableName,
        string timestampColumnName,
        string customWhere = null,
        string orderByColumn = null)
    {
        _overallStopwatch.Restart();
        LogInfo($"Starting incremental transfer for [{schemaName}].[{tableName}]");
        
        var summary = new TransferSummary
        {
            SchemaName = schemaName,
            TableName = tableName,
            StartTime = DateTime.UtcNow
        };

        try
        {
            // Get the last timestamp processed
            DateTime? lastTimestamp = await GetLastTimestampAsync(schemaName, tableName, timestampColumnName);
            LogInfo($"Last timestamp processed: {lastTimestamp?.ToString() ?? "None (first run)"}");
            
            orderByColumn = orderByColumn ?? timestampColumnName;
            
            int totalRowCount = await GetSourceRowCountAsync(schemaName, tableName, timestampColumnName, lastTimestamp, customWhere);
            LogInfo($"Found {totalRowCount} rows to process");
            
            summary.TotalRowsToProcess = totalRowCount;
            
            if (totalRowCount == 0)
            {
                LogInfo("No new data to transfer");
                summary.EndTime = DateTime.UtcNow;
                return summary;
            }

            // Process in batches
            int offset = 0;
            DateTime? newHighWatermark = lastTimestamp;
            
            while (offset < totalRowCount)
            {
                _batchStopwatch.Restart();
                
                var (batchSize, highWatermark) = await ProcessBatchAsync(
                    schemaName, 
                    tableName, 
                    timestampColumnName, 
                    lastTimestamp,
                    offset, 
                    _batchSize, 
                    orderByColumn,
                    customWhere);
                
                _batchStopwatch.Stop();
                
                offset += batchSize;
                _totalRowsProcessed += batchSize;
                _totalBatchesProcessed++;
                
                if (highWatermark.HasValue && (!newHighWatermark.HasValue || highWatermark > newHighWatermark))
                {
                    newHighWatermark = highWatermark;
                }
                
                // Report progress periodically
                if (_totalBatchesProcessed % _reportingFrequency == 0)
                {
                    ReportProgress(offset, totalRowCount);
                }
            }
            
            // Update watermark in tracking table
            if (newHighWatermark.HasValue && !_testMode)
            {
                await UpdateWatermarkAsync(schemaName, tableName, timestampColumnName, newHighWatermark.Value);
                LogInfo($"Updated watermark to: {newHighWatermark}");
            }
            
            _overallStopwatch.Stop();
            
            summary.RowsProcessed = _totalRowsProcessed;
            summary.EndTime = DateTime.UtcNow;
            summary.ElapsedMs = _overallStopwatch.ElapsedMilliseconds;
            summary.RowsPerSecond = _totalRowsProcessed / (_overallStopwatch.ElapsedMilliseconds / 1000.0);
            
            LogInfo($"Completed transfer. Processed {_totalRowsProcessed} rows in {_overallStopwatch.ElapsedMilliseconds/1000.0:F2} seconds " +
                  $"({summary.RowsPerSecond:F2} rows/sec)");
            
            return summary;
        }
        catch (Exception ex)
        {
            LogError($"Error transferring data: {ex.Message}", ex);
            
            summary.EndTime = DateTime.UtcNow;
            summary.ElapsedMs = _overallStopwatch.ElapsedMilliseconds;
            summary.RowsProcessed = _totalRowsProcessed;
            summary.ErrorMessage = ex.Message;
            summary.Success = false;
            
            return summary;
        }
    }

    // Process a single batch of data
    private async Task<(int rowsProcessed, DateTime? highWatermark)> ProcessBatchAsync(
        string schemaName,
        string tableName,
        string timestampColumnName,
        DateTime? lastTimestamp,
        int offset,
        int batchSize,
        string orderByColumn,
        string customWhere)
    {
        string fullTableName = $"[{schemaName}].[{tableName}]";
        DateTime? highWatermark = null;
        
        // Build WHERE clause
        string whereClause = "";
        if (lastTimestamp.HasValue)
        {
            whereClause = $"WHERE [{timestampColumnName}] > @lastTimestamp";
        }
        
        if (!string.IsNullOrEmpty(customWhere))
        {
            whereClause = string.IsNullOrEmpty(whereClause) 
                ? $"WHERE {customWhere}" 
                : $"{whereClause} AND ({customWhere})";
        }
        
        string sql = $@"
            SELECT *
            FROM {fullTableName} WITH (NOLOCK)
            {whereClause}
            ORDER BY [{orderByColumn}]
            OFFSET @offset ROWS
            FETCH NEXT @batchSize ROWS ONLY";
        
        using (var sourceConnection = new SqlConnection(_sourceConnectionString))
        using (var destConnection = new SqlConnection(_destinationConnectionString))
        {
            await sourceConnection.OpenAsync();
            await destConnection.OpenAsync();
            
            using (var command = new SqlCommand(sql, sourceConnection))
            {
                if (lastTimestamp.HasValue)
                {
                    command.Parameters.AddWithValue("@lastTimestamp", lastTimestamp.Value);
                }
                command.Parameters.AddWithValue("@offset", offset);
                command.Parameters.AddWithValue("@batchSize", batchSize);
                
                using (var reader = await command.ExecuteReaderAsync())
                {
                    // Get actual row count in this batch
                    int rowCount = 0;
                    
                    if (reader.HasRows)
                    {
                        // Find the index of the timestamp column
                        int timestampColIndex = -1;
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            if (reader.GetName(i).Equals(timestampColumnName, StringComparison.OrdinalIgnoreCase))
                            {
                                timestampColIndex = i;
                                break;
                            }
                        }
                        
                        if (!_testMode)
                        {
                            using (var bulkCopy = new SqlBulkCopy(destConnection))
                            {
                                bulkCopy.DestinationTableName = fullTableName;
                                bulkCopy.BatchSize = batchSize;
                                bulkCopy.EnableStreaming = true;
                                
                                await bulkCopy.WriteToServerAsync(reader);
                            }
                        }
                        
                        // We need to count rows and track highest timestamp
                        // This requires a second read since SqlBulkCopy consumes the reader
                        SqlDataReader secondReader = null;
                        try
                        {
                            // Re-execute the query to count rows and get highest timestamp
                            using (var countCommand = new SqlCommand(sql, sourceConnection))
                            {
                                if (lastTimestamp.HasValue)
                                {
                                    countCommand.Parameters.AddWithValue("@lastTimestamp", lastTimestamp.Value);
                                }
                                countCommand.Parameters.AddWithValue("@offset", offset);
                                countCommand.Parameters.AddWithValue("@batchSize", batchSize);
                                
                                secondReader = await countCommand.ExecuteReaderAsync();
                                
                                while (await secondReader.ReadAsync())
                                {
                                    rowCount++;
                                    
                                    if (timestampColIndex >= 0 && !secondReader.IsDBNull(timestampColIndex))
                                    {
                                        DateTime ts = secondReader.GetDateTime(timestampColIndex);
                                        if (!highWatermark.HasValue || ts > highWatermark)
                                        {
                                            highWatermark = ts;
                                        }
                                    }
                                }
                            }
                        }
                        finally
                        {
                            secondReader?.Close();
                        }
                    }
                    
                    return (rowCount, highWatermark);
                }
            }
        }
    }
    
    // Get the total count of rows to process
    private async Task<int> GetSourceRowCountAsync(
        string schemaName, 
        string tableName, 
        string timestampColumnName,
        DateTime? lastTimestamp,
        string customWhere)
    {
        string fullTableName = $"[{schemaName}].[{tableName}]";
        
        // Build WHERE clause
        string whereClause = "";
        if (lastTimestamp.HasValue)
        {
            whereClause = $"WHERE [{timestampColumnName}] > @lastTimestamp";
        }
        
        if (!string.IsNullOrEmpty(customWhere))
        {
            whereClause = string.IsNullOrEmpty(whereClause) 
                ? $"WHERE {customWhere}" 
                : $"{whereClause} AND ({customWhere})";
        }
        
        string sql = $@"
            SELECT COUNT(*)
            FROM {fullTableName} WITH (NOLOCK)
            {whereClause}";
        
        using (var connection = new SqlConnection(_sourceConnectionString))
        {
            await connection.OpenAsync();
            
            using (var command = new SqlCommand(sql, connection))
            {
                if (lastTimestamp.HasValue)
                {
                    command.Parameters.AddWithValue("@lastTimestamp", lastTimestamp.Value);
                }
                
                return (int)await command.ExecuteScalarAsync();
            }
        }
    }
    
    // Get the last timestamp processed for this table
    private async Task<DateTime?> GetLastTimestampAsync(
        string schemaName, 
        string tableName, 
        string timestampColumnName)
    {
        // First check if we have a watermark in our tracking table
        try
        {
            using (var connection = new SqlConnection(_destinationConnectionString))
            {
                await connection.OpenAsync();
                
                // Ensure watermark table exists
                await EnsureWatermarkTableExistsAsync(connection);
                
                string sql = @"
                    SELECT LastTimestamp
                    FROM dbo.IncrementalLoadWatermarks
                    WHERE SchemaName = @schemaName
                      AND TableName = @tableName
                      AND TimestampColumnName = @timestampColumnName";
                
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@schemaName", schemaName);
                    command.Parameters.AddWithValue("@tableName", tableName);
                    command.Parameters.AddWithValue("@timestampColumnName", timestampColumnName);
                    
                    var result = await command.ExecuteScalarAsync();
                    if (result != null && result != DBNull.Value)
                    {
                        return (DateTime)result;
                    }
                }
                
                // If no watermark found, we could optionally look for max value in destination table
                // For first run, uncomment this if you want to avoid full initial load
                /*
                string maxSql = $@"
                    SELECT MAX([{timestampColumnName}])
                    FROM [{schemaName}].[{tableName}]";
                    
                using (var maxCommand = new SqlCommand(maxSql, connection))
                {
                    var maxResult = await maxCommand.ExecuteScalarAsync();
                    if (maxResult != null && maxResult != DBNull.Value)
                    {
                        return (DateTime)maxResult;
                    }
                }
                */
            }
        }
        catch (Exception ex)
        {
            LogError($"Error getting last timestamp: {ex.Message}", ex);
            // Continue with null timestamp (full load) if there's an error
        }
        
        return null;
    }
    
    // Update the watermark for this table
    private async Task UpdateWatermarkAsync(
        string schemaName, 
        string tableName, 
        string timestampColumnName,
        DateTime timestamp)
    {
        using (var connection = new SqlConnection(_destinationConnectionString))
        {
            await connection.OpenAsync();
            
            await EnsureWatermarkTableExistsAsync(connection);
            
            string sql = @"
                MERGE dbo.IncrementalLoadWatermarks AS target
                USING (SELECT @schemaName, @tableName, @timestampColumnName) AS source
                    (SchemaName, TableName, TimestampColumnName)
                ON target.SchemaName = source.SchemaName
                   AND target.TableName = source.TableName
                   AND target.TimestampColumnName = source.TimestampColumnName
                WHEN MATCHED THEN
                    UPDATE SET LastTimestamp = @lastTimestamp, LastUpdated = GETUTCDATE()
                WHEN NOT MATCHED THEN
                    INSERT (SchemaName, TableName, TimestampColumnName, LastTimestamp, LastUpdated)
                    VALUES (@schemaName, @tableName, @timestampColumnName, @lastTimestamp, GETUTCDATE());";
            
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@schemaName", schemaName);
                command.Parameters.AddWithValue("@tableName", tableName);
                command.Parameters.AddWithValue("@timestampColumnName", timestampColumnName);
                command.Parameters.AddWithValue("@lastTimestamp", timestamp);
                
                await command.ExecuteNonQueryAsync();
            }
        }
    }
    
    // Ensure the watermark tracking table exists
    private async Task EnsureWatermarkTableExistsAsync(SqlConnection connection)
    {
        string sql = @"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'IncrementalLoadWatermarks' AND schema_id = SCHEMA_ID('dbo'))
            BEGIN
                CREATE TABLE dbo.IncrementalLoadWatermarks (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    SchemaName NVARCHAR(128) NOT NULL,
                    TableName NVARCHAR(128) NOT NULL,
                    TimestampColumnName NVARCHAR(128) NOT NULL,
                    LastTimestamp DATETIME2 NOT NULL,
                    LastUpdated DATETIME2 NOT NULL,
                    CONSTRAINT UK_Watermarks UNIQUE (SchemaName, TableName, TimestampColumnName)
                )
            END";
        
        using (var command = new SqlCommand(sql, connection))
        {
            await command.ExecuteNonQueryAsync();
        }
    }
    
    // Report progress
    private void ReportProgress(int currentRows, int totalRows)
    {
        double percentComplete = (double)currentRows / totalRows * 100;
        double rowsPerSecond = _totalRowsProcessed / (_overallStopwatch.ElapsedMilliseconds / 1000.0);
        double batchRowsPerSecond = _batchSize / (_batchStopwatch.ElapsedMilliseconds / 1000.0);
        
        LogInfo($"Progress: {currentRows:N0}/{totalRows:N0} rows ({percentComplete:F2}%) - " +
               $"Overall: {rowsPerSecond:F2} rows/sec, Current batch: {batchRowsPerSecond:F2} rows/sec");
    }
    
    private void LogInfo(string message)
    {
        _logger?.LogInformation(message);
        Console.WriteLine($"[INFO] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
    }
    
    private void LogError(string message, Exception ex = null)
    {
        _logger?.LogError(ex, message);
        Console.WriteLine($"[ERROR] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
        if (ex != null)
        {
            Console.WriteLine($"Exception: {ex}");
        }
    }
    
    // Class to track summary information
    public class TransferSummary
    {
        public string SchemaName { get; set; }
        public string TableName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public long ElapsedMs { get; set; }
        public int TotalRowsToProcess { get; set; }
        public int RowsProcessed { get; set; }
        public double RowsPerSecond { get; set; }
        public bool Success { get; set; } = true;
        public string ErrorMessage { get; set; }
        
        public override string ToString()
        {
            return $"Table: [{SchemaName}].[{TableName}] - " +
                   $"Processed {RowsProcessed:N0}/{TotalRowsToProcess:N0} rows in {ElapsedMs/1000.0:F2} seconds " +
                   $"({RowsPerSecond:F2} rows/sec) - " +
                   $"Success: {Success}" +
                   (Success ? "" : $" - Error: {ErrorMessage}");
        }
    }
}

// Example usage in an Azure Function
public static class DataTransferFunction
{
    public static async Task<List<IncrementalDataTransfer.TransferSummary>> Run(
        ILogger log,
        string sourceConnectionString,
        string destinationConnectionString,
        int batchSize = 5000)
    {
        var transferService = new IncrementalDataTransfer(
            sourceConnectionString,
            destinationConnectionString,
            batchSize: batchSize,
            reportingFrequency: 10,
            logger: log);
        
        var summaries = new List<IncrementalDataTransfer.TransferSummary>();
        
        // Define tables to transfer with their timestamp columns
        var tables = new List<(string schema, string table, string timestampColumn)>
        {
            ("common", "tbl_Daily_actions", "Date"),
            ("common", "tbl_Daily_actions_transactions", "TransactionDate"),
            ("common", "tbl_Daily_actions_players", "LastUpdate"),
            ("common", "tbl_Daily_actions_games", "GameDate")
        };
        
        foreach (var (schema, table, timestampColumn) in tables)
        {
            var summary = await transferService.TransferTableAsync(schema, table, timestampColumn);
            summaries.Add(summary);
        }
        
        return summaries;
    }
}