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
  
            if (!ModelState.IsValid) return View(dictionary);

            var dbDictionary = await _context.AttributeDictionaries
                .Include(d => d.Values)
                .FirstOrDefaultAsync(d => d.Id == dictionary.Id);

            if (dbDictionary == null)
            {
                return NotFound();
            }

           
            dbDictionary.Name = dictionary.Name;

         
            var currentIds = dictionary.Values.Select(v => v.Id).ToList();
            var valuesToDelete = dbDictionary.Values
                .Where(v => !currentIds.Contains(v.Id))
                .ToList();

            foreach (var value in valuesToDelete)
            {
                _context.Remove(value);
            }

            foreach (var formValue in dictionary.Values)
            {
           
                if (formValue.Id == 0)
                {
                    var newValue = new AttributeDictionaryValue
                    {
                        Value = formValue.Value,
                       
                    };
                    dbDictionary.Values.Add(newValue);
                }
                else
                {
                    var existingValue = dbDictionary.Values
                        .FirstOrDefault(v => v.Id == formValue.Id);

                    if (existingValue != null)
                    {
                        existingValue.Value = formValue.Value;
                    }
                }
            }

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
