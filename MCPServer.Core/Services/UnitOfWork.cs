using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MCPServer.Core.Data;
using MCPServer.Core.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace MCPServer.Core.Services
{
    /// <summary>
    /// Implementation of the Unit of Work pattern to manage database transactions
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly McpServerDbContext _context;
        private readonly ILoggerFactory _loggerFactory;
        private IDbContextTransaction? _transaction;
        private bool _disposed;
        private readonly Dictionary<Type, object> _repositories;

        public UnitOfWork(McpServerDbContext context, ILoggerFactory loggerFactory)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _repositories = new Dictionary<Type, object>();
        }

        /// <inheritdoc />
        public IRepository<TEntity> GetRepository<TEntity>() where TEntity : class
        {
            var type = typeof(TEntity);

            if (!_repositories.ContainsKey(type))
            {
                var logger = _loggerFactory.CreateLogger<Repository<TEntity>>();
                _repositories[type] = new Repository<TEntity>(_context, logger);
            }

            return (IRepository<TEntity>)_repositories[type];
        }

        /// <inheritdoc />
        public async Task BeginTransactionAsync()
        {
            if (_transaction != null)
            {
                throw new InvalidOperationException("A transaction is already in progress");
            }

            _transaction = await _context.Database.BeginTransactionAsync();
        }

        /// <inheritdoc />
        public async Task CommitTransactionAsync()
        {
            if (_transaction == null)
            {
                throw new InvalidOperationException("No transaction is in progress");
            }

            try
            {
                await _context.SaveChangesAsync();
                await _transaction.CommitAsync();
            }
            finally
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        /// <inheritdoc />
        public async Task RollbackTransactionAsync()
        {
            if (_transaction == null)
            {
                throw new InvalidOperationException("No transaction is in progress");
            }

            try
            {
                await _transaction.RollbackAsync();
            }
            finally
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        /// <inheritdoc />
        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _transaction?.Dispose();
                _context.Dispose();
                _disposed = true;
            }
        }
    }
}
