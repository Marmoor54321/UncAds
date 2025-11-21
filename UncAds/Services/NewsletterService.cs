using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Text;
using UncAds.Data;
using UncAds.Models;

namespace UncAds.Services
{
    public interface INewsletterService
    {
        Task SendNewsletterAsync();
    }

    public class NewsletterService : INewsletterService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender; // Wstrzykujemy to

        public NewsletterService(ApplicationDbContext context, IEmailSender emailSender)
        {
            _context = context;
            _emailSender = emailSender;
        }

        public async Task SendNewsletterAsync()
        {
            var usersWithSubs = await _context.Users
                .Include(u => u.CategorySubscriptions)
                .Where(u => u.CategorySubscriptions.Any())
                .ToListAsync();

            foreach (var user in usersWithSubs)
            {
                var dateFrom = user.LastNewsletterSent ?? DateTime.MinValue;
                var userCatIds = user.CategorySubscriptions.Select(s => s.CategoryId).ToList();

                var newAds = await _context.Ads
                    .Include(a => a.AdCategories)
                    .Where(a => a.Date > dateFrom)
                    .Where(a => a.AdCategories.Any(ac => userCatIds.Contains(ac.CategoryId)))
                    .OrderByDescending(a => a.Date)
                    .ToListAsync();

                if (newAds.Any())
                {
                    // Generujemy ładny HTML
                    string emailSubject = $"UncAds: Mamy dla Ciebie {newAds.Count} nowych ogłoszeń!";
                    string emailBody = GenerateHtmlEmailBody(user.DisplayName ?? "Użytkowniku", newAds);

                    // WYSYŁKA PRAWDZIWEGO MAILA
                    try
                    {
                        await _emailSender.SendEmailAsync(user.Email, emailSubject, emailBody);

                        // Aktualizujemy datę tylko jeśli wysyłka się udała
                        user.LastNewsletterSent = DateTime.Now;
                    }
                    catch (Exception ex)
                    {
                        // Logowanie błędu, ale nie przerywamy pętli dla innych użytkowników
                        Console.WriteLine($"Nie udało się wysłać do {user.Email}: {ex.Message}");
                    }
                }
            }

            await _context.SaveChangesAsync();
        }

        private string GenerateHtmlEmailBody(string userName, List<Ad> ads)
        {
            var sb = new StringBuilder();
            sb.Append($"<h3>Cześć {userName}!</h3>");
            sb.Append("<p>W kategoriach, które obserwujesz, pojawiły się nowe ogłoszenia:</p>");

            sb.Append("<table style='width:100%; border-collapse: collapse;'>");
            foreach (var ad in ads)
            {
                // Zakładam, że aplikacja stoi np. na localhost:5000. 
                // W produkcji powinieneś tu wstawić prawdziwy adres domeny.
                // Można go też pobrać z konfiguracji.
                string adLink = $"https://localhost:7234/Ad/Details/{ad.Id}";

                sb.Append("<tr>");
                sb.Append($"<td style='padding: 8px; border-bottom: 1px solid #ddd;'><strong>{ad.Title}</strong></td>");
                sb.Append($"<td style='padding: 8px; border-bottom: 1px solid #ddd;'>{ad.Date:dd.MM.yyyy}</td>");
                sb.Append($"<td style='padding: 8px; border-bottom: 1px solid #ddd;'><a href='{adLink}'>Zobacz</a></td>");
                sb.Append("</tr>");
            }
            sb.Append("</table>");
            sb.Append("<br/><p>Pozdrawiamy,<br/>Zespół UncAds</p>");

            return sb.ToString();
        }
    }
}