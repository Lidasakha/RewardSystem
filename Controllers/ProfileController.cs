using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RewardSystem.Data;
using RewardSystem.Models;
using System.Security.Claims;

namespace RewardSystem.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly RewardSystemDbContext _db;

        public ProfileController(RewardSystemDbContext db)
        {
            _db = db;
        }

        private int UserId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

        [HttpGet]
        public IActionResult Index()
        {
            var user = _db.Users.FirstOrDefault(u => u.Id == UserId);
            if (user == null) return RedirectToAction("Cikis", "Account");

            ViewBag.Active = "Profil";
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == UserId);
            if (user == null) return RedirectToAction("Cikis", "Account");

            ViewBag.Active = "Profil";

            if (!ModelState.IsValid)
            {
                TempData["Hata"] = "Lütfen formdaki hataları düzeltin.";
                return View("Index", user);
            }

            // Verify current password
            var userAuth = await _db.UserAuths.FirstOrDefaultAsync(ua => ua.UserId == user.Id);
            var validPassword = RewardSystemDbContext.VerifyPassword(model.CurrentPassword, user.PasswordHash)
                || (userAuth != null && RewardSystemDbContext.VerifyPassword(model.CurrentPassword, userAuth.PasswordHash ?? ""));

            if (!validPassword)
            {
                TempData["Hata"] = "Mevcut şifrenizi yanlış girdiniz.";
                return View("Index", user);
            }

            // Update password
            var newHash = RewardSystemDbContext.HashPassword(model.NewPassword);
            
            await _db.Database.ExecuteSqlRawAsync(@"
                UPDATE public.users SET password_hash = {0} WHERE id = {1}",
                newHash, user.Id);

            _db.ChangeTracker.Clear();

            TempData["Mesaj"] = "Şifreniz başarıyla değiştirildi.";
            return RedirectToAction("Index");
        }
    }
}
