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
using UncAds.Services;


namespace UncAds.Controllers
{
    public class AdsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHtmlSanitizationService _htmlSanitizer;

        private readonly string[] _allowedMediaExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".mp3", ".wav", ".ogg", ".swf",".mp4" };
        private readonly string[] _allowedAttachmentExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".mp3", ".wav", ".ogg", ".swf", ".mp4", ".avi", ".pdf", ".doc", ".docx", ".zip", ".rar", ".txt", ".csv" };

        public AdsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IHtmlSanitizationService htmlSanitizer)
        {
            _context = context;
            _userManager = userManager;
            _htmlSanitizer = htmlSanitizer;
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
        public async Task<IActionResult> Index(string query, int? categoryId, Dictionary<int, string> attributeFilters, int page = 1)
        {
            // 1. Pobieramy kategorie (do nawigacji i dropdowna)
            var allCats = await _context.Categories
                .Include(c => c.ParentCategory)
                .OrderBy(c => c.Name)
                .ToListAsync();

            // 2. Obsługa Dropdowna (Logika z poprzedniego kroku - naprawa "znikającej" kategorii)
            var dropdownSource = allCats.Where(c => c.ParentCategoryId == null).ToList();
            if (categoryId.HasValue)
            {
                var currentSelected = allCats.FirstOrDefault(c => c.Id == categoryId.Value);
                if (currentSelected != null && !dropdownSource.Any(c => c.Id == currentSelected.Id))
                {
                    dropdownSource.Insert(0, new Category { Id = currentSelected.Id, Name = $"» {currentSelected.Name}" });
                }
            }
            ViewBag.SearchCategories = new SelectList(dropdownSource, "Id", "Name", categoryId);

            // 3. LOGIKA WYŚWIETLANIA: KAFELKI VS ATRYBUTY
            List<Category> categoriesToDisplay = new List<Category>();
            Category currentCategoryObj = null;
            bool showAttributes = false;
            List<CategoryAttribute> attributesToDisplay = new List<CategoryAttribute>();

            if (categoryId.HasValue)
            {
                // Pobierz aktualną kategorię wraz z jej definicjami atrybutów i słownikami
                currentCategoryObj = await _context.Categories
                    .Include(c => c.CategoryAttributes)
                        .ThenInclude(ca => ca.Dictionary)
                            .ThenInclude(d => d.Values)
                    .FirstOrDefaultAsync(c => c.Id == categoryId.Value);

                // SPRAWDZENIE: Czy kategoria ma atrybuty?
                if (currentCategoryObj != null && currentCategoryObj.CategoryAttributes != null && currentCategoryObj.CategoryAttributes.Any())
                {
                    // WARUNEK ZREALIZOWANY: Ma atrybuty -> wyświetlamy atrybuty zamiast kafelków
                    showAttributes = true;
                    attributesToDisplay = currentCategoryObj.CategoryAttributes.ToList();
                }
                else
                {
                    // Nie ma atrybutów -> sprawdzamy dzieci (stara logika)
                    var children = allCats.Where(c => c.ParentCategoryId == categoryId.Value).ToList();
                    if (children.Any())
                    {
                        categoriesToDisplay = children;
                    }
                    else
                    {
                        // Brak dzieci i brak atrybutów -> pokaż rodzeństwo
                        if (currentCategoryObj?.ParentCategoryId != null)
                        {
                            categoriesToDisplay = allCats
                                .Where(c => c.ParentCategoryId == currentCategoryObj.ParentCategoryId.Value)
                                .ToList();
                        }
                        else
                        {
                            categoriesToDisplay = allCats.Where(c => c.ParentCategoryId == null).ToList();
                        }
                    }
                }
            }
            else
            {
                // Strona główna
                categoriesToDisplay = allCats.Where(c => c.ParentCategoryId == null).ToList();
            }

            ViewBag.DisplayCategories = categoriesToDisplay;
            ViewBag.CurrentCategory = currentCategoryObj;

            // Nowe ViewBagi do obsługi atrybutów
            ViewBag.ShowAttributes = showAttributes;
            ViewBag.AttributesToDisplay = attributesToDisplay;
            ViewBag.AttributeFilters = attributeFilters; // Żeby wypełnić formularz po przeładowaniu

            // 4. Budowanie zapytania ADS
            var adsQuery = _context.Ads
                .Include(a => a.User)
                .Include(a => a.Media)
                .Include(a => a.AdCategories)
                    .ThenInclude(ac => ac.Category)
                .Include(a => a.AttributeValues) // Ważne dla filtrowania
                .AsQueryable();

            // Filtrowanie po kategorii
            if (categoryId.HasValue)
            {
                var categoryIdsToSearch = await GetCategoryAndChildrenIdsAsync(categoryId.Value);
                adsQuery = adsQuery.Where(a => a.AdCategories.Any(ac => categoryIdsToSearch.Contains(ac.CategoryId)));
            }

            // --- NOWOŚĆ: FILTROWANIE PO ATRYBUTACH ---
            if (attributeFilters != null && attributeFilters.Any())
            {
                foreach (var filter in attributeFilters)
                {
                    int attrId = filter.Key;
                    string filterValue = filter.Value;

                    if (string.IsNullOrWhiteSpace(filterValue)) continue;

                    // Dla każdego filtra dodajemy warunek "WHERE", że ogłoszenie musi mieć atrybut o danym ID i wartości
                    // Uwaga: To jest proste porównanie stringów. Dla liczb/dat można to rozbudować o zakresy.
                    adsQuery = adsQuery.Where(ad => ad.AttributeValues.Any(av =>
                        av.CategoryAttributeId == attrId && av.Value == filterValue));
                }
            }
            // ------------------------------------------

            var ads = await adsQuery.ToListAsync();

            // Filtrowanie po tekście (AdSearch)
            if (!string.IsNullOrWhiteSpace(query))
            {
                ads = ads.Where(ad => AdSearch.Matches(ad, query.Trim())).ToList();
            }

            // Sortowanie, Paginacja
            ads = ads.OrderByDescending(a => a.Date).ToList();
            var user = await _userManager.GetUserAsync(User);
            int pageSize = user?.AdsPerPage > 0 ? user.AdsPerPage : 10;

            var pagedAds = ads.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)ads.Count / pageSize);
            ViewBag.Query = query;
            ViewBag.SelectedCategoryId = categoryId;

            return View(pagedAds);
        }
        private async Task<List<int>> GetCategoryAndChildrenIdsAsync(int categoryId)
        {
            var allCategories = await _context.Categories.ToListAsync(); // Pobieramy do pamięci, aby szybko przetworzyć drzewo
            var resultIds = new List<int> { categoryId };

            void AddChildren(int parentId)
            {
                var children = allCategories.Where(c => c.ParentCategoryId == parentId).Select(c => c.Id).ToList();
                foreach (var childId in children)
                {
                    resultIds.Add(childId);
                    AddChildren(childId);
                }
            }

            AddChildren(categoryId);
            return resultIds;
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

            // Zwiększ licznik wyświetleń
            ad.ViewCount++;
            await _context.SaveChangesAsync();

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

            var settings = _context.AdminSettings.FirstOrDefault() ?? new AdminSettings();
            ViewBag.MaxAttachments = settings.MaxAttachments;
            ViewBag.MaxFileSizeMB = settings.MaxFileSizeMB;
            ViewBag.MaxMediaFiles = settings.MaxMediaFiles; // Nowe!
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
            Dictionary<int, string[]> AttributeValues,
            List<IFormFile> mediaFiles,
            List<IFormFile> attachmentFiles,
            List<string> attachmentDescriptions)
        {
            // Pobranie ustawień admina
            var settings = await _context.AdminSettings.FirstOrDefaultAsync() ?? new AdminSettings();

            if (attachmentFiles != null)
            {
                if (attachmentFiles.Count > settings.MaxAttachments)
                {
                    ModelState.AddModelError("", $"Maksymalna liczba załączników to {settings.MaxAttachments}.");
                }
                for (int i = 0; i < attachmentFiles.Count; i++)
                {
                    var file = attachmentFiles[i];
                    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (!_allowedAttachmentExtensions.Contains(ext))
                    {
                        ModelState.AddModelError("", $"Nieobsługiwany typ pliku dla załączników: {file.FileName}. Dozwolone: obrazy, dźwięki, video, pdf, doc/docx, zip/rar, txt/csv itp.");
                    }
                    if (file.Length > settings.MaxFileSizeMB * 1024 * 1024)
                    {
                        ModelState.AddModelError("", $"Plik {file.FileName} przekracza limit {settings.MaxFileSizeMB} MB.");
                    }
                }
            }
            if (mediaFiles != null)
            {
                if (mediaFiles.Count > settings.MaxMediaFiles)
                {
                    ModelState.AddModelError("", $"Maksymalna liczba plików multimedialnych to {settings.MaxMediaFiles}.");
                }
                foreach (var file in mediaFiles)
                {
                    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

                    // 1. Typ pliku
                    if (!_allowedMediaExtensions.Contains(ext))
                    {
                        ModelState.AddModelError("", $"Nieobsługiwany typ pliku dla multimediów: {file.FileName}. Dozwolone: jpg, png, gif, mp3, wav, ogg, swf.");
                        continue;
                    }

                    // 2. Rozmiar
                    if (file.Length > settings.MaxFileSizeMB * 1024 * 1024)
                    {
                        ModelState.AddModelError("", $"Plik multimedialny {file.FileName} przekracza limit {settings.MaxFileSizeMB} MB.");
                        continue;
                    }

                    // 3. Pusty plik
                    if (file.Length == 0)
                    {
                        ModelState.AddModelError("", $"Plik {file.FileName} jest pusty.");
                        continue;
                    }
                }
            }

            // sprawdzenie czy ogloszenie nie ma zakazanych slow
            var forbidden = await _context.ForbiddenWords
                .Select(f => f.Word.ToLower())
                .ToListAsync();

            string textToCheck = $"{ad.Title} {ad.Description}".ToLower();

            foreach (var word in forbidden)
            {
                if (!string.IsNullOrWhiteSpace(word) && textToCheck.Contains(word))
                {
                    ModelState.AddModelError("",
                        $"Ogłoszenie zawiera zakazane słowo: '{word}'. Nie można go dodać.");

                    ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name");
                    return View(ad);
                }
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name");
                return View(ad);
            }

            // Zapis ogłoszenia
            ad.Date = DateTime.Now;
            ad.UserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            ad.Description = _htmlSanitizer.Sanitize(ad.Description);

            _context.Ads.Add(ad);
            await _context.SaveChangesAsync();  

            // Kategorie
            foreach (var catId in SelectedCategoryIds)
                _context.AdCategories.Add(new AdCategory { AdId = ad.Id, CategoryId = catId });
            await _context.SaveChangesAsync();

            // Atrybuty
            if (AttributeValues != null && AttributeValues.Any())
            {
                foreach (var kv in AttributeValues)
                {
                    var attributeId = kv.Key;
                    var valuesArray = kv.Value; // To jest teraz tablica string[]

                    if (valuesArray == null) continue;

                    // Iterujemy po każdej wybranej wartości dla danego atrybutu
                    foreach (var singleValue in valuesArray)
                    {
                        // Pomiń puste
                        if (string.IsNullOrWhiteSpace(singleValue)) continue;

                        try
                        {
                            _context.AdAttributeValues.Add(new AdAttributeValue
                            {
                                AdId = ad.Id,
                                CategoryAttributeId = attributeId,
                                Value = singleValue // Zapisujemy konkretną wartość
                            });
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Błąd przy dodawaniu atrybutu {attributeId}: {ex.Message}");
                        }
                    }
                }
                // Zapisujemy wszystko raz po pętli
                await _context.SaveChangesAsync();
            }

            // Multimedia
            if (mediaFiles != null && mediaFiles.Any())
            {
                var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "ads", ad.Id.ToString());
                Directory.CreateDirectory(uploadDir);

                foreach (var file in mediaFiles)
                {
                    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

                    // PONOWNA walidacja (na wszelki wypadek)
                    if (!_allowedMediaExtensions.Contains(ext) ||
                        file.Length > settings.MaxFileSizeMB * 1024 * 1024 ||
                        file.Length == 0)
                    {
                        continue; // pomiń zapis
                    }

                    string mediaType = ext switch
                    {
                        ".jpg" or ".jpeg" or ".png" or ".gif" => "image",
                        ".mp3" or ".wav" or ".ogg" => "audio",
                        ".mp4" => "video",
                        ".swf" => "flash",
                        _ => "other"
                    };

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
                    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

                    // Pomijaj niepoprawne (już sprawdzone w walidacji)
                    if (!_allowedAttachmentExtensions.Contains(ext) ||
                        file.Length > settings.MaxFileSizeMB * 1024 * 1024 ||
                        file.Length == 0)
                    {
                        continue;
                    }

                    var fileName = Guid.NewGuid().ToString() + ext;
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

            //sprawdzenie czy ogloszenie nie ma slow zakazanych
            var forbidden = await _context.ForbiddenWords
                .Select(f => f.Word.ToLower())
                .ToListAsync();

            string textToCheck = $"{ad.Title} {ad.Description}".ToLower();

            foreach (var word in forbidden)
            {
                if (textToCheck.Contains(word))
                {
                    ModelState.AddModelError("", $"Ogłoszenie zawiera zakazane słowo: '{word}'.");

                    var selectedIds = SelectedCategoryIds?.ToList() ?? new List<int>();
                    ViewBag.Categories = new MultiSelectList(_context.Categories, "Id", "Name", selectedIds);

                    var attributes = GetAttributesForCategories(selectedIds.ToArray());
                    ViewBag.Attributes = attributes;

                    return View(ad);
                }
            }


            if (ModelState.IsValid)
            {
                existingAd.Title = ad.Title;
                existingAd.Description = _htmlSanitizer.Sanitize(ad.Description);

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
                        if (string.IsNullOrWhiteSpace(kv.Value))
                            continue;

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
                .Include(a => a.Media)
                .Include(a => a.Attachments)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (ad == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");
            if (ad.UserId != user.Id && !isAdmin)
                return Forbid();

            _context.AdCategories.RemoveRange(ad.AdCategories);

            if (ad.Media != null)
            {
                foreach (var media in ad.Media)
                {
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", media.FilePath.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }
                _context.AdMedia.RemoveRange(ad.Media);
            }

            if (ad.Attachments != null)
            {
                foreach (var attach in ad.Attachments)
                {
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", attach.FilePath.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }
                _context.AdAttachments.RemoveRange(ad.Attachments);
            }

            // usuń ogłoszenie
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
            var attrs = _context.CategoryAttributes
            .Include(a => a.Dictionary)
            .ThenInclude(d => d.Values)
            .Where(a => categoryIds.Contains(a.CategoryId))
            .Select(a => new
            {
                a.Id,
                a.Name,
                a.Type,
                a.Options,
                a.AllowMultiple,
                DictionaryValues = a.Dictionary != null
                    ? a.Dictionary.Values.Select(v => v.Value).ToList()
                    : null
            })
            .ToList();

            return Json(attrs);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Report(int id, string reason)
        {
            var ad = await _context.Ads.FindAsync(id);
            if (ad == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);

            var report = new AdReport
            {
                AdId = id,
                ReporterId = user.Id,
                Reason = reason
            };

            _context.AdReports.Add(report);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Dziękujemy. Zgłoszenie zostało przekazane moderatorowi.";
            return RedirectToAction("Details", new { id });
        }


    }
}
