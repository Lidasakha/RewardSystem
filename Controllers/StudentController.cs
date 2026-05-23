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
                AkademikPuan   = makaleSayisi  * 100
                               + projeSayisi   * 80
                               + bildiriSayisi * 40
                               + patentSayisi  * 50,

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

        // 📁 Dosya Kaydet
        private async Task<string?> SaveFileAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0) return null;
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
            var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }
            return "/uploads/" + uniqueFileName;
        }

        [HttpPost]
        public async Task<IActionResult> MakaleEkle(Article model, IFormFile? file)
        {
            model.UserId   = KullaniciId;
            model.Status   = "OnayBekliyor";
            model.FilePath = await SaveFileAsync(file);
            _db.Articles.Add(model);
            await _db.SaveChangesAsync();
            return RedirectToAction("Akademik");
        }

        [HttpPost]
        public async Task<IActionResult> ProjeEkle(Project model, IFormFile? file)
        {
            model.UserId   = KullaniciId;
            model.FilePath = await SaveFileAsync(file);
            _db.Projects.Add(model);
            await _db.SaveChangesAsync();
            return RedirectToAction("Akademik");
        }

        [HttpPost]
        public async Task<IActionResult> BildiriEkle(Presentation model, IFormFile? file)
        {
            model.UserId   = KullaniciId;
            model.FilePath = await SaveFileAsync(file);
            _db.Presentations.Add(model);
            await _db.SaveChangesAsync();
            return RedirectToAction("Akademik");
        }

        [HttpPost]
        public async Task<IActionResult> PatentEkle(Patent model, IFormFile? file)
        {
            model.UserId   = KullaniciId;
            model.FilePath = await SaveFileAsync(file);
            _db.Patents.Add(model);
            await _db.SaveChangesAsync();
            return RedirectToAction("Akademik");
        }

        // 📊 İSTATİSTİKLER
        public IActionResult Istatistikler()
        {
            ViewBag.MakaleSayisi  = _db.Articles.Count(a => a.UserId == KullaniciId);
            ViewBag.ProjeSayisi   = _db.Projects.Count(p => p.UserId == KullaniciId);
            ViewBag.BildiriSayisi = _db.Presentations.Count(b => b.UserId == KullaniciId);
            return View();
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
                    Score = _db.Articles.Count(a => a.UserId == u.Id)       * 100
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
    }
}