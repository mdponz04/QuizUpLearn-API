namespace BusinessLogic.Helpers
{
    public class MailerSendRecipient
    {
        public string? Name { get; set; }
        public required string Email { get; set; }
    }

    public class MailerSendEmail
    {
        public required MailerSendRecipient From { get; set; }
        public List<MailerSendRecipient> To { get; set; } = new();
        public required string Subject { get; set; }
        public string? Text { get; set; }
        public string? Html { get; set; }

        public MailerSendEmail SetFrom(string name, string email)
        {
            From = new MailerSendRecipient { Name = name, Email = email };
            return this;
        }

        public MailerSendEmail AddRecipient(string name, string email)
        {
            To.Add(new MailerSendRecipient { Name = name, Email = email });
            return this;
        }
    }

    public class MailerSendResponse
    {
        public string? MessageId { get; set; }
    }
}

