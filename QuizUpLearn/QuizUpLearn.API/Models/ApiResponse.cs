using System.Text.Json.Serialization;

namespace QuizUpLearn.API.Models
{
	public class ApiResponse<T>
	{
		[JsonPropertyName("success")]
		public bool Success { get; set; }

		[JsonPropertyName("data")]
		public T? Data { get; set; }

		[JsonPropertyName("message")]
		public string? Message { get; set; }

		[JsonPropertyName("error")]
		public string? Error { get; set; }

		[JsonPropertyName("errorType")]
		public ApiErrorType? ErrorType { get; set; }

		public static ApiResponse<T> Ok(T? data, string? message = null)
		{
			return new ApiResponse<T>
			{
				Success = true,
				Data = data,
				Message = message
			};
		}

		public static ApiResponse<T> Fail(string error, ApiErrorType errorType, string? message = null)
		{
			return new ApiResponse<T>
			{
				Success = false,
				Error = error,
				ErrorType = errorType,
				Message = message
			};
		}
	}
}

