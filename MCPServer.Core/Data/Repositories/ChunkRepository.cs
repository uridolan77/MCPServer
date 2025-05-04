using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MCPServer.Core.Models.Rag;
using MCPServer.Core.Services;
using MCPServer.Core.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MCPServer.Core.Data.Repositories
{
    /// <summary>
    /// Repository implementation for Chunk entities
    /// </summary>
    public class ChunkRepository : Repository<Chunk>, IChunkRepository
    {
        public ChunkRepository(McpServerDbContext context, ILogger<ChunkRepository> logger)
            : base(context, logger)
        {
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Chunk>> GetChunksByDocumentIdAsync(string documentId)
        {
            try
            {
                return await _dbSet
                    .Where(c => c.DocumentId == documentId)
                    .OrderBy(c => c.ChunkIndex)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chunks by document ID: {DocumentId}", documentId);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<bool> DeleteChunksByDocumentIdAsync(string documentId)
        {
            try
            {
                var chunks = await _dbSet
                    .Where(c => c.DocumentId == documentId)
                    .ToListAsync();

                if (chunks.Count == 0)
                {
                    return true;
                }

                _dbSet.RemoveRange(chunks);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted {ChunkCount} chunks for document ID: {DocumentId}", chunks.Count, documentId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting chunks for document ID: {DocumentId}", documentId);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Chunk>> GetChunksByMetadataAsync(Dictionary<string, string> metadata)
        {
            try
            {
                if (metadata == null || metadata.Count == 0)
                {
                    return await GetAllAsync();
                }

                // Since metadata is stored as JSON, we need to filter in memory
                // This is not efficient for large datasets, but works for now
                var chunks = await _dbSet.ToListAsync();
                
                return chunks.Where(c =>
                    metadata.All(m =>
                        c.Metadata.ContainsKey(m.Key) &&
                        c.Metadata[m.Key] == m.Value)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chunks by metadata");
                throw;
            }
        }
    }
}
