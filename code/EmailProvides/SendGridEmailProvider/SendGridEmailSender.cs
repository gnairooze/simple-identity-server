using Microsoft.AspNetCore.Identity.UI.Services;
using SendGrid.Helpers.Mail;
using SendGrid;
using System.Text.RegularExpressions;
using System.Net;

namespace EmailProviders.SendGridEmailProvider
{
    public partial class SendGridEmailSender : IEmailSender
    {
        private readonly Serilog.Core.Logger _logger;
        private readonly string _apiKey;
        private readonly string _fromEmail;
        private readonly string _fromName;

        public SendGridEmailSender(Serilog.Core.Logger logger, string apiKey, string fromEmail, string fromName)
        {
            _logger = logger;
            _apiKey = apiKey;
            _fromEmail = fromEmail;
            _fromName = fromName;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            bool isValid = ValidateInput(email, subject, htmlMessage);

            if (!isValid)
            {
                throw new ArgumentException("Invalid input parameters for SendGridEmailSender");
            }

            var client = new SendGridClient(_apiKey);
            var from = new EmailAddress(_fromEmail, _fromName);

            var tos = new List<EmailAddress>();

            foreach (var item in email.Split(','))
            {
                tos.Add(new EmailAddress(item));
            }

            string plainTextContent = ExtractTextFromHtml(htmlMessage);

            var msg = MailHelper.CreateSingleEmailToMultipleRecipients(from, tos, subject, plainTextContent, htmlMessage);
            var response = await client.SendEmailAsync(msg);
            var result = response.Body.ReadAsStringAsync().Result;

            if (response.IsSuccessStatusCode && response.StatusCode == System.Net.HttpStatusCode.Accepted)
            {
                _logger.Information("email sent");
            }
            else
            {
                _logger.Error("Status Code {0} and response content: {1}", response.StatusCode, result);
                throw new InvalidOperationException(result);
            }

        }

        public bool ValidateInput(string email, string subject, string htmlMessage)
        {
            bool isValid = true;

            if (string.IsNullOrEmpty(_apiKey))
            {
                isValid = false;
                _logger.Error("SendGrid API key is empty");
            }

            if (string.IsNullOrEmpty(_fromEmail))
            {
                isValid = false;
                _logger.Error("SendGrid Email is empty");
            }

            if (string.IsNullOrEmpty(_fromName))
            {
                isValid = false;
                _logger.Error("SendGrid Name is empty");
            }

            if (string.IsNullOrEmpty(email))
            {
                isValid = false;
                _logger.Error("Email is empty");
            }

            if (string.IsNullOrEmpty(subject))
            {
                isValid = false;
                _logger.Error("Subject is empty");
            }

            if (string.IsNullOrEmpty(htmlMessage))
            {
                isValid = false;
                _logger.Error("HTML message is empty");
            }

            return isValid;
        }

        private static string ExtractTextFromHtml(string html)
        {
            // Remove HTML tags
            string plainText = Regex.Replace(html, "<[^>]+?>", " ");

            // Decode HTML entities
            plainText = WebUtility.HtmlDecode(plainText).Trim();
            return plainText;
        }
    }
}
