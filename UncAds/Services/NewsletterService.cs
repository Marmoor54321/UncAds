using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using System.Text;
using UncAds.Data;
using UncAds.Models;
using Newtonsoft.Json;

namespace UncAds.Services
{
    public interface INewsletterService
    {
        Task SendNewsletterAsync();
    }

    public class NewsletterService : INewsletterService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender; // Teraz to jest oficjalny interfejs Microsoftu

        public NewsletterService(ApplicationDbContext context, IEmailSender emailSender)
        {
            _context = context;
            _emailSender = emailSender;
        }

        public async Task SendNewsletterAsync()
        {
            // 1. Get users with subscriptions
            var usersWithSubs = await _context.Users
                .Include(u => u.CategorySubscriptions)
                .Where(u => u.CategorySubscriptions.Any())
                .ToListAsync();

            foreach (var user in usersWithSubs)
            {
                var dateFrom = user.LastNewsletterSent ?? DateTime.MinValue;

                // We need all Ad Attributes to compare against User Filters
                // Warning: For high traffic, this query should be optimized (e.g., filtered by date first)
                var potentialAds = await _context.Ads
                    .Include(a => a.AdCategories)
                    .Include(a => a.AttributeValues) // Include Ad Values
                    .Where(a => a.Date > dateFrom)
                    .OrderByDescending(a => a.Date)
                    .ToListAsync();

                var adsToSend = new List<Ad>();

                foreach (var ad in potentialAds)
                {
                    // Does the user subscribe to any category this ad belongs to?
                    // And does the ad match the specific filters for that category?

                    bool isMatch = false;

                    foreach (var sub in user.CategorySubscriptions)
                    {
                        // Check if Ad is in this subscription's category
                        if (ad.AdCategories.Any(ac => ac.CategoryId == sub.CategoryId))
                        {
                            // If no filters defined, it's a match (basic subscription)
                            if (string.IsNullOrEmpty(sub.FiltersJson))
                            {
                                isMatch = true;
                                break;
                            }

                            // Deep Filter Check
                            var userFilters = JsonConvert.DeserializeObject<Dictionary<int, string>>(sub.FiltersJson);
                            if (AdMatchesFilters(ad, userFilters))
                            {
                                isMatch = true;
                                break;
                            }
                        }
                    }

                    if (isMatch)
                    {
                        adsToSend.Add(ad);
                    }
                }

                if (adsToSend.Any())
                {
                    string subject = $"UncAds: Wybrane dla Ciebie ({adsToSend.Count})";
                    string body = GenerateHtmlEmailBody(user.DisplayName, adsToSend);

                    try
                    {
                        await _emailSender.SendEmailAsync(user.Email, subject, body);
                        user.LastNewsletterSent = DateTime.Now;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error sending to {user.Email}: {ex.Message}");
                    }
                }
            }

            await _context.SaveChangesAsync();
        }
        private bool AdMatchesFilters(Ad ad, Dictionary<int, string> filters)
        {
            if (filters == null || !filters.Any()) return true;

            foreach (var filter in filters)
            {
                int attrId = filter.Key;
                string requiredValue = filter.Value.Trim().ToLower(); // Case-insensitive comparison

                // Find the value in the Ad
                var adValueObj = ad.AttributeValues?.FirstOrDefault(v => v.CategoryAttributeId == attrId);

                // If Ad doesn't have this attribute value but filter requires it -> mismatch
                if (adValueObj == null || string.IsNullOrEmpty(adValueObj.Value))
                    return false;

                string adValue = adValueObj.Value.Trim().ToLower();

                // Comparison Logic
                // For numbers/booleans/dictionaries, usually Exact Match is best
                // For text inputs, Contains might be better, but "exact" is safer for automated filters
                if (adValue != requiredValue)
                {
                    return false;
                }
            }

            return true; // All filters matched
        }
        private string GenerateHtmlEmailBody(string userName, List<Ad> ads)
        {
            var sb = new StringBuilder();
            sb.Append($"<h3>Cześć {userName}!</h3>");
            sb.Append("<p>W kategoriach, które obserwujesz, pojawiły się nowe ogłoszenia:</p>");

            sb.Append("<table style='width:100%; border-collapse: collapse;'>");
            foreach (var ad in ads)
            {
                // Link do ogłoszenia
                string adLink = $"https://localhost:7006/Ads/Details/{ad.Id}";

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