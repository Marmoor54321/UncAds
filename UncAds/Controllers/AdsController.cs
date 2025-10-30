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
                .Include(a => a.Media)
                .Include(a => a.Attachments)
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
                .Include(a => a.Media)
                .Include(a => a.Attachments)
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
        public async Task<IActionResult> Create(
    Ad ad,
    int[] SelectedCategoryIds,
    Dictionary<int, string> AttributeValues,
    List<IFormFile> mediaFiles,
    List<IFormFile> attachmentFiles,
    List<string> attachmentDescriptions)
        {
            // Pobranie ustawień admina
            var settings = await _context.AdminSettings.FirstOrDefaultAsync() ?? new AdminSettings();

            // WALIDACJA ZAŁĄCZNIKÓW PRZED ZAPISANIEM
            if (attachmentFiles != null && attachmentFiles.Count > settings.MaxAttachments)
            {
                ModelState.AddModelError("", $"Maksymalna liczba załączników to {settings.MaxAttachments}.");
            }

            if (attachmentFiles != null)
            {
                for (int i = 0; i < attachmentFiles.Count; i++)
                {
                    if (attachmentFiles[i].Length > settings.MaxFileSizeMB * 1024 * 1024)
                    {
                        ModelState.AddModelError("", $"Plik {attachmentFiles[i].FileName} przekracza limit {settings.MaxFileSizeMB} MB.");
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name");
                return View(ad); // <-- brak zapisu do bazy jeśli walidacja nie przeszła
            }

            // Zapis ogłoszenia
            ad.Date = DateTime.Now;
            ad.UserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            _context.Ads.Add(ad);
            await _context.SaveChangesAsync();

            // Kategorie
            foreach (var catId in SelectedCategoryIds)
                _context.AdCategories.Add(new AdCategory { AdId = ad.Id, CategoryId = catId });
            await _context.SaveChangesAsync();

            // Atrybuty
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

            // Multimedia
            if (mediaFiles != null && mediaFiles.Any())
            {
                var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "ads", ad.Id.ToString());
                Directory.CreateDirectory(uploadDir);

                foreach (var file in mediaFiles)
                {
                    if (file.Length > 0)
                    {
                        var ext = Path.GetExtension(file.FileName).ToLower();
                        string mediaType = "other";
                        if (ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".gif")
                            mediaType = "image";
                        else if (ext == ".mp3" || ext == ".wav" || ext == ".ogg")
                            mediaType = "audio";
                        else if (ext == ".swf")
                            mediaType = "flash";

                        var fileName = Guid.NewGuid().ToString() + ext;
                        var filePath = Path.Combine(uploadDir, fileName);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                            await file.CopyToAsync(stream);

                        var relativePath = $"/uploads/ads/{ad.Id}/{fileName}";
                        _context.AdMedia.Add(new AdMedia
                        {
                            AdId = ad.Id,
                            FilePath = relativePath,
                            MediaType = mediaType
                        });
                    }
                }
                await _context.SaveChangesAsync();
            }

            // Załączniki
            if (attachmentFiles != null && attachmentFiles.Any())
            {
                var attachDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "attachments", ad.Id.ToString());
                Directory.CreateDirectory(attachDir);

                for (int i = 0; i < attachmentFiles.Count; i++)
                {
                    var file = attachmentFiles[i];
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    var filePath = Path.Combine(attachDir, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                        await file.CopyToAsync(stream);

                    var relativePath = $"/uploads/attachments/{ad.Id}/{fileName}";
                    string? desc = i < attachmentDescriptions.Count ? attachmentDescriptions[i] : null;

                    _context.AdAttachments.Add(new AdAttachment
                    {
                        AdId = ad.Id,
                        FilePath = relativePath,
                        Description = desc
                    });
                }

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
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
