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
            var ads = await _context.Ads.Include(a => a.User).ToListAsync();
            return View(ads);
        }

        // GET: Ads/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var ad = await _context.Ads
                .Include(a => a.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (ad == null)
                return NotFound();

            return View(ad);
        }

        // GET: Ads/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Ads/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,Description,Date")] Ad ad)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Challenge(); // wymusza logowanie
                }

                ad.UserId = user.Id; // 🔹 przypisanie użytkownika

                _context.Add(ad);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(ad);
        }

        // GET: Ads/Edit/5
        [Authorize]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var ad = await _context.Ads.FindAsync(id);
            if (ad == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");
            if (ad.UserId != user.Id && !isAdmin)
                return Forbid();

            return View(ad);
        }

        // POST: Ads/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Ad ad)
        {
            if (id != ad.Id)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            var existingAd = await _context.Ads.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
            if (existingAd == null)
                return NotFound();

            var isAdmin = User.IsInRole("Admin");
            if (existingAd.UserId != user.Id && !isAdmin)
                return Forbid(); // 403 Forbidden

            if (ModelState.IsValid)
            {
                ad.UserId = existingAd.UserId; // zachowujemy właściciela
                try
                {
                    _context.Update(ad);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AdExists(ad.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
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
            var ad = await _context.Ads.FindAsync(id);
            if (ad == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");
            if (ad.UserId != user.Id && !isAdmin)
                return Forbid();

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
