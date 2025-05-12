using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using MCPServer.Core.Models.DataTransfer;

namespace MCPServer.Core.Data
{
    /// <summary>
    /// DbContext for connecting to the ProgressPlayDB SQL Server database
    /// </summary>
    public class ProgressPlayDbContext : DbContext
    {
        public ProgressPlayDbContext(DbContextOptions<ProgressPlayDbContext> options)
            : base(options)
        {
        }

        // DataTransfer tables
        public DbSet<DataTransferConnection> DataTransferConnections { get; set; } = null!;
        public DbSet<DataTransferConfiguration> DataTransferConfigurations { get; set; } = null!;
        public DbSet<DataTransferRun> DataTransferRuns { get; set; } = null!;
        public DbSet<DataTransferLog> DataTransferLogs { get; set; } = null!;
        public DbSet<DataTransferSchedule> DataTransferSchedule { get; set; } = null!;
        public DbSet<DataTransferTableMapping> DataTransferTableMappings { get; set; } = null!;
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure DataTransferConnection entity
            modelBuilder.Entity<DataTransferConnection>(entity =>
            {
                entity.HasKey(e => e.ConnectionId);
                entity.Property(e => e.ConnectionName).HasMaxLength(100).IsRequired();
                entity.Property(e => e.ConnectionString).HasMaxLength(1000).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.ConnectionAccessLevel).HasMaxLength(20).IsRequired();
                entity.Property(e => e.CreatedBy).HasMaxLength(100).IsRequired();
                entity.Property(e => e.LastModifiedBy).HasMaxLength(100);
                entity.HasIndex(e => e.ConnectionName).IsUnique();
                entity.ToTable("DataTransferConnections", "dbo");
            });
            
            // Configure DataTransferConfiguration entity
            modelBuilder.Entity<DataTransferConfiguration>(entity =>
            {
                entity.HasKey(e => e.ConfigurationId);
                entity.Property(e => e.ConfigurationName).HasMaxLength(128).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.CreatedBy).HasMaxLength(128).IsRequired();
                entity.Property(e => e.LastModifiedBy).HasMaxLength(128);
                entity.HasIndex(e => e.ConfigurationName).IsUnique();
                
                // Configure foreign key relationships
                entity.HasOne(d => d.SourceConnection)
                      .WithMany()
                      .HasForeignKey(d => d.SourceConnectionId)
                      .OnDelete(DeleteBehavior.Restrict);
                
                entity.HasOne(d => d.DestinationConnection)
                      .WithMany()
                      .HasForeignKey(d => d.DestinationConnectionId)
                      .OnDelete(DeleteBehavior.Restrict);
                
                entity.ToTable("DataTransferConfigurations", "dbo");
            });
            
            // Configure DataTransferRun entity
            modelBuilder.Entity<DataTransferRun>(entity =>
            {
                entity.HasKey(e => e.RunId);
                entity.Property(e => e.Status).HasMaxLength(50).IsRequired();
                entity.Property(e => e.TriggeredBy).HasMaxLength(100);
                
                // Configure foreign key relationship
                entity.HasOne(d => d.Configuration)
                      .WithMany()
                      .HasForeignKey(d => d.ConfigurationId)
                      .OnDelete(DeleteBehavior.Restrict);
                
                entity.ToTable("DataTransferRuns", "dbo");
            });
            
            // Configure DataTransferLog entity
            modelBuilder.Entity<DataTransferLog>(entity =>
            {
                entity.HasKey(e => e.LogId);
                entity.Property(e => e.LogLevel).HasMaxLength(20).IsRequired();
                entity.Property(e => e.Message).IsRequired();
                
                // Configure foreign key relationship
                entity.HasOne(d => d.Run)
                      .WithMany()
                      .HasForeignKey(d => d.RunId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                entity.ToTable("DataTransferLogs", "dbo");
            });
            
            // Configure DataTransferSchedule entity
            modelBuilder.Entity<DataTransferSchedule>(entity =>
            {
                entity.HasKey(e => e.ScheduleId);
                entity.Property(e => e.ScheduleType).HasMaxLength(20).IsRequired();
                entity.Property(e => e.FrequencyUnit).HasMaxLength(20);
                entity.Property(e => e.WeekDays).HasMaxLength(100);
                entity.Property(e => e.MonthDays).HasMaxLength(100);
                entity.Property(e => e.CreatedBy).HasMaxLength(100).IsRequired();
                entity.Property(e => e.LastModifiedBy).HasMaxLength(100);
                
                // Configure foreign key relationship
                entity.HasOne(d => d.Configuration)
                      .WithMany()
                      .HasForeignKey(d => d.ConfigurationId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                entity.ToTable("DataTransferSchedule", "dbo");
            });
            
            // Configure DataTransferTableMapping entity
            modelBuilder.Entity<DataTransferTableMapping>(entity =>
            {
                entity.HasKey(e => e.MappingId);
                entity.Property(e => e.SchemaName).HasMaxLength(128).IsRequired();
                entity.Property(e => e.TableName).HasMaxLength(128).IsRequired();
                entity.Property(e => e.TimestampColumnName).HasMaxLength(128).IsRequired();
                entity.Property(e => e.OrderByColumn).HasMaxLength(128);
                entity.Property(e => e.CustomWhereClause).HasMaxLength(1024);
                
                // Configure foreign key relationship
                entity.HasOne(d => d.Configuration)
                      .WithMany()
                      .HasForeignKey(d => d.ConfigurationId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                entity.ToTable("DataTransferTableMappings", "dbo");
            });
        }
    }
}