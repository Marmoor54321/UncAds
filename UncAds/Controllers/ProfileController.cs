using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UncAds.Data; 
using UncAds.Models;

namespace UncAds.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context; 

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

           
            var currentSubscriptions = user.CategorySubscriptions.ToList();
            _context.UserCategorySubscriptions.RemoveRange(currentSubscriptions);

            // nowe subskrypcje
            if (selectedCategories != null)
            {
                foreach (var catId in selectedCategories)
                {
                    string jsonFilters = null;

                    if (Filters.ContainsKey(catId))
                    {
                        
                        var activeFilters = Filters[catId]
                            .Where(kvp => !string.IsNullOrWhiteSpace(kvp.Value))
                            .ToDictionary(k => k.Key, v => v.Value);

                        if (activeFilters.Any())
                        {
                          
                            jsonFilters = Newtonsoft.Json.JsonConvert.SerializeObject(activeFilters);
                        }
                    }

                    _context.UserCategorySubscriptions.Add(new UserCategorySubscription
                    {
                        UserId = user.Id,
                        CategoryId = catId,
                        FiltersJson = jsonFilters 
                    });
                }
            }

            await _userManager.UpdateAsync(user);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }
    }
}