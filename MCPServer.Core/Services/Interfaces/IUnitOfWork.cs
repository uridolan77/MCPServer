using System;
using System.Threading.Tasks;

namespace MCPServer.Core.Services.Interfaces
{
    /// <summary>
    /// Interface for the Unit of Work pattern to manage database transactions
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        /// <summary>
        /// Gets a repository for the specified entity type
        /// </summary>
        IRepository<TEntity> GetRepository<TEntity>() where TEntity : class;
        
        /// <summary>
        /// Begins a new transaction
        /// </summary>
        Task BeginTransactionAsync();
        
        /// <summary>
        /// Commits the current transaction
        /// </summary>
        Task CommitTransactionAsync();
        
        /// <summary>
        /// Rolls back the current transaction
        /// </summary>
        Task RollbackTransactionAsync();
        
        /// <summary>
        /// Saves all changes made in this context to the database
        /// </summary>
        Task<int> SaveChangesAsync();
    }
}
