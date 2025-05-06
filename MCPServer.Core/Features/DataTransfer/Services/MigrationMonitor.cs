using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace MCPServer.Core.Features.DataTransfer.Services
{
    public class MigrationMonitor
    {
        private readonly ILogger _logger;
        private readonly Dictionary<string, TableMetrics> _tableMetrics = new Dictionary<string, TableMetrics>();
        private readonly Stopwatch _overallStopwatch = new Stopwatch();

        public MigrationMonitor(ILogger logger)
        {
            _logger = logger;
        }

        public void StartMigration()
        {
            _overallStopwatch.Start();
            _logger.LogInformation("Migration started at {StartTime}", DateTime.Now);
        }

        public void EndMigration()
        {
            _overallStopwatch.Stop();
            _logger.LogInformation("Migration completed at {EndTime}", DateTime.Now);
            _logger.LogInformation("Total migration time: {TotalTime}", _overallStopwatch.Elapsed);

            // Log metrics for each table
            foreach (var metric in _tableMetrics.Values)
            {
                _logger.LogInformation("Table {TableName}: {RowCount} rows, {ElapsedTime} elapsed, {RowsPerSecond:F1} rows/sec",
                    metric.TableName,
                    metric.RowCount,
                    metric.ElapsedTime,
                    metric.RowsPerSecond);
            }
        }

        public void StartTableMigration(string tableName)
        {
            var metric = new TableMetrics
            {
                TableName = tableName,
                StartTime = DateTime.Now
            };
            
            metric.Stopwatch.Start();
            _tableMetrics[tableName] = metric;
        }

        public void EndTableMigration(string tableName, int rowCount)
        {
            if (_tableMetrics.TryGetValue(tableName, out var metric))
            {
                metric.Stopwatch.Stop();
                metric.EndTime = DateTime.Now;
                metric.RowCount = rowCount;
                metric.ElapsedTime = metric.Stopwatch.Elapsed;
                metric.RowsPerSecond = rowCount / Math.Max(1, metric.ElapsedTime.TotalSeconds);

                _logger.LogInformation(
                    "Completed migration for table {TableName}: {RowCount} rows in {ElapsedTime}",
                    tableName, rowCount, metric.ElapsedTime);
            }
        }

        public void UpdateTableProgress(string tableName, int currentRowCount)
        {
            if (_tableMetrics.TryGetValue(tableName, out var metric))
            {
                var currentElapsed = metric.Stopwatch.Elapsed;
                var rowsPerSecond = currentRowCount / Math.Max(1, currentElapsed.TotalSeconds);
                
                _logger.LogDebug(
                    "Table {TableName} progress: {CurrentRowCount} rows, {RowsPerSecond:F1} rows/sec",
                    tableName, currentRowCount, rowsPerSecond);
            }
        }

        public class TableMetrics
        {
            public string TableName { get; set; }
            public DateTime StartTime { get; set; }
            public DateTime EndTime { get; set; }
            public TimeSpan ElapsedTime { get; set; }
            public int RowCount { get; set; }
            public double RowsPerSecond { get; set; }
            public Stopwatch Stopwatch { get; } = new Stopwatch();
        }
    }
}