using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RewardSystem.Data;
using RewardSystem.Models;
using System.Linq;
using System.Security.Claims;

namespace RewardSystem.Controllers
{
    [Authorize(Roles = "admin,teacher,superadmin")]
    public class AdminController : Controller
    {
        private readonly RewardSystemDbContext _db;
        public AdminController(RewardSystemDbContext db) { _db = db; }

        private int UserId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

        public IActionResult Index()
        {
            ViewBag.PendingArticles = _db.Articles.Count(m => m.Status == "OnayBekliyor");
            ViewBag.PendingStudents = _db.Users.Count(u => u.UserType != null && u.UserType.ToLower() == "ogrenci" && !u.IsActive);
            ViewBag.TotalActiveStudents = _db.Users.Count(u => u.UserType != null && u.UserType.ToLower() == "ogrenci" && u.IsActive);
            
            ViewBag.RecentPendingArticles = _db.Articles
                .Include(a => a.User)
                .Where(a => a.Status == "OnayBekliyor")
                .OrderByDescending(a => a.CreatedAt)
                .Take(5).ToList();

            return View();
        }

        public IActionResult Ogrenciler()
        {
            var students = _db.Users
                .Where(u => u.UserType != null && u.UserType.ToLower() == "ogrenci" && u.IsActive)
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .ToList();
            return View(students);
        }

        [Authorize(Roles = "admin,superadmin")]
        public IActionResult OnayBekleyenOgrenciler()
        {
            var students = _db.Users
                .Where(u => u.UserType != null && u.UserType.ToLower() == "ogrenci" && !u.IsActive)
                .OrderByDescending(u => u.CreateDate)
                .ToList();
            return View(students);
        }

        [Authorize(Roles = "admin,superadmin")]
        [HttpPost]
        public IActionResult OgrenciOnayla(int id)
        {
            var student = _db.Users.FirstOrDefault(u => u.Id == id && u.UserType != null && u.UserType.ToLower() == "ogrenci");
            if (student != null)
            {
                student.IsActive = true;
                _db.SaveChanges();
            }
            return RedirectToAction("OnayBekleyenOgrenciler");
        }

        [Authorize(Roles = "admin,superadmin")]
        public IActionResult AdminYonetimi()
        {
            var bekleyenMakaleler = _db.Articles
                .Include(m => m.User)
                .Where(m => m.Status == "OnayBekliyor")
                .ToList();

            ViewBag.Hocalar = _db.Users.Where(u => u.UserType != null && (u.UserType.ToLower() == "teacher" || u.UserType.ToLower() == "admin" || u.UserType.ToLower() == "superadmin") && u.IsActive).OrderBy(u => u.FirstName).ThenBy(u => u.LastName).ToList();
            return View(bekleyenMakaleler);
        }

