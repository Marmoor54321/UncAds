using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UncAds.Data;
using UncAds.Models;

namespace UncAds.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AttributeDictionariesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AttributeDictionariesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var dicts = await _context.AttributeDictionaries.Include(d => d.Values).ToListAsync();
            return View(dicts);
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AttributeDictionary dictionary)
        {
            if (!ModelState.IsValid) return View(dictionary);

            _context.Add(dictionary);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var dict = await _context.AttributeDictionaries.Include(d => d.Values).FirstOrDefaultAsync(d => d.Id == id);
            if (dict == null) return NotFound();
            return View(dict);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AttributeDictionary dictionary)
        {
            // 1. Walidacja podstawowa
            if (!ModelState.IsValid) return View(dictionary);

            // 2. Pobierz encję z bazy danych WRAZ z wartościami (Include)
            var dbDictionary = await _context.AttributeDictionaries
                .Include(d => d.Values)
                .FirstOrDefaultAsync(d => d.Id == dictionary.Id);

            if (dbDictionary == null)
            {
                return NotFound();
            }

            // 3. Aktualizuj proste pola
            dbDictionary.Name = dictionary.Name;

            // 4. Synchronizacja kolekcji Values (Add / Update / Delete)

            // A. Usuwanie: Znajdź te, które są w bazie, a nie ma ich w formularzu
            // (zakładamy, że dictionary.Values zawiera to, co przyszło z formularza)
            var currentIds = dictionary.Values.Select(v => v.Id).ToList();
            var valuesToDelete = dbDictionary.Values
                .Where(v => !currentIds.Contains(v.Id))
                .ToList();

            foreach (var value in valuesToDelete)
            {
                _context.Remove(value);
            }

            // B. Dodawanie i Aktualizacja
            foreach (var formValue in dictionary.Values)
            {
                // Jeśli ID = 0, to nowa wartość -> Dodaj do kolekcji rodzica
                if (formValue.Id == 0)
                {
                    var newValue = new AttributeDictionaryValue
                    {
                        Value = formValue.Value,
                        // Nie musisz ustawiać AttributeDictionaryId, EF zrobi to sam dodając do kolekcji
                    };
                    dbDictionary.Values.Add(newValue);
                }
                else
                {
                    // Jeśli ID > 0, to edycja -> Znajdź w załadowanej kolekcji i zaktualizuj
                    var existingValue = dbDictionary.Values
                        .FirstOrDefault(v => v.Id == formValue.Id);

                    if (existingValue != null)
                    {
                        existingValue.Value = formValue.Value;
                    }
                }
            }

            // 5. Zapisz zmiany
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AttributeDictionaryExists(dictionary.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToAction(nameof(Index));
        }

        // Metoda pomocnicza (jeśli jej nie masz)
        private bool AttributeDictionaryExists(int id)
        {
            return _context.AttributeDictionaries.Any(e => e.Id == id);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var dict = await _context.AttributeDictionaries.FindAsync(id);
            if (dict == null) return NotFound();
            _context.Remove(dict);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
