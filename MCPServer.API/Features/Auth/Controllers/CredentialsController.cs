using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MCPServer.API.Features.Auth.Models;
using MCPServer.Core.Models;
using MCPServer.Core.Models.Llm;
using MCPServer.Core.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MCPServer.API.Features.Shared;

namespace MCPServer.API.Features.Auth.Controllers
{
    /// <summary>
    /// Controller for managing LLM provider credentials
    /// </summary>
    [ApiController]
    [Route("api/llm/credentials")]
    [Authorize(AuthenticationSchemes = "Test,Bearer")]
    public class CredentialsController : ApiControllerBase
    {
        private readonly ICredentialService _credentialService;

        public CredentialsController(
            ILogger<CredentialsController> logger,
            ICredentialService credentialService)
            : base(logger)
        {
            _credentialService = credentialService ?? throw new ArgumentNullException(nameof(credentialService));
        }

        /// <summary>
        /// Gets all credentials for the current user
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<LlmProviderCredential>>>> GetUserCredentials()
        {
            try
            {
                var userId = GetCurrentUserId();
                var credentials = await _credentialService.GetCredentialsByUserIdAsync(userId);

                _logger.LogInformation("Found {Count} credentials for user {UserId}", credentials.Count, userId);

                return SuccessResponse<IEnumerable<LlmProviderCredential>>(credentials);
            }
            catch (Exception ex)
            {
                return ErrorResponse<IEnumerable<LlmProviderCredential>>("Error getting user credentials", ex);
            }
        }

