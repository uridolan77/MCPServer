using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MCPServer.Core.Models.Rag;
using MCPServer.Core.Features.Shared.Services;
using MCPServer.Core.Features.Shared.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MCPServer.Core.Data.Repositories
{
    /// <summary>
    /// Repository implementation for Document entities
    /// </summary>
    public class DocumentRepository : Repository<Document>, IDocumentRepository
    {
        public DocumentRepository(McpServerDbContext context, ILogger<DocumentRepository> logger)
            : base(context, logger)
        {
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Document>> GetDocumentsByTagAsync(string tag)
        {
            try
            {
                // Using JSON_CONTAINS function in MySQL to query within the JSON array
                // This is more efficient than deserializing the entire Tags collection
                return await _context.Documents
                    .FromSqlRaw(@"
                        SELECT * FROM Documents
                        WHERE JSON_CONTAINS(Tags, JSON_QUOTE({0}))",
                        tag)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting documents by tag: {Tag}", tag);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Document>> GetDocumentsByTagsAsync(IEnumerable<string> tags)
        {
            try
            {
                var tagsList = tags.ToList();
                if (tagsList.Count == 0)
                {
                    return await GetAllAsync();
                }

                // Start with all documents
                var query = _dbSet.AsQueryable();

                // Apply each tag filter
                foreach (var tag in tagsList)
                {
                    // Using JSON_CONTAINS function in MySQL to query within the JSON array
                    var serializedTag = System.Text.Json.JsonSerializer.Serialize(tag);
                    query = query.Where(d => EF.Functions.JsonContains(
                        EF.Property<string>(d, "Tags"),
                        serializedTag));
                }

                return await query.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting documents by multiple tags");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<Dictionary<string, int>> GetTagStatisticsAsync()
        {
            var results = new Dictionary<string, int>();

            try
            {
                // Load all documents
                var documents = await _dbSet.ToListAsync();

                // Extract and count tags
                foreach (var document in documents)
                {
                    foreach (var tag in document.Tags)
                    {
                        if (results.ContainsKey(tag))
                            results[tag]++;
                        else
                            results[tag] = 1;
                    }
                }

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tag statistics");
                throw;
            }
        }
    }
}

