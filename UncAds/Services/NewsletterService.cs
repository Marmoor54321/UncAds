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
        // Tutaj wstrzyknąłbyś też IEmailSender, gdybyś miał prawdziwą wysyłkę

        public NewsletterService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task SendNewsletterAsync()
        {
            // 1. Pobierz użytkowników, którzy mają jakiekolwiek subskrypcje
            var usersWithSubs = await _context.Users
                .Include(u => u.CategorySubscriptions)
                .Where(u => u.CategorySubscriptions.Any())
                .ToListAsync();

            foreach (var user in usersWithSubs)
            {
                // Data od której szukamy ogłoszeń (od ostatniej wysyłki lub od zawsze jeśli null)
                var dateFrom = user.LastNewsletterSent ?? DateTime.MinValue;

                // Lista ID kategorii subskrybowanych przez usera
                var userCatIds = user.CategorySubscriptions.Select(s => s.CategoryId).ToList();

                // 2. Znajdź nowe ogłoszenia
                // Ogłoszenie jest "nowe", jeśli powstało po 'dateFrom'
                // I należy do jednej z subskrybowanych kategorii
                var newAds = await _context.Ads
                    .Include(a => a.AdCategories)
                    .Where(a => a.Date > dateFrom)
                    .Where(a => a.AdCategories.Any(ac => userCatIds.Contains(ac.CategoryId)))
                    .OrderByDescending(a => a.Date)
                    .ToListAsync();

                if (newAds.Any())
                {
                    // 3. Symulacja wysłania e-maila
                    string emailContent = GenerateEmailBody(user.DisplayName, newAds);

                    // TODO: Tutaj wywołanie np. _emailSender.SendEmailAsync(user.Email, "Nowe ogłoszenia", emailContent);
                    Console.WriteLine($"[NEWSLETTER] Wysyłanie do {user.Email}. Liczba ogłoszeń: {newAds.Count}");
                    Debug.WriteLine($"[NEWSLETTER] Wysyłanie do {user.Email}. Liczba ogłoszeń: {newAds.Count}");

                    // 4. Zaktualizuj datę ostatniej wysyłki
                    user.LastNewsletterSent = DateTime.Now;
                }
            }

            await _context.SaveChangesAsync();
        }

        private string GenerateEmailBody(string userName, List<Ad> ads)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Cześć {userName}, oto nowe ogłoszenia w Twoich kategoriach:");
            foreach (var ad in ads)
            {
                sb.AppendLine($"- {ad.Title} ({ad.Date.ToShortDateString()})");
                // Tutaj można dodać link do ogłoszenia
            }
            return sb.ToString();
        }
    }
}