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
        private readonly IEmailSender _emailSender; 

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

               
                var potentialAds = await _context.Ads
                    .Include(a => a.AdCategories)
                    .Include(a => a.AttributeValues) 
                    .Where(a => a.Date > dateFrom)
                    .OrderByDescending(a => a.Date)
                    .ToListAsync();

                var adsToSend = new List<Ad>();

                foreach (var ad in potentialAds)
                {
                  

                    bool isMatch = false;

                    foreach (var sub in user.CategorySubscriptions)
                    {
                       
                        if (ad.AdCategories.Any(ac => ac.CategoryId == sub.CategoryId))
                        {
                           
                            if (string.IsNullOrEmpty(sub.FiltersJson))
                            {
                                isMatch = true;
                                break;
                            }

                           
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
                string requiredValue = filter.Value.Trim().ToLower(); 

             
                var adValueObj = ad.AttributeValues?.FirstOrDefault(v => v.CategoryAttributeId == attrId);

                
                if (adValueObj == null || string.IsNullOrEmpty(adValueObj.Value))
                    return false;

                string adValue = adValueObj.Value.Trim().ToLower();

                
                if (adValue != requiredValue)
                {
                    return false;
                }
            }

            return true; 
        }
        private string GenerateHtmlEmailBody(string userName, List<Ad> ads)
        {
            var sb = new StringBuilder();
            sb.Append($"<h3>Cześć {userName}!</h3>");
            sb.Append("<p>W kategoriach, które obserwujesz, pojawiły się nowe ogłoszenia:</p>");

            sb.Append("<table style='width:100%; border-collapse: collapse;'>");
            foreach (var ad in ads)
            {
                
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