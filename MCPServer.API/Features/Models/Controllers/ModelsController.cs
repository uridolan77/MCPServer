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

namespace MCPServer.API.Features.Models.Controllers
{
    /// <summary>
    /// Controller for managing LLM models
    /// </summary>
    [ApiController]
    [Route("api/llm/models")]
    [Authorize(AuthenticationSchemes = "Test,Bearer")]
    public class ModelsController : ApiControllerBase
    {
        private readonly ILlmProviderService _providerService;

        public ModelsController(
            ILogger<ModelsController> logger,
            ILlmProviderService providerService)
            : base(logger)
        {
            _providerService = providerService ?? throw new ArgumentNullException(nameof(providerService));
        }

        /// <summary>
        /// Gets all models
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<LlmModel>>>> GetAllModels()
        {
            try
            {
                var models = await _providerService.GetAllModelsAsync();
                _logger.LogInformation("Found {Count} models", models.Count);

                return SuccessResponse<IEnumerable<LlmModel>>(models);
            }
            catch (Exception ex)
            {
                return ErrorResponse<IEnumerable<LlmModel>>("Error getting all models", ex);
            }
        }

        /// <summary>
        /// Gets models by provider ID
        /// </summary>
        [HttpGet("provider/{providerId}")]
        public async Task<ActionResult<ApiResponse<IEnumerable<LlmModel>>>> GetModelsByProviderId(int providerId)
        {
            try
            {
                var models = await _providerService.GetModelsByProviderIdAsync(providerId);
                _logger.LogInformation("Found {Count} models for provider {ProviderId}", models.Count, providerId);

                return SuccessResponse<IEnumerable<LlmModel>>(models);
            }
            catch (Exception ex)
            {
                return ErrorResponse<IEnumerable<LlmModel>>($"Error getting models for provider {providerId}", ex);
            }
        }

        /// <summary>
        /// Gets a model by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<LlmModel>>> GetModelById(int id)
        {
            try
            {
                var model = await _providerService.GetModelByIdAsync(id);

                if (model == null)
                {
                    return NotFoundResponse<LlmModel>($"Model with ID {id} not found");
                }

                return SuccessResponse(model);
            }
            catch (Exception ex)
            {
                return ErrorResponse<LlmModel>($"Error getting model with ID {id}", ex);
            }
        }

        /// <summary>
        /// Adds a new model
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<LlmModel>>> AddModel([FromBody] LlmModel model)
        {
            try
            {
                var result = await _providerService.AddModelAsync(model);

                return SuccessResponse(result, "Model added successfully");
            }
            catch (Exception ex)
            {
                return ErrorResponse<LlmModel>("Error adding model", ex);
            }
        }

        /// <summary>
        /// Updates an existing model
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<LlmModel>>> UpdateModel(int id, [FromBody] LlmModel model)
        {
            try
            {
                if (id != model.Id)
                {
                    return BadRequestResponse<LlmModel>("Model ID mismatch");
                }

                var result = await _providerService.UpdateModelAsync(model);

                if (!result)
                {
                    return NotFoundResponse<LlmModel>($"Model with ID {id} not found");
                }

                // Get the updated model
                var updatedModel = await _providerService.GetModelByIdAsync(id);
                return SuccessResponse(updatedModel!, "Model updated successfully");
            }
            catch (Exception ex)
            {
                return ErrorResponse<LlmModel>($"Error updating model with ID {id}", ex);
            }
        }

        /// <summary>
        /// Deletes a model
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteModel(int id)
        {
            try
            {
                var result = await _providerService.DeleteModelAsync(id);

                if (!result)
                {
                    return NotFoundResponse<bool>($"Model with ID {id} not found");
                }

                return SuccessResponse(true, "Model deleted successfully");
            }
            catch (Exception ex)
            {
                return ErrorResponse<bool>($"Error deleting model with ID {id}", ex);
            }
        }
    }
}
