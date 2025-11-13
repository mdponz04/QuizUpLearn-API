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
            if (context.Request.Path.StartsWithSegments("/swagger"))
            {
                await _next(context);
                return;
            }

            if (context.Request.Path.StartsWithSegments("/game-hub") || 
                context.Request.Path.StartsWithSegments("/one-vs-one-hub") ||
                context.Request.Path.StartsWithSegments("/background-jobs"))
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

                bool isAlreadyWrapped = false;
                bool isPaginationResponse = false;
                object? paginationData = null;
                object? responseData = null;

                try
                {
                    if (!string.IsNullOrWhiteSpace(body))
                    {
                        using var doc = JsonDocument.Parse(body);
                        var root = doc.RootElement;
                        if (root.ValueKind == JsonValueKind.Object)
                        {
                            if (root.TryGetProperty("success", out _))
                            {
                                isAlreadyWrapped = true;
                            }
                            else if (root.TryGetProperty("data", out var dataProp) && 
                                     root.TryGetProperty("pagination", out var paginationProp))
                            {
                                isPaginationResponse = true;
                                responseData = JsonSerializer.Deserialize<object>(dataProp.GetRawText());
                                paginationData = JsonSerializer.Deserialize<object>(paginationProp.GetRawText());
                            }

                            // Kiểm tra response có pagination info (TotalCount/totalCount, Page/page, PageSize/pageSize) nhưng không có cấu trúc PaginationResponseDto
                            // Ví dụ: PlayerHistoryResponseDto có Attempts/attempts thay vì data
                            else if ((root.TryGetProperty("TotalCount", out _) || root.TryGetProperty("totalCount", out _)) &&
                                     (root.TryGetProperty("Page", out _) || root.TryGetProperty("page", out _)) &&
                                     (root.TryGetProperty("PageSize", out _) || root.TryGetProperty("pageSize", out _)))
                            {
                                // Tìm property chứa data array (Attempts, attempts hoặc data)
                                JsonElement dataPropElement;
                                bool hasDataProperty = false;
                                
                                // Kiểm tra Attempts (chữ hoa) - từ C# property
                                if (root.TryGetProperty("Attempts", out dataPropElement))
                                {
                                    isPaginationResponse = true;
                                    hasDataProperty = true;
                                    responseData = JsonSerializer.Deserialize<object>(dataPropElement.GetRawText());
                                }
                                // Kiểm tra attempts (chữ thường) - từ JSON serialization
                                else if (root.TryGetProperty("attempts", out dataPropElement))
                                {
                                    isPaginationResponse = true;
                                    hasDataProperty = true;
                                    responseData = JsonSerializer.Deserialize<object>(dataPropElement.GetRawText());
                                }
                                // Kiểm tra data property
                                else if (root.TryGetProperty("data", out dataPropElement))
                                {
                                    isPaginationResponse = true;
                                    hasDataProperty = true;
                                    responseData = JsonSerializer.Deserialize<object>(dataPropElement.GetRawText());
                                }
                                
                                if (isPaginationResponse && hasDataProperty)
                                {
                                    // Tạo pagination object từ các property (kiểm tra cả chữ hoa và chữ thường)
                                    var totalCount = root.TryGetProperty("TotalCount", out var tc) ? tc.GetInt32() : 
                                                    (root.TryGetProperty("totalCount", out var tc2) ? tc2.GetInt32() : 0);
                                    var page = root.TryGetProperty("Page", out var p) ? p.GetInt32() : 
                                              (root.TryGetProperty("page", out var p2) ? p2.GetInt32() : 1);
                                    var pageSize = root.TryGetProperty("PageSize", out var ps) ? ps.GetInt32() : 
                                                  (root.TryGetProperty("pageSize", out var ps2) ? ps2.GetInt32() : 10);
                                    var totalPages = root.TryGetProperty("TotalPages", out var tp) ? tp.GetInt32() : 
                                                    (root.TryGetProperty("totalPages", out var tp2) ? tp2.GetInt32() : (int)Math.Ceiling((double)totalCount / pageSize));
                                    var hasNextPage = root.TryGetProperty("HasNextPage", out var hnp) ? hnp.GetBoolean() : 
                                                     (root.TryGetProperty("hasNextPage", out var hnp2) ? hnp2.GetBoolean() : page < totalPages);
                                    var hasPreviousPage = root.TryGetProperty("HasPreviousPage", out var hpp) ? hpp.GetBoolean() : 
                                                         (root.TryGetProperty("hasPreviousPage", out var hpp2) ? hpp2.GetBoolean() : page > 1);
                                    
                                    paginationData = new
                                    {
                                        currentPage = page,
                                        pageSize = pageSize,
                                        totalCount = totalCount,
                                        totalPages = totalPages,
                                        hasNextPage = hasNextPage,
                                        hasPreviousPage = hasPreviousPage,
                                        searchTerm = (string?)null,
                                        sortBy = (string?)null,
                                        sortDirection = (string?)null
                                    };
                                }
                            }
                        }
                    }
                }
                catch
                {
                }

                string json;
                if (isAlreadyWrapped)
                {
                    json = body; 
                }
                else if (isPaginationResponse)
                {
                    // Response có pagination: tách data và pagination ra ngoài cùng cấp
                    // data là array trực tiếp (từ Attempts hoặc data property), không phải nested object
                    var wrapped = new
                    {
                        success = true,
                        data = responseData,  // Array trực tiếp, không phải data.Attempts
                        pagination = paginationData,
                        message = (string?)null
                    };
                    json = JsonSerializer.Serialize(wrapped);
                }
                else
                {
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
                    json = JsonSerializer.Serialize(wrapped);
                }

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

