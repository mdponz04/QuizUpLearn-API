namespace QuizUpLearn.API.Models
{
	public enum ApiErrorType
	{
		BadRequest = 400,
		Unauthorized = 401,
		Forbidden = 403,
		NotFound = 404,
		Conflict = 409,
		UnprocessableEntity = 422,
		TooManyRequests = 429,
		InternalServerError = 500,
		ServiceUnavailable = 503
	}
}

