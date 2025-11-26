using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Potrzebne do Include
using UncAds.Data; // Twój namespace do DbContext
using UncAds.Models;

namespace UncAds.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context; // Dodajemy kontekst

        public ProfileController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            return View(user);
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var user = await _userManager.Users
                .Include(u => u.CategorySubscriptions)
                .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

            if (user == null) return NotFound();

            // Load categories WITH attributes and dictionary values for the form
            ViewBag.AllCategories = await _context.Categories
                .Include(c => c.CategoryAttributes)
                    .ThenInclude(a => a.Dictionary)
                        .ThenInclude(d => d.Values)
                .ToListAsync();

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(
            ApplicationUser model,
            int[] selectedCategories,
            // Binding: Key=CategoryId, Value=(Key=AttrId, Value=UserInputValue)
            Dictionary<int, Dictionary<int, string>> Filters)
        {
            var user = await _userManager.Users
                .Include(u => u.CategorySubscriptions)
                .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

            if (user == null) return NotFound();

            user.DisplayName = model.DisplayName;
            user.Bio = model.Bio; 
            user.AdsPerPage = model.AdsPerPage; 
            user.AvatarUrl = model.AvatarUrl;

            // 1. Clear old subscriptions
            var currentSubscriptions = user.CategorySubscriptions.ToList();
            _context.UserCategorySubscriptions.RemoveRange(currentSubscriptions);

            // 2. Add new subscriptions with filters
            if (selectedCategories != null)
            {
                foreach (var catId in selectedCategories)
                {
                    string jsonFilters = null;

                    // Check if there are filters for this specific category
                    if (Filters.ContainsKey(catId))
                    {
                        // Remove empty values to save space
                        var activeFilters = Filters[catId]
                            .Where(kvp => !string.IsNullOrWhiteSpace(kvp.Value))
                            .ToDictionary(k => k.Key, v => v.Value);

                        if (activeFilters.Any())
                        {
                            // Serialize using Newtonsoft.Json or System.Text.Json
                            jsonFilters = Newtonsoft.Json.JsonConvert.SerializeObject(activeFilters);
                        }
                    }

                    _context.UserCategorySubscriptions.Add(new UserCategorySubscription
                    {
                        UserId = user.Id,
                        CategoryId = catId,
                        FiltersJson = jsonFilters // Save the JSON
                    });
                }
            }

            await _userManager.UpdateAsync(user);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }
    }
}