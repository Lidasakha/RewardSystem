using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RewardSystem.Data;
using RewardSystem.Models;
using System.Security.Claims;

namespace RewardSystem.Controllers
{
    [Authorize(Roles = "ogrenci")]
    public class StudentController : Controller
    {
        private readonly RewardSystemDbContext _db;
        public StudentController(RewardSystemDbContext db) { _db = db; }

        private int KullaniciId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private string GetHarfNotu(int? puan)
        {
            if (puan == null) return "---";
            if (puan >= 90) return "AA";
            if (puan >= 80) return "BA";
            if (puan >= 70) return "BB";
            if (puan >= 60) return "CB";
            if (puan >= 50) return "CC";
            return "FF";
        }

        // 🏠 ANA SAYFA
        public IActionResult Index()
        {
            var kullanici = _db.Users.FirstOrDefault(u => u.Id == KullaniciId);
            if (kullanici == null) return RedirectToAction("Giris", "Account");

            // --- Son Gönderiler: tüm türleri birleştir ---
            var sonMakaleler = _db.Articles
                .Where(a => a.UserId == KullaniciId)
                .OrderByDescending(a => a.CreatedAt)
                .Take(5)
                .Select(a => new SonGonderiRow {
                    Baslik = a.Title,
                    Tur    = "Makale",
                    Status = a.Status,
                    Tarih  = a.CreatedAt
                }).ToList();

            var sonProjeler = _db.Projects
                .Where(p => p.UserId == KullaniciId)
                .OrderByDescending(p => p.StartDate)
                .Take(5)
                .Select(p => new SonGonderiRow {
                    Baslik = p.Title,
                    Tur    = "Proje",
                    Status = "OnayBekliyor",
                    Tarih  = p.StartDate ?? DateTime.Now
                }).ToList();

            var sonBildiriler = _db.Presentations
                .Where(b => b.UserId == KullaniciId)
                .OrderByDescending(b => b.Year)
                .Take(5)
                .AsEnumerable()
                .Select(b => new SonGonderiRow {
                    Baslik   = b.Title,
                    Tur      = "Bildiri",
                    Status   = "OnayBekliyor",
                    Tarih    = DateTime.Now,
                    TarihStr = b.Year.ToString()
                }).ToList();

            var sonPatentler = _db.Patents
                .Where(p => p.UserId == KullaniciId)
                .OrderByDescending(p => p.Year)
                .Take(5)
                .AsEnumerable()
                .Select(p => new SonGonderiRow {
                    Baslik   = p.Title,
                    Tur      = "Patent",
                    Status   = "OnayBekliyor",
                    Tarih    = DateTime.Now,
                    TarihStr = p.Year.ToString()
                }).ToList();
            
            var tumSonGonderiler = sonMakaleler
                .Concat(sonProjeler)
                .Concat(sonBildiriler)
                .Concat(sonPatentler)
                .OrderByDescending(x => x.Tarih)
                .Take(5)
                .ToList();

            // --- Sayılar ---
            int makaleSayisi  = _db.Articles.Count(a => a.UserId == KullaniciId);
            int projeSayisi   = _db.Projects.Count(p => p.UserId == KullaniciId);
            int bildiriSayisi = _db.Presentations.Count(b => b.UserId == KullaniciId);
            int patentSayisi  = _db.Patents.Count(p => p.UserId == KullaniciId);

            var vm = new OgrenciDashboardViewModel
            {
                Kullanici      = kullanici,
                MakaleSayisi   = makaleSayisi,
                ProjeSayisi    = projeSayisi,
                BildiriSayisi  = bildiriSayisi,
                PatentSayisi   = patentSayisi,
                AkademikPuan   = _db.Articles.Count(a => a.UserId == KullaniciId && a.Status == "Onaylandi") * 100
                               + _db.Projects.Count(p => p.UserId == KullaniciId) * 80
                               + _db.Presentations.Count(b => b.UserId == KullaniciId) * 40
                               + _db.Patents.Count(pt => pt.UserId == KullaniciId) * 50,

                SonGonderiler = tumSonGonderiler,

                OkunmamisSayisi = _db.Notifications
                    .Count(n => n.UserId == KullaniciId && !n.IsRead),

                Bildirimler = _db.Notifications
                    .Where(n => n.UserId == KullaniciId)
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(5)
                    .ToList(),

                Siralama = _db.Users
                    .Where(u => u.UserType != null
                             && u.UserType.ToLower() == "ogrenci"
                             && u.IsActive)
                    .Select(u => new RankingRow {
                        User  = u,
                        Score = _db.Articles.Count(a => a.UserId == u.Id)       * 100
                              + _db.Projects.Count(p => p.UserId == u.Id)       * 80
                              + _db.Presentations.Count(b => b.UserId == u.Id)  * 40
                              + _db.Certificates.Count(c => c.UserId == u.Id)   * 50
                    })
                    .OrderByDescending(r => r.Score)
                    .Take(5)
                    .ToList()
            };

            return View(vm);
        }

