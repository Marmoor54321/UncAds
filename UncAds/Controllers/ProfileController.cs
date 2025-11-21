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
            // Pobieramy użytkownika wraz z jego subskrypcjami
            var user = await _userManager.Users
                .Include(u => u.CategorySubscriptions)
                .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

            if (user == null) return NotFound();

            // Przekazujemy wszystkie kategorie do widoku, żeby zbudować listę checkboxów
            ViewBag.AllCategories = await _context.Categories.ToListAsync();

            // Tworzymy listę ID kategorii, które użytkownik już subskrybuje
            ViewBag.SelectedCategoryIds = user.CategorySubscriptions?.Select(x => x.CategoryId).ToList() ?? new List<int>();

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(ApplicationUser model, int[] selectedCategories)
        {
            var user = await _userManager.Users
                .Include(u => u.CategorySubscriptions)
                .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

            if (user == null) return NotFound();

            // Aktualizacja standardowych pól
            user.DisplayName = model.DisplayName;
            user.Bio = model.Bio;
            user.AvatarUrl = model.AvatarUrl;
            user.AdsPerPage = model.AdsPerPage;

            // Aktualizacja subskrypcji
            // 1. Usuwamy stare subskrypcje
            var currentSubscriptions = user.CategorySubscriptions.ToList();
            _context.UserCategorySubscriptions.RemoveRange(currentSubscriptions);

            // 2. Dodajemy nowe wybrane
            if (selectedCategories != null)
            {
                foreach (var catId in selectedCategories)
                {
                    _context.UserCategorySubscriptions.Add(new UserCategorySubscription
                    {
                        UserId = user.Id,
                        CategoryId = catId
                    });
                }
            }

            // Zapisujemy zmiany w User i w tabeli łącznikowej
            await _userManager.UpdateAsync(user); // To aktualizuje usera
            await _context.SaveChangesAsync();    // To aktualizuje relacje w tabeli UserCategorySubscriptions

            return RedirectToAction("Index");
        }
    }
}