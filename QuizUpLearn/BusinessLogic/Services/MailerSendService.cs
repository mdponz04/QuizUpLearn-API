using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using BusinessLogic.Helpers;
using BusinessLogic.Interfaces;
using Microsoft.Extensions.Configuration;

namespace BusinessLogic.Services
{
    public class MailerSendService : IMailerSendService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _emailEndpoint;

        public MailerSendService(IConfiguration configuration, HttpClient httpClient)
        {
            _httpClient = httpClient;
            _apiKey = configuration["MailerSend:ApiKey"] ?? string.Empty;
            var baseUrl = configuration["MailerSend:BaseUrl"] ?? "https://api.mailersend.com";
            _emailEndpoint = configuration["MailerSend:EmailEndpoint"] ?? "/v1/email";
            if (_httpClient.BaseAddress == null)
            {
                _httpClient.BaseAddress = new Uri(baseUrl);
            }
        }

        public async Task<object?> SendEmailAsync(MailerSendEmail email)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
                throw new InvalidOperationException("MailerSend API key is missing.");

            var request = new HttpRequestMessage(HttpMethod.Post, _emailEndpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var payload = new
            {
                from = new { email = email.From.Email, name = email.From.Name },
                to = email.To.Select(r => new { email = r.Email, name = r.Name }).ToArray(),
                subject = email.Subject,
                text = email.Text,
                html = email.Html
            };

            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull });
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var responseString = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                // Trả nguyên body lỗi để middleware bọc vào ApiResponse.Fail
                throw new InvalidOperationException($"MailerSend error ({(int)response.StatusCode}): {responseString}");
            }
            if (string.IsNullOrWhiteSpace(responseString))
            {
                return null; // thành công nhưng không có body
            }
            try
            {
                // Trả về JSON object gốc của MailerSend (deserialized)
                return JsonSerializer.Deserialize<object>(responseString);
            }
            catch
            {
                // Không phải JSON, trả về chuỗi thô
                return responseString;
            }
        }
    }
}