        [Authorize(Roles = "admin,superadmin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MakaleHocaAtama(long makaleId, List<int>? hocaIds, List<int>? yuzdeler)
        {
            if (yuzdeler == null || !yuzdeler.Any() || yuzdeler.Sum() != 100)
            {
                TempData["Hata"] = "Toplam puan ağırlığı %100 olmalıdır!";
                return RedirectToAction("AdminYonetimi");
            }
            var makale = _db.Articles.Find(makaleId);
            if (makale != null) { 
                makale.Status = "Degerlendirmede"; 

                var eskiAtamalar = _db.ArticleTeacherAssignments.Where(a => a.ArticleId == makaleId);
                _db.ArticleTeacherAssignments.RemoveRange(eskiAtamalar);

                for (int i = 0; i < hocaIds.Count; i++)
                {
                    _db.ArticleTeacherAssignments.Add(new ArticleTeacherAssignment {
                        ArticleId = makaleId,
                        TeacherId = hocaIds[i],
                        WeightPercentage = yuzdeler[i],
                        IsCompleted = false
                    });
                }
                _db.SaveChanges();
                TempData["Mesaj"] = "Hocalar başarıyla atandı.";
            }
            return RedirectToAction("AdminYonetimi");
        }

        [Authorize(Roles = "teacher,admin,superadmin")]
        public IActionResult Degerlendirmelerim()
        {
            var atamalar = _db.ArticleTeacherAssignments
                .Include(a => a.Article)
                .ThenInclude(art => art.User)
                .Where(a => a.TeacherId == UserId)
                .OrderByDescending(a => a.Id)
                .ToList();

            ViewBag.ProjeAtamalari = new List<object>();
            ViewBag.PatentAtamalari = new List<object>();

            return View(atamalar);
        }

        [Authorize(Roles = "teacher,admin,superadmin")]
        [HttpPost]
        public IActionResult MakaleNotuVer(long makaleId, int puan)
        {
            var atama = _db.ArticleTeacherAssignments.FirstOrDefault(a => a.ArticleId == makaleId && a.TeacherId == UserId);
            if (atama != null)
            {
                atama.GivenScore = puan;
                atama.IsCompleted = true;
                _db.SaveChanges();

                var tumAtamalar = _db.ArticleTeacherAssignments.Where(a => a.ArticleId == makaleId).ToList();
                if (tumAtamalar.All(a => a.IsCompleted))
                {
                    double finalOrtalama = 0;
                    foreach (var a in tumAtamalar)
                    {
                        finalOrtalama += (a.GivenScore * a.WeightPercentage / 100.0);
                    }

                    var makale = _db.Articles.Find(makaleId);
                    if (makale != null)
                    {
                        makale.Score = (int)Math.Round(finalOrtalama);
                        makale.Status = "Onaylandi";
                        _db.SaveChanges();
                    }
                }
            }
            return RedirectToAction("Degerlendirmelerim");
        }

        public IActionResult Bildirimler()
        {
            var notifications = _db.Notifications
                .Where(n => n.UserId == UserId)
                .OrderByDescending(n => n.CreatedAt)
                .ToList();
            return View(notifications);
        }

        [HttpPost]
        public IActionResult BildirimOku(long id)
        {
            var n = _db.Notifications.FirstOrDefault(n => n.Id == id && n.UserId == UserId);
            if (n != null) { n.IsRead = true; _db.SaveChanges(); }
            return RedirectToAction("Bildirimler");
        }

        [Authorize(Roles = "superadmin")]
        public IActionResult KullaniciYonetimi()
        {
            var admins = _db.Users
                .Where(u => u.UserType != null && (u.UserType.ToLower() == "admin" || u.UserType.ToLower() == "superadmin"))
                .OrderBy(u => u.UserType)
                .ToList();
            return View(admins);
        }

        [Authorize(Roles = "superadmin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
            public IActionResult KullaniciEkle(User model, string rawPassword)
            {
                var isAuthenticated = User.Identity?.IsAuthenticated ?? false;
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "null";
                var roles = string.Join(",", User.FindAll(ClaimTypes.Role).Select(r => r.Value));

                if (_db.Users.Any(u => u.Username == model.Username))
                {
                    TempData["Hata"] = "Bu kullanıcı adı zaten mevcut!";
                    return RedirectToAction("KullaniciYonetimi");
                }

                try
                {
                    var passwordHash = RewardSystemDbContext.HashPassword(rawPassword);
                    var userIdClaimValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    var parsedClaimId = int.TryParse(userIdClaimValue, out var claimId) ? claimId : 0;
                    var currentUserId = parsedClaimId > 0 ? parsedClaimId : UserId;
            
                    if (currentUserId <= 0)
                    {
                        currentUserId = _db.Users
                            .Where(u => u.UserType != null && u.UserType.ToLower() == "superadmin")
                            .Select(u => u.Id)
                            .FirstOrDefault();
                    }

                    TempData["DebugMessage"] = $"Authenticated={isAuthenticated}; UserIdClaim={userIdClaimValue}; " +
                        $"ParsedClaimId={parsedClaimId}; UserId={UserId}; CurrentUserId={currentUserId}; Roles={roles}";

                    if (currentUserId <= 0)
                        throw new InvalidOperationException("Geçerli kullanıcı kimliği alınamadı.");

                    _db.Database.ExecuteSqlRaw(
                    $"SET LOCAL app.current_user_id = '{currentUserId}'");
                    model.PasswordHash = passwordHash;
                    model.IsActive = true;
                    model.CreateDate = DateTime.UtcNow;
                    model.CreatedBy = currentUserId;
                    _db.Users.Add(model);
                    _db.SaveChanges();

        
                    var insertedUser = _db.Users.FirstOrDefault(u => u.Username == model.Username);
                    if (insertedUser != null)
                    {
                        int roleId = 3; // teacher default
                        if (model.UserType?.ToLower() == "superadmin") roleId = 1;
                        else if (model.UserType?.ToLower() == "admin") roleId = 2;

            
                        string roleSql = @"
                            INSERT INTO public.user_roles (user_id, role_id, operation_user_id, created_by, create_date)
                            VALUES ({0}, {1}, {2}, {3}, {4})";

                        _db.Database.ExecuteSqlRaw(roleSql,
                            insertedUser.Id,
                            roleId,
                            currentUserId,
                            currentUserId,
                            DateTime.UtcNow);
                         _db.SaveChanges();
                    }

                    TempData["Mesaj"] = "Kullanıcı başarıyla eklendi.";
            }
            catch (Exception ex)
            {
                TempData["Hata"] = "Hata oluştu: " + ex.Message +
                    (ex.InnerException != null ? " | Detay: " + ex.InnerException.Message : "");
            }

            return RedirectToAction("KullaniciYonetimi");
        }

        [Authorize(Roles = "superadmin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult KullaniciDuzenle(int Id, User model, string? rawPassword)
        {
            var user = _db.Users.Find(Id);
            if (user != null)
            {
                user.FullName = model.FullName ?? "";
                user.Username = model.Username ?? "";
                user.Email = model.Email ?? "";
                user.Role = model.Role ?? "";
                user.Department = model.Department ?? "";

                if (!string.IsNullOrEmpty(rawPassword))
                {
                    user.PasswordHash = RewardSystemDbContext.HashPassword(rawPassword);
                }

                _db.SaveChanges();
                TempData["Mesaj"] = "Kullanıcı başarıyla güncellendi.";
            }
            else
            {
                TempData["Hata"] = "Kullanıcı bulunamadı.";
            }
            return RedirectToAction("KullaniciYonetimi");
        }

        [Authorize(Roles = "superadmin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult KullaniciSil(int id)
        {
            var user = _db.Users.Find(id);
            if (user != null)
            {
                // Note: Consider foreign key constraints (Articles, Assignments, etc.)
                // In a real app, you might just want to set user.IsActive = false;
                try 
                {
                    _db.Users.Remove(user);
                    _db.SaveChanges();
                    TempData["Mesaj"] = "Kullanıcı başarıyla silindi.";
                }
                catch (Exception ex)
                {
                    TempData["Hata"] = "Kullanıcı silinemedi (Bağlı veriler olabilir). Sadece durumu pasife çekmeyi deneyin.";
                }
            }
            else
            {
                TempData["Hata"] = "Kullanıcı bulunamadı.";
            }
            return RedirectToAction("KullaniciYonetimi");
        }
    }
}