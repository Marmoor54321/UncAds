using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UncAds.Data;
using UncAds.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;


namespace UncAds.Controllers
{
    public class AdsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public AdsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Ads
        public async Task<IActionResult> Index()
        {
            var ads = await _context.Ads
                .Include(a => a.User)
                .Include(a => a.AdCategories)
                    .ThenInclude(ac => ac.Category)
                .ToListAsync();

            return View(ads);
        }

        // GET: Ads/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var ad = await _context.Ads
                .Include(a => a.User)
                .Include(a => a.AdCategories)
                    .ThenInclude(ac => ac.Category)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (ad == null)
                return NotFound();

            // 🔽 Dociągamy pełne drzewo dla każdej kategorii
            foreach (var adCat in ad.AdCategories)
            {
                await LoadCategoryPath(adCat.Category);
            }

            return View(ad);
        }
        //Funkcja pomocnicza dla Details do wczytywania drzewa kategorii
        private async Task LoadCategoryPath(Category category)
        {
            if (category == null || category.ParentCategory != null)
                return;

            category.ParentCategory = await _context.Categories
                .Include(c => c.ParentCategory)
                .FirstOrDefaultAsync(c => c.Id == category.ParentCategoryId);

            if (category.ParentCategory != null)
                await LoadCategoryPath(category.ParentCategory);
        }

        // GET: Ads/Create
        public IActionResult Create()
        {
            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name");
            return View();
        }

        // POST: Ads/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Ad ad, int[] SelectedCategoryIds)
        {
            if (ModelState.IsValid)
            {
                ad.Date = DateTime.Now; // 🔒 nadpisanie dla bezpieczeństwa
                ad.UserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                _context.Ads.Add(ad);
                await _context.SaveChangesAsync();

                // 🔽 dodaj relacje z kategoriami
                if (SelectedCategoryIds != null && SelectedCategoryIds.Length > 0)
                {
                    foreach (var categoryId in SelectedCategoryIds)
                    {
                        var adCategory = new AdCategory
                        {
                            AdId = ad.Id,
                            CategoryId = categoryId
                        };
                        _context.AdCategories.Add(adCategory);
                    }
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name");
            return View(ad);
        }

        // GET: Ads/Edit/5
        [Authorize]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var ad = await _context.Ads
                .Include(a => a.AdCategories)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (ad == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");
            if (ad.UserId != user.Id && !isAdmin)
                return Forbid();

            // aktualnie wybrane kategorie
            var selectedIds = ad.AdCategories.Select(ac => ac.CategoryId).ToList();
            ViewBag.Categories = new MultiSelectList(_context.Categories, "Id", "Name", selectedIds);

            return View(ad);
        }

        // POST: Ads/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Ad ad, int[] SelectedCategoryIds)
        {
            if (id != ad.Id) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var existingAd = await _context.Ads
                .Include(a => a.AdCategories)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (existingAd == null) return NotFound();

            var isAdmin = User.IsInRole("Admin");
            if (existingAd.UserId != user.Id && !isAdmin)
                return Forbid();

            if (ModelState.IsValid)
            {
                existingAd.Title = ad.Title;
                existingAd.Description = ad.Description;

                // 🔄 aktualizacja kategorii
                existingAd.AdCategories.Clear();
                foreach (var catId in SelectedCategoryIds)
                {
                    existingAd.AdCategories.Add(new AdCategory
                    {
                        AdId = id,
                        CategoryId = catId
                    });
                }

                _context.Update(existingAd);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = new MultiSelectList(_context.Categories, "Id", "Name", SelectedCategoryIds);
            return View(ad);
        }


        // GET: Ads/Delete/5
        [Authorize]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var ad = await _context.Ads.Include(a => a.User).FirstOrDefaultAsync(m => m.Id == id);
            if (ad == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");
            if (ad.UserId != user.Id && !isAdmin)
                return Forbid();

            return View(ad);
        }

        [HttpPost, ActionName("Delete")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var ad = await _context.Ads
                .Include(a => a.AdCategories)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (ad == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");
            if (ad.UserId != user.Id && !isAdmin)
                return Forbid();

            // usuń powiązania
            _context.AdCategories.RemoveRange(ad.AdCategories);
            _context.Ads.Remove(ad);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private bool AdExists(int id)
        {
            return _context.Ads.Any(e => e.Id == id);
        }
    }
}
