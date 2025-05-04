using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace MCPServer.Core.Services.Interfaces
{
    /// <summary>
    /// Generic repository interface for data access operations
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    public interface IRepository<T> where T : class
    {
        /// <summary>
        /// Gets all entities
        /// </summary>
        Task<IEnumerable<T>> GetAllAsync();
        
        /// <summary>
        /// Gets an entity by its ID
        /// </summary>
        Task<T?> GetByIdAsync(object id);
        
        /// <summary>
        /// Gets entities based on a filter expression
        /// </summary>
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        
        /// <summary>
        /// Gets a single entity based on a filter expression
        /// </summary>
        Task<T?> FindSingleAsync(Expression<Func<T, bool>> predicate);
        
        /// <summary>
        /// Adds a new entity
        /// </summary>
        Task<T> AddAsync(T entity);
        
        /// <summary>
        /// Updates an existing entity
        /// </summary>
        Task UpdateAsync(T entity);
        
        /// <summary>
        /// Deletes an entity by its ID
        /// </summary>
        Task DeleteAsync(object id);
        
        /// <summary>
        /// Gets a queryable for the entity type
        /// </summary>
        IQueryable<T> Query();
        
        /// <summary>
        /// Checks if any entity matches the given predicate
        /// </summary>
        Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);
        
        /// <summary>
        /// Gets the count of entities matching the given predicate
        /// </summary>
        Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);
    }
}
