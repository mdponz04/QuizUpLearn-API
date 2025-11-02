using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using QuizUpLearn.API.Models;

namespace QuizUpLearn.API.Middlewares
{
	public class ApiResponseWrappingMiddleware
	{
		private readonly RequestDelegate _next;

		public ApiResponseWrappingMiddleware(RequestDelegate next)
		{
			_next = next;
		}

		public async Task InvokeAsync(HttpContext context)
		{
			// Bỏ qua swagger
			if (context.Request.Path.StartsWithSegments("/swagger"))
			{
				await _next(context);
				return;
			}

			if (context.Request.Path.StartsWithSegments("/game-hub") || 
			    context.Request.Path.StartsWithSegments("/one-vs-one-hub"))
			{
				await _next(context);
				return;
			}

			var originalBody = context.Response.Body;
			await using var buffer = new MemoryStream();
			context.Response.Body = buffer;

			try
			{
				await _next(context);

				buffer.Seek(0, SeekOrigin.Begin);
				var body = await new StreamReader(buffer).ReadToEndAsync();
				buffer.Seek(0, SeekOrigin.Begin);

				// Chỉ bọc khi là 2xx và content-type là application/json
				var status = context.Response.StatusCode;
				var contentType = context.Response.ContentType ?? string.Empty;
				var isSuccess = status >= 200 && status < 300;
				var isJson = contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase);

				if (!isSuccess || !isJson)
				{
					context.Response.Body = originalBody;
					await buffer.CopyToAsync(originalBody);
					return;
				}

				object? data;
				try
				{
					data = string.IsNullOrWhiteSpace(body) ? null : JsonSerializer.Deserialize<object>(body);
				}
				catch
				{
					data = body;
				}

				var wrapped = ApiResponse<object>.Ok(data, null);
				var json = JsonSerializer.Serialize(wrapped);
				context.Response.Body = originalBody;
				context.Response.ContentType = "application/json";
				await context.Response.WriteAsync(json, Encoding.UTF8);
			}
			catch
			{
				// Trả body về stream gốc để middleware xử lý exception có thể ghi ra
				context.Response.Body = originalBody;
				throw;
			}
		}
	}
}

