using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net.Mail;

namespace EmailProviders.MailHogEmailProvider
{
    public class MailHogEmailSender : IEmailSender
    {
        private readonly Serilog.Core.Logger _logger;
        private readonly string _fromEmail;
        private readonly string _emailServer;
        private readonly int _emailServerPort;

        public MailHogEmailSender(Serilog.Core.Logger logger, string fromEmail, string emailServer, int emailServerPort)
        {
            _logger = logger;
            _fromEmail = fromEmail;
            _emailServer = emailServer;
            _emailServerPort = emailServerPort;
        }

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            try
            {

                MailMessage Msg = new()
                {
                    From = new MailAddress(_fromEmail)
                };

                foreach (var item in email.Split(','))
                {
                    Msg.To.Add(item);
                }

                Msg.Subject = subject;
                Msg.IsBodyHtml = true;
                Msg.Body = htmlMessage;

                if (string.IsNullOrEmpty(_emailServer))
                {
                    throw new ArgumentException("Email server is not configured.");
                }

                SmtpClient client = new(_emailServer, _emailServerPort)
                {
                    // add credentials
                    UseDefaultCredentials = true
                };

                // send message
                client.Send(Msg);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.Error("Error sending email: {0}", ex.Message);
                throw new InvalidOperationException("Error sending email", ex);
            }
        }
    }
}
