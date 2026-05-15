using System.Net;
using System.Net.Mail;

namespace ECommerceApp.Services
{
    public interface IEmailService
    {
        public Task SendEmailAsync(string ToEmail, string Subject, string Body, bool IsBodyHtml = false);
    }

    public class EmailService : IEmailService
    {
        // Configuration property to access application settings.
        private readonly IConfiguration _configuration;

        // Constructor that injects the configuration dependency.
        public EmailService(IConfiguration configuration)
        {
            // Save the configuration object for later use.
            _configuration = configuration;
        }

        // Method to send an email asynchronously.
        public Task SendEmailAsync(string ToEmail, string Subject, string Body, bool IsBodyHtml = true)
        {
            // Retrieve the mail server (SMTP host) from the configuration.
            string? MailServer = _configuration["EmailSettings:SmtpServer"];

            // Retrieve the sender email address from the configuration.
            string? SenderEmail = _configuration["EmailSettings:SenderEmail"];

            // Retrieve the sender email password from the configuration.
            string? Password = _configuration["EmailSettings:Password"];

            // Retrieve the sender's display name from the configuration.
            string? SenderName = _configuration["EmailSettings:SenderName"];

            // Retrieve the SMTP port number from the configuration and convert it to an integer.
            int Port = Convert.ToInt32(_configuration["EmailSettings:SmtpPort"]);

            // Create a new instance of SmtpClient using the mail server and port number.
            var client = new SmtpClient(MailServer, Port)
            {
                // Set the credentials (email and password) for the SMTP server.
                Credentials = new NetworkCredential(SenderEmail, Password),
                // Enable SSL for secure email communication.
                EnableSsl = true,
            };

            // Create a MailAddress object with the sender's email and display name.
            MailAddress senderAddress = new MailAddress(SenderEmail, SenderName);

            // Create a new MailMessage object to define the email's properties.
            MailMessage mailMessage = new MailMessage
            {
                From = senderAddress, // Set the sender's email address with display name.
                Subject = Subject, // Set the email subject line.
                Body = Body, // Set the email body content.
                IsBodyHtml = true // Specify whether the body content is in HTML format.
            };

            // Add the recipient's email address to the message.
            mailMessage.To.Add(ToEmail);

            // Send the email asynchronously using the SmtpClient instance.
            return client.SendMailAsync(mailMessage);
        }
    }
}
