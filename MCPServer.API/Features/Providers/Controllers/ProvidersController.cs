using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MCPServer.Core.Models;
using MCPServer.Core.Models.Llm;
using MCPServer.Core.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MCPServer.API.Features.Shared;

namespace MCPServer.API.Features.Providers.Controllers
{
    /// <summary>
    /// Controller for managing LLM providers
    /// </summary>
    [ApiController]
    [Route("api/llm/providers")]
    [Authorize(AuthenticationSchemes = "Test,Bearer")]
    public class ProvidersController : ApiControllerBase
    {
        private readonly ILlmProviderService _providerService;

        public ProvidersController(
            ILogger<ProvidersController> logger,
            ILlmProviderService providerService)
            : base(logger)
        {
            _providerService = providerService ?? throw new ArgumentNullException(nameof(providerService));
        }

        /// <summary>
        /// Gets all providers
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<LlmProvider>>>> GetAllProviders()
        {
            try
            {
                var providers = await _providerService.GetAllProvidersAsync();
                _logger.LogInformation("Found {Count} providers", providers.Count);

                return SuccessResponse<IEnumerable<LlmProvider>>(providers);
            }
            catch (Exception ex)
            {
                return ErrorResponse<IEnumerable<LlmProvider>>("Error getting all providers", ex);
            }
        }

        /// <summary>
        /// Gets a provider by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<LlmProvider>>> GetProviderById(int id)
        {
            try
            {
                var provider = await _providerService.GetProviderByIdAsync(id);

                if (provider == null)
                {
                    return NotFoundResponse<LlmProvider>($"Provider with ID {id} not found");
                }

                return SuccessResponse(provider);
            }
            catch (Exception ex)
            {
                return ErrorResponse<LlmProvider>($"Error getting provider with ID {id}", ex);
            }
        }

        /// <summary>
        /// Adds a new provider
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<LlmProvider>>> AddProvider([FromBody] LlmProvider provider)
        {
            try
            {
                var result = await _providerService.AddProviderAsync(provider);

                return SuccessResponse(result, "Provider added successfully");
            }
            catch (Exception ex)
            {
                return ErrorResponse<LlmProvider>("Error adding provider", ex);
            }
        }

        /// <summary>
        /// Updates an existing provider
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<LlmProvider>>> UpdateProvider(int id, [FromBody] LlmProvider provider)
        {
            try
            {
                if (id != provider.Id)
                {
                    return BadRequestResponse<LlmProvider>("Provider ID mismatch");
                }

                var result = await _providerService.UpdateProviderAsync(provider);

                if (!result)
                {
                    return NotFoundResponse<LlmProvider>($"Provider with ID {id} not found");
                }

                // Get the updated provider
                var updatedProvider = await _providerService.GetProviderByIdAsync(id);
                return SuccessResponse(updatedProvider!, "Provider updated successfully");
            }
            catch (Exception ex)
            {
                return ErrorResponse<LlmProvider>($"Error updating provider with ID {id}", ex);
            }
        }

        /// <summary>
        /// Deletes a provider
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteProvider(int id)
        {
            try
            {
                var result = await _providerService.DeleteProviderAsync(id);

                if (!result)
                {
                    return NotFoundResponse<bool>($"Provider with ID {id} not found");
                }

                return SuccessResponse(true, "Provider deleted successfully");
            }
            catch (Exception ex)
            {
                return ErrorResponse<bool>($"Error deleting provider with ID {id}", ex);
            }
        }
    }
}