        // 📚 AKADEMİK
        public IActionResult Akademik()
        {
            var vm = new AkademikViewModel
            {
                Makaleler = _db.Articles
                    .Where(a => a.UserId == KullaniciId)
                    .OrderByDescending(a => a.Year)
                    .ToList(),

                Projeler = _db.Projects
                    .Where(p => p.UserId == KullaniciId)
                    .OrderByDescending(p => p.StartDate)
                    .ToList(),

                Bildiriler = _db.Presentations
                    .Where(b => b.UserId == KullaniciId)
                    .OrderByDescending(b => b.Year)
                    .ToList(),

                Patentler = _db.Patents
                    .Where(p => p.UserId == KullaniciId)
                    .OrderByDescending(p => p.Year)
                    .ToList()
            };

            return View(vm);
        }

        // 📁 Dosya Kaydet — boyut ve tip kontrolü ile
        private static readonly string[] _izinliTipler = {
            ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
            ".jpg", ".jpeg", ".png", ".zip", ".rar"
        };
        private const long _maxDosyaBoyutu = 10 * 1024 * 1024; // 10 MB

        private async Task<(string? path, string? error)> SaveFileAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0) return (null, null);

            // Boyut kontrolü
            if (file.Length > _maxDosyaBoyutu)
                return (null, $"Dosya boyutu 10 MB'ı aşamaz. Yüklenen: {file.Length / 1024 / 1024} MB");

