using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using QuizUpLearn.API.Models;

namespace QuizUpLearn.API.Middlewares
{
	public class ExceptionHandlingMiddleware
	{
		private readonly RequestDelegate _next;

		public ExceptionHandlingMiddleware(RequestDelegate next)
		{
			_next = next;
		}

		public async Task InvokeAsync(HttpContext context)
		{
			if (context.Request.Path.StartsWithSegments("/game-hub") || 
			    context.Request.Path.StartsWithSegments("/one-vs-one-hub") ||
				context.Request.Path.StartsWithSegments("/background-jobs"))
			{
				await _next(context);
				return;
			}

			try
			{
				await _next(context);
			}
			catch (HttpException ex)
			{
				await WriteErrorAsync(context, ex.StatusCode, ex.Message, ex.ErrorType);
			}
			catch (UnauthorizedAccessException ex)
			{
				await WriteErrorAsync(context, HttpStatusCode.Unauthorized, ex.Message, ApiErrorType.Unauthorized);
			}
			catch (KeyNotFoundException ex)
			{
				await WriteErrorAsync(context, HttpStatusCode.BadRequest, ex.Message, ApiErrorType.BadRequest);
			}
			catch (ArgumentException ex)
			{
				await WriteErrorAsync(context, HttpStatusCode.BadRequest, ex.Message, ApiErrorType.BadRequest);
			}
			catch (TimeoutException ex)
			{
				await WriteErrorAsync(context, HttpStatusCode.InternalServerError, ex.Message, ApiErrorType.InternalServerError);
			}
			catch (Exception ex)
			{
				await WriteErrorAsync(context, HttpStatusCode.InternalServerError, ex.Message, ApiErrorType.InternalServerError);
			}
		}

		private static async Task WriteErrorAsync(HttpContext context, HttpStatusCode statusCode, string message, ApiErrorType errorType)
		{
			context.Response.ContentType = "application/json";
			context.Response.StatusCode = (int)statusCode;

			var response = ApiResponse<object>.Fail(message, errorType);
			var json = JsonSerializer.Serialize(response);
			await context.Response.WriteAsync(json);
		}
	}
}

