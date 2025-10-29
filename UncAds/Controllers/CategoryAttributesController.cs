using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using UncAds.Data;
using UncAds.Models;

namespace UncAds.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CategoryAttributesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoryAttributesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: CategoryAttributes
        public async Task<IActionResult> Index()
        {
            var attributes = await _context.CategoryAttributes
                .Include(a => a.Category)
                .OrderBy(a => a.Category.Name)
                .ToListAsync();

            return View("~/Views/AdminCategoryAttributes/Index.cshtml", attributes);

        }

        // GET: CategoryAttributes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var attribute = await _context.CategoryAttributes
                .Include(a => a.Category)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (attribute == null) return NotFound();

            return View("~/Views/AdminCategoryAttributes/Details.cshtml", attribute);

        }

        // GET: CategoryAttributes/Create
        public IActionResult Create()
        {
            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name");

            return View("~/Views/AdminCategoryAttributes/Create.cshtml");

        }

        // POST: CategoryAttributes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryAttribute categoryAttribute)
        {
            if (ModelState.IsValid)
            {
                _context.Add(categoryAttribute);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name", categoryAttribute.CategoryId);

            return View("~/Views/AdminCategoryAttributes/Create.cshtml", categoryAttribute);

        }

        // GET: CategoryAttributes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var categoryAttribute = await _context.CategoryAttributes.FindAsync(id);
            if (categoryAttribute == null) return NotFound();

            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name", categoryAttribute.CategoryId);
            return View("~/Views/AdminCategoryAttributes/Edit.cshtml", categoryAttribute);

        }

        // POST: CategoryAttributes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CategoryAttribute categoryAttribute)
        {
            if (id != categoryAttribute.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(categoryAttribute);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.CategoryAttributes.Any(e => e.Id == categoryAttribute.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name", categoryAttribute.CategoryId);
            return View("~/Views/AdminCategoryAttributes/Edit.cshtml", categoryAttribute);

        }

        // GET: CategoryAttributes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var categoryAttribute = await _context.CategoryAttributes
                .Include(c => c.Category)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (categoryAttribute == null) return NotFound();

            return View("~/Views/AdminCategoryAttributes/Delete.cshtml", categoryAttribute);

        }

        // POST: CategoryAttributes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var categoryAttribute = await _context.CategoryAttributes.FindAsync(id);
            if (categoryAttribute != null)
            {
                _context.CategoryAttributes.Remove(categoryAttribute);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