            // Tip kontrolü
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_izinliTipler.Contains(ext))
                return (null, $"Geçersiz dosya türü: {ext}. İzin verilenler: PDF, Word, Excel, PowerPoint, JPG, PNG, ZIP");

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
            var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }
            return ("/uploads/" + uniqueFileName, null);
        }

        [HttpPost]
        public async Task<IActionResult> MakaleEkle(Article model, IFormFile? file)
        {
            var (mPath, mErr) = await SaveFileAsync(file);
            if (mErr != null) { TempData["Hata"] = mErr; return RedirectToAction("YeniCalisma"); }
            model.UserId   = KullaniciId;
            model.Status   = "OnayBekliyor";
            model.FilePath = mPath;
            model.CreatedAt = DateTime.UtcNow;
            _db.Articles.Add(model);
            await _db.SaveChangesAsync();
            _db.Notifications.Add(new RewardSystem.Models.Notification {
                UserId = KullaniciId,
                Message = $"'{model.Title}' başlıklı makaleniz sisteme gönderildi. Onay süreci başlatıldı.",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
            TempData["Mesaj"] = "Makale başarıyla gönderildi.";
            return RedirectToAction("Akademik");
        }

        [HttpPost]
        public async Task<IActionResult> ProjeEkle(Project model, IFormFile? file)
        {
            var (pPath, pErr) = await SaveFileAsync(file);
            if (pErr != null) { TempData["Hata"] = pErr; return RedirectToAction("YeniCalisma"); }
            model.UserId   = KullaniciId;
            model.FilePath = pPath;
            _db.Projects.Add(model);
            await _db.SaveChangesAsync();
            _db.Notifications.Add(new RewardSystem.Models.Notification {
                UserId = KullaniciId,
                Message = $"'{model.Title}' başlıklı projeniz sisteme gönderildi. Onay süreci başlatıldı.",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
            TempData["Mesaj"] = "Proje başarıyla gönderildi.";
            return RedirectToAction("Akademik");
        }

        [HttpPost]
        public async Task<IActionResult> BildiriEkle(Presentation model, IFormFile? file)
        {
            var (bPath, bErr) = await SaveFileAsync(file);
            if (bErr != null) { TempData["Hata"] = bErr; return RedirectToAction("YeniCalisma"); }
            model.UserId   = KullaniciId;
            model.FilePath = bPath;
            _db.Presentations.Add(model);
            await _db.SaveChangesAsync();
            _db.Notifications.Add(new RewardSystem.Models.Notification {
                UserId = KullaniciId,
                Message = $"'{model.Title}' başlıklı sönümünüz sisteme gönderildi. Onay süreci başlatıldı.",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
            TempData["Mesaj"] = "Sönum başarıyla gönderildi.";
            return RedirectToAction("Akademik");
        }

        [HttpPost]
        public async Task<IActionResult> PatentEkle(Patent model, IFormFile? file)
        {
            var (ptPath, ptErr) = await SaveFileAsync(file);
            if (ptErr != null) { TempData["Hata"] = ptErr; return RedirectToAction("YeniCalisma"); }
            model.UserId   = KullaniciId;
            model.FilePath = ptPath;
            _db.Patents.Add(model);
            await _db.SaveChangesAsync();
            _db.Notifications.Add(new RewardSystem.Models.Notification {
                UserId = KullaniciId,
                Message = $"'{model.Title}' başlıklı patentiniz sisteme gönderildi. Onay süreci başlatıldı.",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
            TempData["Mesaj"] = "Patent başarıyla gönderildi.";
            return RedirectToAction("Akademik");
        }

        // 📊 İSTATİSTİKLER
        public IActionResult YeniCalisma()
        {
            return View();
        }

        public IActionResult Istatistikler()
        {
            ViewBag.MakaleSayisi  = _db.Articles.Count(a => a.UserId == KullaniciId);
            ViewBag.ProjeSayisi   = _db.Projects.Count(p => p.UserId == KullaniciId);
            ViewBag.BildiriSayisi = _db.Presentations.Count(b => b.UserId == KullaniciId);
            return View();
        }



        // 🎓 SERTİFİKA
        public IActionResult Sertifika(long id)
        {
            var badge = _db.Badges
                .Include(b => b.User)
                .FirstOrDefault(b => b.Id == id && b.UserId == KullaniciId);

            if (badge == null) return NotFound();

            var toplamPuan = _db.Articles
                .Where(a => a.UserId == KullaniciId && a.Status == "Onaylandi")
                .Sum(a => (int?)a.Score) ?? 0;

            ViewBag.ToplamPuan = toplamPuan;
            return View(badge);
        }

        // 🏆 ÖDÜLLER
        public IActionResult Oduller()
        {
            var benimOdullerim = _db.Badges
                .Where(b => b.UserId == KullaniciId)
                .OrderByDescending(b => b.EarnedAt)
                .ToList();

            var toplamPuan = _db.Articles
                .Where(a => a.UserId == KullaniciId && a.Status == "Onaylandi")
                .Sum(a => (int?)a.Score) ?? 0;

            ViewBag.ToplamPuan = toplamPuan;

            // Tüm aktif ödüller ve hangilerini kazandı/kazanamadı
            var tumOduller = _db.Rewards
                .Where(r => r.IsActive)
                .OrderBy(r => r.MinPoints)
                .ToList();

            ViewBag.TumOduller = tumOduller;
            ViewBag.KazanilanAdlar = benimOdullerim.Select(b => b.BadgeName).ToList();

            return View(benimOdullerim);
        }

        // 🏆 SIRALAMA
        public IActionResult Siralama()
        {
            ViewBag.MyId = KullaniciId;

            var siralama = _db.Users
                .Where(u => u.UserType != null
                         && u.UserType.ToLower() == "ogrenci"
                         && u.IsActive)
                .Select(u => new RankingRow {
                    User  = u,
                    Score = _db.Articles.Count(a => a.UserId == u.Id && a.Status == "Onaylandi") * 100
                          + _db.Projects.Count(p => p.UserId == u.Id)       * 80
                          + _db.Presentations.Count(b => b.UserId == u.Id)  * 40
                          + _db.Certificates.Count(c => c.UserId == u.Id)   * 50
                })
                .OrderByDescending(r => r.Score)
                .ToList();

            return View(siralama);
        }

        // 🔔 BİLDİRİMLER
        public IActionResult Bildirimler()
        {
            var bildirimler = _db.Notifications
                .Where(n => n.UserId == KullaniciId)
                .OrderByDescending(n => n.CreatedAt)
                .ToList();

            return View(bildirimler);
        }

        [HttpPost]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult BildirimOkundu(long id)
        {
            var bildirim = _db.Notifications
                .FirstOrDefault(n => n.Id == id && n.UserId == KullaniciId);
            if (bildirim != null)
            {
                bildirim.IsRead = true;
                _db.SaveChanges();
            }
            return RedirectToAction("Bildirimler");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TumunuOku()
        {
            var bildirimler = _db.Notifications
                .Where(n => n.UserId == KullaniciId && !n.IsRead)
                .ToList();
            foreach (var b in bildirimler)
                b.IsRead = true;
            _db.SaveChanges();
            return RedirectToAction("Bildirimler");
        }
    }
}