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
        [Route("feed")] 
        public async Task<IActionResult> Index()
        {
    
            var ads = await _context.Ads
                .Include(a => a.User) 
                .OrderByDescending(a => a.Date)
                .Take(20)
                .ToListAsync();

          
            var feedUrl = $"{Request.Scheme}://{Request.Host}/feed";
            var siteUrl = $"{Request.Scheme}://{Request.Host}";

            var feed = new SyndicationFeed(
                "UncAds - Najnowsze Ogłoszenia",    
                "Świeże ogłoszenia drobne z serwisu UncAds", 
                new Uri(siteUrl),              
                "UncAdsFeedID",                 
                DateTime.Now                 
            );

            feed.Copyright = new TextSyndicationContent($"Copyright {DateTime.Now.Year} UncAds");
            feed.Language = "pl-PL";

      
            var items = new List<SyndicationItem>();

            foreach (var ad in ads)
            {

                string adUrl = Url.Action("Details", "Ad", new { id = ad.Id }, Request.Scheme);

                var item = new SyndicationItem(
                    ad.Title,                      
                    ad.Description,                   
                    new Uri(adUrl),
                    ad.Id.ToString(),               
                    ad.Date                             
                );

               
                if (ad.User != null)
                {
                    item.Authors.Add(new SyndicationPerson(ad.User.Email, ad.User.DisplayName ?? ad.User.UserName, null));
                }

                items.Add(item);
            }

            feed.Items = items;

            var settings = new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,
                NewLineHandling = NewLineHandling.Entitize,
                Indent = true, 
                Async = true
            };

            using var stream = new MemoryStream();
            using var xmlWriter = XmlWriter.Create(stream, settings);

            var rssFormatter = new Rss20FeedFormatter(feed, false);
            rssFormatter.WriteTo(xmlWriter);
            await xmlWriter.FlushAsync();

            return File(stream.ToArray(), "application/rss+xml; charset=utf-8");
        }
    }
}