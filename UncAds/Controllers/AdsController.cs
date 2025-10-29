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
        // pomocnicza: pobiera atrybuty dla wybranych kategorii
        private List<CategoryAttribute> GetAttributesForCategories(int[] categoryIds)
        {
            var attributes = _context.CategoryAttributes
                .Where(a => categoryIds.Contains(a.CategoryId))
                .ToList();

            // jeśli w różnych kategoriach są takie same nazwy — eliminuj duplikaty
            return attributes
                .GroupBy(a => a.Name)
                .Select(g => g.First())
                .ToList();
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
                .Include(a => a.AttributeValues)
                    .ThenInclude(av => av.CategoryAttribute)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (ad == null)
                return NotFound();

            foreach (var adCat in ad.AdCategories)
                await LoadCategoryPath(adCat.Category);

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
        public async Task<IActionResult> Create(Ad ad, int[] SelectedCategoryIds, Dictionary<int, string> AttributeValues)
        {
            if (ModelState.IsValid)
            {
                ad.Date = DateTime.Now;
                ad.UserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                _context.Ads.Add(ad);
                await _context.SaveChangesAsync();

                // relacja z kategoriami
                foreach (var categoryId in SelectedCategoryIds)
                {
                    _context.AdCategories.Add(new AdCategory { AdId = ad.Id, CategoryId = categoryId });
                }
                await _context.SaveChangesAsync();

                // zapis wartości atrybutów
                if (AttributeValues != null)
                {
                    foreach (var kv in AttributeValues)
                    {
                        _context.AdAttributeValues.Add(new AdAttributeValue
                        {
                            AdId = ad.Id,
                            CategoryAttributeId = kv.Key,
                            Value = kv.Value
                        });
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
                .Include(a => a.AttributeValues)
                    .ThenInclude(av => av.CategoryAttribute)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (ad == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");
            if (ad.UserId != user.Id && !isAdmin)
                return Forbid();

            var selectedIds = ad.AdCategories.Select(ac => ac.CategoryId).ToList();
            ViewBag.Categories = new MultiSelectList(_context.Categories, "Id", "Name", selectedIds);

            // wczytaj wszystkie możliwe atrybuty
            var attributes = GetAttributesForCategories(selectedIds.ToArray());
            ViewBag.Attributes = attributes;

            return View(ad);
        }


        // POST: Ads/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Ad ad, int[] SelectedCategoryIds, Dictionary<int, string> AttributeValues)
        {
            if (id != ad.Id) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var existingAd = await _context.Ads
                .Include(a => a.AdCategories)
                .Include(a => a.AttributeValues)
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
                    existingAd.AdCategories.Add(new AdCategory { AdId = id, CategoryId = catId });

                // 🔄 aktualizacja wartości atrybutów
                _context.AdAttributeValues.RemoveRange(existingAd.AttributeValues);
                if (AttributeValues != null)
                {
                    foreach (var kv in AttributeValues)
                    {
                        _context.AdAttributeValues.Add(new AdAttributeValue
                        {
                            AdId = id,
                            CategoryAttributeId = kv.Key,
                            Value = kv.Value
                        });
                    }
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

        [HttpGet]
        public IActionResult GetCategoryAttributes([FromQuery] int[] categoryIds)
        {
            var attrs = GetAttributesForCategories(categoryIds)
                .Select(a => new
                {
                    a.Id,
                    a.Name,
                    a.Type,
                    a.Options
                }).ToList();

            return Json(attrs);
        }

    }
}
