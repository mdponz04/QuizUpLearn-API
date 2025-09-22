using System.Net;

namespace QuizUpLearn.API.Models
{
	public class HttpException : Exception
	{
		public HttpStatusCode StatusCode { get; }
		public ApiErrorType ErrorType { get; }

		public HttpException(HttpStatusCode statusCode, string message, ApiErrorType? errorType = null) : base(message)
		{
			StatusCode = statusCode;
			ErrorType = errorType ?? MapToErrorType(statusCode);
		}

		private static ApiErrorType MapToErrorType(HttpStatusCode statusCode)
		{
			return statusCode switch
			{
				HttpStatusCode.BadRequest => ApiErrorType.BadRequest,
				HttpStatusCode.Unauthorized => ApiErrorType.Unauthorized,
				HttpStatusCode.Forbidden => ApiErrorType.Forbidden,
				HttpStatusCode.NotFound => ApiErrorType.NotFound,
				HttpStatusCode.Conflict => ApiErrorType.Conflict,
				(HttpStatusCode)422 => ApiErrorType.UnprocessableEntity,
				(HttpStatusCode)429 => ApiErrorType.TooManyRequests,
				HttpStatusCode.ServiceUnavailable => ApiErrorType.ServiceUnavailable,
				_ => ApiErrorType.InternalServerError
			};
		}
	}
}

