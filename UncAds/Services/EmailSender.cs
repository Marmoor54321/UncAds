using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using UncAds.Configuration;

namespace UncAds.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly EmailSettings _emailSettings;

        public EmailSender(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // Konfiguracja klienta SMTP
            var client = new SmtpClient(_emailSettings.MailServer, _emailSettings.MailPort)
            {
                Credentials = new NetworkCredential(_emailSettings.SenderEmail, _emailSettings.Password),
                EnableSsl = true // Ważne dla Gmaila
            };

            // Tworzenie wiadomości
            var mailMessage = new MailMessage
            {
                From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true // Pozwala na używanie HTML w treści maila
            };

            mailMessage.To.Add(email);

            try
            {
                await client.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                // Tutaj możesz zalogować błąd, jeśli mail nie wyjdzie
                Console.WriteLine($"Błąd wysyłania maila: {ex.Message}");
                throw; // Rzucamy dalej, żeby serwis wywołujący wiedział o błędzie
            }
        }
    }
}