using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UncAds.Data;
using UncAds.Models;

namespace UncAds.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ForbiddenWordsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ForbiddenWordsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.ForbiddenWords.ToListAsync());
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ForbiddenWord model)
        {
            if (!ModelState.IsValid)
                return View(model);

            _context.ForbiddenWords.Add(model);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var word = await _context.ForbiddenWords.FindAsync(id);
            if (word == null) return NotFound();

            return View(word);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var word = await _context.ForbiddenWords.FindAsync(id);
            if (word != null)
            {
                _context.ForbiddenWords.Remove(word);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
