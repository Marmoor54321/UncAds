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
            _context.Update(dictionary);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
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