        /// <summary>
        /// Gets credentials by provider ID
        /// </summary>
        [HttpGet("provider/{providerId}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<IEnumerable<LlmProviderCredential>>>> GetCredentialsByProviderId(int providerId)
        {
            try
            {
                var credentials = await _credentialService.GetCredentialsByProviderIdAsync(providerId);

                _logger.LogInformation("Found {Count} credentials for provider {ProviderId}", credentials.Count, providerId);

                return SuccessResponse<IEnumerable<LlmProviderCredential>>(credentials);
            }
            catch (Exception ex)
            {
                return ErrorResponse<IEnumerable<LlmProviderCredential>>($"Error getting credentials for provider {providerId}", ex);
            }
        }

        /// <summary>
        /// Gets a credential by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<LlmProviderCredential>>> GetCredentialById(int id)
        {
            try
            {
                var credential = await _credentialService.GetCredentialByIdAsync(id);

                if (credential == null)
                {
                    return NotFoundResponse<LlmProviderCredential>($"Credential with ID {id} not found");
                }

                // Check if user has access to this credential
                var userId = GetCurrentUserId();
                var hasAccess = await _credentialService.ValidateUserAccessAsync(id, userId, IsUserInRole("Admin"));

                if (!hasAccess && !IsUserInRole("Admin"))
                {
                    return ForbiddenResponse<LlmProviderCredential>("You do not have access to this credential");
                }

                return SuccessResponse(credential);
            }
            catch (Exception ex)
            {
                return ErrorResponse<LlmProviderCredential>($"Error getting credential with ID {id}", ex);
            }
        }

        /// <summary>
        /// Adds a new credential
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<LlmProviderCredential>>> AddCredential([FromBody] CredentialRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();

                // Only admins can create system-wide credentials
                if (request.UserId == null && !IsUserInRole("Admin"))
                {
                    return ForbiddenResponse<LlmProviderCredential>("Only administrators can create system-wide credentials");
                }

                // Regular users can only create their own credentials
                if (request.UserId.HasValue && request.UserId != userId && !IsUserInRole("Admin"))
                {
                    return ForbiddenResponse<LlmProviderCredential>("You can only create credentials for your own account");
                }

                var credential = new LlmProviderCredential
                {
                    ProviderId = request.ProviderId,
                    UserId = request.UserId,
                    Name = request.Name,
                    IsEnabled = true
                };

                // Ensure we have a valid credentials object
                var credentialsObj = request.Credentials ?? new { };

                var result = await _credentialService.AddCredentialAsync(credential, credentialsObj);

                return SuccessResponse(result, "Credential added successfully");
            }
            catch (Exception ex)
            {
                return ErrorResponse<LlmProviderCredential>("Error adding credential", ex);
            }
        }

        /// <summary>
        /// Updates an existing credential
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<LlmProviderCredential>>> UpdateCredential(int id, [FromBody] CredentialRequest request)
        {
            try
            {
                var existingCredential = await _credentialService.GetCredentialByIdAsync(id);

                if (existingCredential == null)
                {
                    return NotFoundResponse<LlmProviderCredential>($"Credential with ID {id} not found");
                }

                // Check if user has access to this credential
                var userId = GetCurrentUserId();
                var hasAccess = await _credentialService.ValidateUserAccessAsync(id, userId, IsUserInRole("Admin"));

                if (!hasAccess && !IsUserInRole("Admin"))
                {
                    return ForbiddenResponse<LlmProviderCredential>("You do not have access to this credential");
                }

                // Update credential properties
                existingCredential.Name = request.Name;
                existingCredential.ProviderId = request.ProviderId;

                // Only admins can change the user ID
                if (IsUserInRole("Admin"))
                {
                    existingCredential.UserId = request.UserId;
                }

                var result = await _credentialService.UpdateCredentialAsync(existingCredential, request.Credentials);

                if (!result)
                {
                    return StatusCode(500, ApiResponse<LlmProviderCredential>.ErrorResponse("Failed to update credential"));
                }

                // Get the updated credential
                var updatedCredential = await _credentialService.GetCredentialByIdAsync(id);
                return SuccessResponse(updatedCredential!, "Credential updated successfully");
            }
            catch (Exception ex)
            {
                return ErrorResponse<LlmProviderCredential>($"Error updating credential with ID {id}", ex);
            }
        }

        /// <summary>
        /// Deletes a credential
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteCredential(int id)
        {
            try
            {
                var credential = await _credentialService.GetCredentialByIdAsync(id);

                if (credential == null)
                {
                    return NotFoundResponse<bool>($"Credential with ID {id} not found");
                }

                // Check if user has access to this credential
                var userId = GetCurrentUserId();
                var hasAccess = await _credentialService.ValidateUserAccessAsync(id, userId, IsUserInRole("Admin"));

                if (!hasAccess && !IsUserInRole("Admin"))
                {
                    return ForbiddenResponse<bool>("You do not have access to this credential");
                }

                var result = await _credentialService.DeleteCredentialAsync(id);

                if (!result)
                {
                    return StatusCode(500, ApiResponse<bool>.ErrorResponse("Failed to delete credential"));
                }

                return SuccessResponse(true, "Credential deleted successfully");
            }
            catch (Exception ex)
            {
                return ErrorResponse<bool>($"Error deleting credential with ID {id}", ex);
            }
        }

        /// <summary>
        /// Sets a credential as the default for the current user
        /// </summary>
        [HttpPost("{id}/set-default")]
        public async Task<ActionResult<ApiResponse<bool>>> SetDefaultCredential(int id)
        {
            try
            {
                var credential = await _credentialService.GetCredentialByIdAsync(id);

                if (credential == null)
                {
                    return NotFoundResponse<bool>($"Credential with ID {id} not found");
                }

                // Check if user has access to this credential
                var userId = GetCurrentUserId();
                var hasAccess = await _credentialService.ValidateUserAccessAsync(id, userId, IsUserInRole("Admin"));

                if (!hasAccess && !IsUserInRole("Admin"))
                {
                    return ForbiddenResponse<bool>("You do not have access to this credential");
                }

                // Only admins can set system-wide default credentials
                if (credential.UserId == null && !IsUserInRole("Admin"))
                {
                    return ForbiddenResponse<bool>("Only administrators can set system-wide default credentials");
                }

                var result = await _credentialService.SetDefaultCredentialAsync(id, credential.UserId);

                if (!result)
                {
                    return StatusCode(500, ApiResponse<bool>.ErrorResponse("Failed to set default credential"));
                }

                return SuccessResponse(true, "Default credential set successfully");
            }
            catch (Exception ex)
            {
                return ErrorResponse<bool>($"Error setting default credential with ID {id}", ex);
            }
        }
    }
}


