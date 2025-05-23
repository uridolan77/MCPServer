using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MCPServer.Core.Models;
using MCPServer.Core.Models.Llm;
using MCPServer.Core.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MCPServer.API.Features.Shared;

namespace MCPServer.API.Features.Llm.Controllers
{
    /// <summary>
    /// Controller for public LLM operations that don't require authentication
    /// </summary>
    [ApiController]
    [Route("api/public/llm")]
    public class PublicLlmController : ApiControllerBase
    {
        private readonly ILlmProviderService _llmProviderService;

        public PublicLlmController(
            ILogger<PublicLlmController> logger,
            ILlmProviderService llmProviderService)
            : base(logger)
        {
            _llmProviderService = llmProviderService ?? throw new ArgumentNullException(nameof(llmProviderService));
        }

        /// <summary>
        /// Gets all LLM providers
        /// </summary>
        [HttpGet("providers")]
        public async Task<ActionResult<ApiResponse<List<LlmProvider>>>> GetAllProviders()
        {
            try
            {
                _logger.LogInformation("PublicLlmController: Getting all providers");
                var providers = await _llmProviderService.GetAllProvidersAsync();
                return SuccessResponse(providers, "Providers retrieved successfully");
            }
            catch (Exception ex)
            {
                return ErrorResponse<List<LlmProvider>>("Error getting all providers", ex);
            }
        }

        /// <summary>
        /// Gets all LLM models
        /// </summary>
        [HttpGet("models")]
        public async Task<ActionResult<ApiResponse<List<LlmModel>>>> GetAllModels()
        {
            try
            {
                _logger.LogInformation("PublicLlmController: Getting all models");
                var models = await _llmProviderService.GetAllModelsAsync();
                return SuccessResponse(models, "Models retrieved successfully");
            }
            catch (Exception ex)
            {
                return ErrorResponse<List<LlmModel>>("Error getting all models", ex);
            }
        }

        /// <summary>
        /// Gets a provider by ID
        /// </summary>
        [HttpGet("providers/{id}")]
        public async Task<ActionResult<ApiResponse<LlmProvider>>> GetProviderById(int id)
        {
            try
            {
                _logger.LogInformation("PublicLlmController: Getting provider by ID {ProviderId}", id);
                var provider = await _llmProviderService.GetProviderByIdAsync(id);

                if (provider == null)
                {
                    return NotFoundResponse<LlmProvider>($"Provider with ID {id} not found");
                }

                return SuccessResponse(provider, $"Provider with ID {id} retrieved successfully");
            }
            catch (Exception ex)
            {
                return ErrorResponse<LlmProvider>($"Error getting provider with ID {id}", ex);
            }
        }

        /// <summary>
        /// Gets a model by ID
        /// </summary>
        [HttpGet("models/{id}")]
        public async Task<ActionResult<ApiResponse<LlmModel>>> GetModelById(int id)
        {
            try
            {
                _logger.LogInformation("PublicLlmController: Getting model by ID {ModelId}", id);
                var model = await _llmProviderService.GetModelByIdAsync(id);

                if (model == null)
                {
                    return NotFoundResponse<LlmModel>($"Model with ID {id} not found");
                }

                return SuccessResponse(model, $"Model with ID {id} retrieved successfully");
            }
            catch (Exception ex)
            {
                return ErrorResponse<LlmModel>($"Error getting model with ID {id}", ex);
            }
        }
    }
}
