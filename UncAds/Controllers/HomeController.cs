using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using UncAds.Models;
using UncAds.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Localization;

namespace UncAds.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var settings = await _context.AdminSettings.FirstOrDefaultAsync();
            ViewData["HomepageMessage"] = settings?.HomePageMessage;

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
        public IActionResult Ads()
        {
            return View();
        }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost]
        public IActionResult SetTheme(string theme, string returnUrl)
        {
            Response.Cookies.Append("Theme", theme, new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) });

            if (!string.IsNullOrEmpty(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }
            return RedirectToAction("Index");
        }
        [HttpPost]
        public IActionResult SetLanguage(string culture, string returnUrl)
        {
            if (string.IsNullOrEmpty(culture))
            {
                return LocalRedirect(returnUrl ?? "/");
            }

            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    IsEssential = true,
                    HttpOnly = false
                }
            );

            return LocalRedirect(returnUrl ?? "/");
        }
    }
}
