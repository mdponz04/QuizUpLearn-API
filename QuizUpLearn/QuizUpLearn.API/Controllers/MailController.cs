using BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizUpLearn.API.Attributes;
using Repository.Models;

namespace QuizUpLearn.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MailController : ControllerBase
    {
        private readonly IMailerSendService _mailerSendService;
        private readonly IConfiguration _configuration;

        public MailController(IMailerSendService mailerSendService, IConfiguration configuration)
        {
            _mailerSendService = mailerSendService;
            _configuration = configuration;
        }

        [HttpPost("send")]
        [SubscriptionAndRoleAuthorize("Moderator")]
        public async Task<IActionResult> Send()
        {
            var fromEmail = _configuration["MailerSend:FromEmail"];
            var fromName = _configuration["MailerSend:FromName"];

            var email = new MailerSendEmail
            {
                From = new MailerSendRecipient { Name = fromName, Email = fromEmail! },
                Subject = "Xin chào từ MailerSend 🚀",
                Text = "Đây là email text",
                Html = "<h1>Hello 👋</h1><p>This is HTML content</p>"
            };
            email.To.Add(new MailerSendRecipient { Name = "User One", Email = "manhmanhd0402@gmail.com" });

            var result = await _mailerSendService.SendEmailAsync(email);
            return Ok(result);
        }
    }
}

