using System;
using System.Collections.Generic;

namespace MCPServer.Core.Models
{
    /// <summary>
    /// Standard API response model for consistent response format
    /// </summary>
    /// <typeparam name="T">The type of data being returned</typeparam>
    public class ApiResponse<T>
    {
        /// <summary>
        /// Indicates whether the request was successful
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// The data returned by the API
        /// </summary>
        public T? Data { get; set; }
        
        /// <summary>
        /// A message describing the result of the operation
        /// </summary>
        public string? Message { get; set; }
        
        /// <summary>
        /// A list of error messages if the request failed
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();
        
        /// <summary>
        /// Creates a successful response with the provided data
        /// </summary>
        public static ApiResponse<T> SuccessResponse(T data, string message = "Operation completed successfully")
        {
            return new ApiResponse<T>
            {
                Success = true,
                Data = data,
                Message = message
            };
        }
        
        /// <summary>
        /// Creates an error response with the provided message and errors
        /// </summary>
        public static ApiResponse<T> ErrorResponse(string message, List<string>? errors = null)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Errors = errors ?? new List<string>()
            };
        }
        
        /// <summary>
        /// Creates an error response with a single error message
        /// </summary>
        public static ApiResponse<T> ErrorResponse(string message, string error)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Errors = new List<string> { error }
            };
        }
        
        /// <summary>
        /// Creates an error response from an exception
        /// </summary>
        public static ApiResponse<T> ErrorResponse(string message, Exception ex)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Errors = new List<string> { ex.Message }
            };
        }
    }
}
