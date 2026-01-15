using BusinessLogic.Helpers;

namespace BusinessLogic.Interfaces
{
    public interface IMailerSendService
    {
        Task<object?> SendEmailAsync(MailerSendEmail email);
    }
}

