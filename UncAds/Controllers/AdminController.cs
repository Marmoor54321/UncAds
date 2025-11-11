using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using UncAds.Data;
using UncAds.Models;

namespace UncAds.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public AdminController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        // GET: Admin/Users
        public IActionResult Users()
        {
            var users = _userManager.Users.ToList();
            return View(users);
        }

        // GET: Admin/EditRole/{id}
        public async Task<IActionResult> EditRole(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var userRoles = await _userManager.GetRolesAsync(user);
            var allRoles = _roleManager.Roles.Select(r => r.Name).ToList();

            ViewBag.AllRoles = allRoles;
            ViewBag.UserRoles = userRoles;

            return View(user);
        }

        // POST: Admin/EditRole/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRole(string id, string[] selectedRoles)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var currentRoles = await _userManager.GetRolesAsync(user);
            var rolesToAdd = selectedRoles.Except(currentRoles);
            var rolesToRemove = currentRoles.Except(selectedRoles);

            await _userManager.AddToRolesAsync(user, rolesToAdd);
            await _userManager.RemoveFromRolesAsync(user, rolesToRemove);

            return RedirectToAction(nameof(Users));
        }

        // GET: Admin/DeleteUser/{id}
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            return View(user);
        }

        // POST: Admin/DeleteUser/{id}
        [HttpPost, ActionName("DeleteUser")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUserConfirmed(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            await _userManager.DeleteAsync(user);
            return RedirectToAction(nameof(Users));
        }
        // GET: Admin/Settings
        public async Task<IActionResult> Settings()
        {
            var settings = await _context.AdminSettings.FirstOrDefaultAsync();
            if (settings == null)
            {
                settings = new AdminSettings();
                _context.AdminSettings.Add(settings);
                await _context.SaveChangesAsync();
            }
            return View(settings);
        }

        // POST: Admin/Settings
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Settings(AdminSettings model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var settings = await _context.AdminSettings.FirstOrDefaultAsync();
            if (settings == null)
            {
                settings = new AdminSettings();
                _context.AdminSettings.Add(settings);
            }

            settings.MaxAttachments = model.MaxAttachments;
            settings.MaxFileSizeMB = model.MaxFileSizeMB;
            settings.MaxMediaFiles = model.MaxMediaFiles;
            settings.HomePageMessage = model.HomePageMessage;

            await _context.SaveChangesAsync();
            ViewBag.Message = "Zapisano ustawienia.";
            return View(settings);
        }
    }
}
