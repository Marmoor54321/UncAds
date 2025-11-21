using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ServiceModel.Syndication;
using System.Text;
using System.Xml;
using UncAds.Data;

namespace UncAds.Controllers
{
    public class FeedController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FeedController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Route("feed")] // Adres będzie wyglądał tak: twojadomena.pl/feed
        public async Task<IActionResult> Index()
        {
            // 1. Pobierz 20 najnowszych ogłoszeń
            var ads = await _context.Ads
                .Include(a => a.User) // Opcjonalnie, jeśli chcesz dodać autora
                .OrderByDescending(a => a.Date)
                .Take(20)
                .ToListAsync();

            // 2. Przygotuj podstawowe informacje o kanale
            // Request.Scheme + "://" + Request.Host tworzy pełny adres URL (np. https://localhost:5000)
            var feedUrl = $"{Request.Scheme}://{Request.Host}/feed";
            var siteUrl = $"{Request.Scheme}://{Request.Host}";

            var feed = new SyndicationFeed(
                "UncAds - Najnowsze Ogłoszenia",     // Tytuł kanału
                "Świeże ogłoszenia drobne z serwisu UncAds", // Opis
                new Uri(siteUrl),                   // Link do strony głównej
                "UncAdsFeedID",                     // ID kanału
                DateTime.Now                        // Data ostatniej aktualizacji
            );

            feed.Copyright = new TextSyndicationContent($"Copyright {DateTime.Now.Year} UncAds");
            feed.Language = "pl-PL";

            // 3. Konwersja ogłoszeń na elementy RSS (SyndicationItem)
            var items = new List<SyndicationItem>();

            foreach (var ad in ads)
            {
                // Generowanie pełnego linku do ogłoszenia
                // Ważne: Url.Action generuje link relatywny, musimy go zmienić na absolutny
                string adUrl = Url.Action("Details", "Ad", new { id = ad.Id }, Request.Scheme);

                var item = new SyndicationItem(
                    ad.Title,                           // Tytuł wpisu
                    ad.Description,                     // Treść (opis)
                    new Uri(adUrl),                     // Link do wpisu
                    ad.Id.ToString(),                   // Unikalne ID wpisu
                    ad.Date                             // Data publikacji
                );

                // Opcjonalnie: Dodanie autora
                if (ad.User != null)
                {
                    item.Authors.Add(new SyndicationPerson(ad.User.Email, ad.User.DisplayName ?? ad.User.UserName, null));
                }

                items.Add(item);
            }

            feed.Items = items;

            // 4. Serializacja do XML (RSS 2.0)
            var settings = new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,
                NewLineHandling = NewLineHandling.Entitize,
                Indent = true, // Ładne formatowanie XML
                Async = true
            };

            using var stream = new MemoryStream();
            using var xmlWriter = XmlWriter.Create(stream, settings);

            var rssFormatter = new Rss20FeedFormatter(feed, false);
            rssFormatter.WriteTo(xmlWriter);
            await xmlWriter.FlushAsync();

            // Zwracamy XML jako plik/tekst o odpowiednim typie MIME
            return File(stream.ToArray(), "application/rss+xml; charset=utf-8");
        }
    }
}