using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UncAds.Data;
using UncAds.Models;

namespace UncAds.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Categories
        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories
                .Include(c => c.ParentCategory)
                .ToListAsync();

            return View(categories);
        }

        // GET: /Categories/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var category = await _context.Categories
                .Include(c => c.ParentCategory)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (category == null) return NotFound();

            return View(category);
        }

        // GET: /Categories/Create
        public IActionResult Create()
        {
            ViewData["ParentCategoryId"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                _context.Categories, "Id", "Name");
            return View();
        }

        // POST: /Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,ParentCategoryId")] Category category)
        {
            if (ModelState.IsValid)
            {
                _context.Add(category);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["ParentCategoryId"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                _context.Categories, "Id", "Name", category.ParentCategoryId);
            return View(category);
        }

        // GET: /Categories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();

            ViewData["ParentCategoryId"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                _context.Categories.Where(c => c.Id != id),
                "Id", "Name", category.ParentCategoryId);

            return View(category);
        }

        // POST: /Categories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,ParentCategoryId")] Category category)
        {
            if (id != category.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(category);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Categories.Any(e => e.Id == category.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["ParentCategoryId"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                _context.Categories, "Id", "Name", category.ParentCategoryId);
            return View(category);
        }

        // GET: /Categories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var category = await _context.Categories
                .Include(c => c.ParentCategory)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (category == null) return NotFound();

            return View(category);
        }

        // POST: /Categories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Children)
                .Include(c => c.CategoryAttributes)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category != null)
            {
                // usuń atrybuty kategorii
                if (category.CategoryAttributes?.Any() == true)
                    _context.CategoryAttributes.RemoveRange(category.CategoryAttributes);

                // usuń dzieci (rekurencyjnie lub pojedynczo)
                if (category.Children?.Any() == true)
                    _context.Categories.RemoveRange(category.Children);

                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
