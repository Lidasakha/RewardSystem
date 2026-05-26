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
            var rol = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value?.ToLower();

            ViewBag.PendingArticles     = _db.Articles.Count(m => m.Status == "OnayBekliyor");
            ViewBag.PendingStudents     = _db.Users.Count(u => u.UserType != null && u.UserType.ToLower() == "ogrenci" && !u.IsActive);
            ViewBag.TotalActiveStudents = _db.Users.Count(u => u.UserType != null && u.UserType.ToLower() == "ogrenci" && u.IsActive);

            ViewBag.RecentPendingArticles = _db.Articles
                .Include(a => a.User)
                .Where(a => a.Status == "OnayBekliyor")
                .OrderByDescending(a => a.CreatedAt)
                .Take(5).ToList();

            // Teacher-specific stats
            if (rol == "teacher" || rol == "admin")
            {
                var benimDept = _db.Users.Where(u => u.Id == UserId).Select(u => u.Department).FirstOrDefault() ?? "";
                ViewBag.BenimDept = benimDept;
                ViewBag.BenimDegerlendirme = _db.ArticleTeacherAssignments.Count(a => a.TeacherId == UserId && a.IsCompleted);
                ViewBag.BekleyenDegerlendirme = _db.ArticleTeacherAssignments.Count(a => a.TeacherId == UserId && !a.IsCompleted);
                ViewBag.OrtPuan = _db.ArticleTeacherAssignments
                    .Where(a => a.TeacherId == UserId && a.IsCompleted && a.GivenScore > 0)
                    .Select(a => (double?)a.GivenScore)
                    .Average() ?? 0;

                var deptStudentIds = _db.Users
                    .Where(u => u.UserType != null && u.UserType.ToLower() == "ogrenci" && u.IsActive && u.Department == benimDept)
                    .Select(u => u.Id).ToList();
                ViewBag.BolumOgrenci = deptStudentIds.Count;

                // Son değerlendirdikleri
                ViewBag.SonDegerlendirmeler = _db.ArticleTeacherAssignments
                    .Include(a => a.Article).ThenInclude(art => art.User)
                    .Where(a => a.TeacherId == UserId && a.IsCompleted)
                    .OrderByDescending(a => a.Id)
                    .Take(5).ToList();
            }

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
            var tumCalismalar = _db.Articles
                .Include(m => m.User)
                .Where(m => m.Status != null)
                .OrderByDescending(m => m.CreatedAt)
                .ToList();

            // Atamaları da çek — değerlendirme durumu için
            var atamalar = _db.ArticleTeacherAssignments
                .Include(a => a.Article)
                .ToList();
            ViewBag.Atamalar = atamalar;

            ViewBag.Hocalar = _db.Users
                .Where(u => u.UserType != null && 
                    (u.UserType.ToLower() == "teacher" || u.UserType.ToLower() == "admin" || u.UserType.ToLower() == "superadmin") 
                    && u.IsActive)
                .OrderBy(u => u.FirstName).ThenBy(u => u.LastName).ToList();
            return View(tumCalismalar);
        }

        [Authorize(Roles = "admin,superadmin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MakaleOnayVer(long makaleId)
        {
            var makale = _db.Articles.Include(a => a.User).FirstOrDefault(a => a.Id == makaleId);
            if (makale != null)
            {
                makale.Status = "OnaylandiBekliyor"; // Onaylandı ama hoca atanmayı bekliyor
                _db.SaveChanges();

                // Öğrenciye bildirim
                if (makale.UserId > 0)
                {
                    _db.Notifications.Add(new Notification {
                        UserId = (int)makale.UserId,
                        Message = $"'{makale.Title}' başlıklı çalışmanız onaylandı ve hoca ataması bekleniyor.",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    });
                    _db.SaveChanges();
                }
                TempData["Mesaj"] = "Çalışma onaylandı. Artık hoca atayabilirsiniz.";

            }
            return RedirectToAction("AdminYonetimi");
        }

        [Authorize(Roles = "admin,superadmin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MakaleReddet(long makaleId, string? red_neden)
        {
            var makale = _db.Articles.Include(a => a.User).FirstOrDefault(a => a.Id == makaleId);
            if (makale != null)
            {
                makale.Status = "Reddedildi";
                _db.SaveChanges();

                // Öğrenciye bildirim
                if (makale.UserId > 0)
                {
                    var mesaj = string.IsNullOrEmpty(red_neden)
                        ? $"'{makale.Title}' başlıklı çalışmanız reddedildi."
                        : $"'{makale.Title}' başlıklı çalışmanız reddedildi. Sebep: {red_neden}";
                    _db.Notifications.Add(new Notification {
                        UserId = (int)makale.UserId,
                        Message = mesaj,
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    });
                    _db.SaveChanges();
                }
                TempData["Mesaj"] = "Çalışma reddedildi ve öğrenciye bildirim gönderildi.";

            }
            return RedirectToAction("AdminYonetimi");
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
                // Öğrenciye bildirim
                if (makale.UserId > 0)
                {
                    _db.Notifications.Add(new Notification {
                        UserId = (int)makale.UserId,
                        Message = $"'{makale.Title}' başlıklı çalışmanız değerlendirme sürecine alındı. Sonuç bildirilecektir.",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
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

                        // Öğrenciye puan bildirimi
                        if (makale.UserId > 0)
                        {
                            _db.Notifications.Add(new Notification {
                                UserId = (int)makale.UserId,
                                Message = $"'{makale.Title}' başlıklı çalışmanız değerlendirildi ve {makale.Score} puan aldı! 🎉",
                                IsRead = false,
                                CreatedAt = DateTime.UtcNow
                            });
                            _db.SaveChanges();

                            // Ödül kontrolü — yeni puana göre ödül kazanıldı mı?
                            OdulKontrolEt(makale.UserId);
                        }
                    }
                }
            }
            return RedirectToAction("Degerlendirmelerim");
        }


        [Authorize(Roles = "superadmin")]
        public IActionResult Raporlar()
        {
            ViewBag.ToplamOgrenci       = _db.Users.Count(u => u.UserType != null && u.UserType.ToLower() == "ogrenci" && u.IsActive);
            ViewBag.ToplamHoca          = _db.Users.Count(u => u.UserType != null && (u.UserType.ToLower() == "teacher" || u.UserType.ToLower() == "admin") && u.IsActive);
            // Gerçek çalışma sayısı: sadece aktif olanlar (Reddedilenler hariç)
            ViewBag.ToplamMakale        = _db.Articles.Count(a => a.Status != "Reddedildi");
            ViewBag.ToplamOnaylanan     = _db.Articles.Count(a => a.Status == "Onaylandi");
            ViewBag.ToplamBekleyen      = _db.Articles.Count(a => a.Status == "OnayBekliyor");
            ViewBag.ToplamDegerlendirme = _db.Articles.Count(a => a.Status == "Degerlendirmede" || a.Status == "OnaylandiBekliyor");
            ViewBag.ToplamReddedilen    = _db.Articles.Count(a => a.Status == "Reddedildi");

            var durumlar = new[] {
                new { durum = "Onaylandı",          sayi = _db.Articles.Count(a => a.Status == "Onaylandi") },
                new { durum = "Onay Bekliyor",      sayi = _db.Articles.Count(a => a.Status == "OnayBekliyor") },
                new { durum = "Hoca Ataması Bekl.",  sayi = _db.Articles.Count(a => a.Status == "OnaylandiBekliyor") },
                new { durum = "Değerlendirmede",    sayi = _db.Articles.Count(a => a.Status == "Degerlendirmede") },
                new { durum = "Reddedildi",         sayi = _db.Articles.Count(a => a.Status == "Reddedildi") }
            };
            ViewBag.DurumDagilimi = durumlar;

            var bolumIds = _db.Users
                .Where(u => u.UserType != null && u.UserType.ToLower() == "ogrenci" && u.IsActive && u.Department != null && u.Department != "")
                .Select(u => new { u.Id, u.Department })
                .ToList();

            var bolumIstatistik = bolumIds
                .GroupBy(u => u.Department ?? "Belirsiz")
                .Select(g => new {
                    bolum   = g.Key,
                    ogrenci = g.Count(),
                    makale  = _db.Articles.Count(a => g.Select(u => u.Id).Contains(a.UserId))
                })
                .OrderByDescending(x => x.makale)
                .ThenByDescending(x => x.ogrenci)
                .Take(8)
                .ToList<object>();
            ViewBag.BolumIstatistik = bolumIstatistik;

            // Year null ise CreatedAt yılını kullan
            var tumMakaleler = _db.Articles.ToList();
            var yillikTrend = tumMakaleler
                .GroupBy(a => a.Year.HasValue ? a.Year.Value : a.CreatedAt.Year)
                .Select(g => new { yil = (int?)g.Key, sayi = g.Count() })
                .OrderBy(x => x.yil)
                .ToList<object>();
            ViewBag.YillikTrend = yillikTrend;

            var tumOgrenciler = _db.Users
                .Where(u => u.UserType != null && u.UserType.ToLower() == "ogrenci" && u.IsActive)
                .ToList();

            var topOgrenciler = tumOgrenciler
                .Select(u => new {
                    ad     = (u.FirstName + " " + u.LastName).Trim(),
                    bolum  = u.Department ?? "—",
                    puan   = _db.Articles.Where(a => a.UserId == u.Id && a.Status == "Onaylandi").Sum(a => (int?)a.Score) ?? 0,
                    makale = _db.Articles.Count(a => a.UserId == u.Id)
                })
                .OrderByDescending(x => x.puan)
                .Take(5)
                .ToList<object>();
            ViewBag.TopOgrenciler = topOgrenciler;

            var tumHocalar = _db.Users
                .Where(u => u.UserType != null && (u.UserType.ToLower() == "teacher" || u.UserType.ToLower() == "admin") && u.IsActive)
                .ToList();

            var hocaPerf = tumHocalar
                .Select(u => new {
                    ad            = (u.FirstName + " " + u.LastName).Trim(),
                    degerlendirme = _db.ArticleTeacherAssignments.Count(a => a.TeacherId == u.Id && a.IsCompleted)
                })
                .OrderByDescending(x => x.degerlendirme)
                .Take(5)
                .ToList<object>();
            ViewBag.HocaPerformans = hocaPerf;

            return View();
        }



        [Authorize(Roles = "superadmin")]
        public IActionResult RaporlarExcelIndir()
        {
            var ogrenciler = _db.Users
                .Where(u => u.UserType != null && u.UserType.ToLower() == "ogrenci" && u.IsActive)
                .ToList();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Ad Soyad,Bölüm,Toplam Çalışma,Onaylanan,Bekleyen,Toplam Puan");

            foreach (var o in ogrenciler)
            {
                var toplam    = _db.Articles.Count(a => a.UserId == o.Id);
                var onaylanan = _db.Articles.Count(a => a.UserId == o.Id && a.Status == "Onaylandi");
                var bekleyen  = _db.Articles.Count(a => a.UserId == o.Id && a.Status == "OnayBekliyor");
                var puan      = _db.Articles.Where(a => a.UserId == o.Id && a.Status == "Onaylandi").Sum(a => (int?)a.Score) ?? 0;

                sb.AppendLine($"{o.FullName},{o.Department ?? "—"},{toplam},{onaylanan},{bekleyen},{puan}");
            }

            var bytes = System.Text.Encoding.UTF8.GetPreamble()
                .Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString()))
                .ToArray();

            return File(bytes, "text/csv", $"TOS_Rapor_{DateTime.Now:yyyyMMdd}.csv");
        }

        [Authorize(Roles = "superadmin")]
        public IActionResult BolumExcelIndir()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Bölüm,Öğrenci Sayısı,Toplam Çalışma,Onaylanan,Reddedilen");

            var bolumler = _db.Users
                .Where(u => u.UserType != null && u.UserType.ToLower() == "ogrenci" && u.IsActive && u.Department != null)
                .Select(u => new { u.Id, u.Department })
                .ToList()
                .GroupBy(u => u.Department);

            foreach (var g in bolumler)
            {
                var ids       = g.Select(u => u.Id).ToList();
                var toplam    = _db.Articles.Count(a => ids.Contains(a.UserId));
                var onaylanan = _db.Articles.Count(a => ids.Contains(a.UserId) && a.Status == "Onaylandi");
                var reddedilen= _db.Articles.Count(a => ids.Contains(a.UserId) && a.Status == "Reddedildi");

                sb.AppendLine($"{g.Key},{g.Count()},{toplam},{onaylanan},{reddedilen}");
            }

            var bytes = System.Text.Encoding.UTF8.GetPreamble()
                .Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString()))
                .ToArray();

            return File(bytes, "text/csv", $"TOS_Bolum_Rapor_{DateTime.Now:yyyyMMdd}.csv");
        }


        // ══════════════════════════════════════════════
        // ÖDÜL YÖNETİMİ
        // ══════════════════════════════════════════════

        [Authorize(Roles = "superadmin")]
        public IActionResult OdulYonetimi()
        {
            var oduller = _db.Rewards
                .OrderByDescending(r => r.IsActive)
                .ThenBy(r => r.MinPoints)
                .ToList();

            ViewBag.OdulSayilari = oduller.ToDictionary(
                r => r.Id,
                r => _db.Badges.Count(b => b.BadgeName == r.Name)
            );

            // Aktif öğrenciler (manuel ödül verme için)
            ViewBag.Ogrenciler = _db.Users
                .Where(u => u.UserType != null && u.UserType.ToLower() == "ogrenci" && u.IsActive)
                .OrderBy(u => u.FirstName).ThenBy(u => u.LastName)
                .ToList();

            // Verilen tüm rozetler (geri alma için)
            ViewBag.VerilenRozetler = _db.Badges
                .Include(b => b.User)
                .OrderByDescending(b => b.EarnedAt)
                .Take(50)
                .ToList();

            return View(oduller);
        }

        [Authorize(Roles = "superadmin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult OdulEkle(Reward model)
        {
            if (string.IsNullOrWhiteSpace(model.Name) || model.MinPoints < 0)
            {
                TempData["Hata"] = "Ödül adı ve minimum puan zorunludur.";
                return RedirectToAction("OdulYonetimi");
            }
            model.IsActive  = true;
            model.CreatedAt = DateTime.UtcNow;
            _db.Rewards.Add(model);
            _db.SaveChanges();
            TempData["Mesaj"] = $"'{model.Name}' ödülü başarıyla eklendi.";
            return RedirectToAction("OdulYonetimi");
        }

        [Authorize(Roles = "superadmin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult OdulDurumDegistir(int id)
        {
            var odul = _db.Rewards.Find(id);
            if (odul != null)
            {
                odul.IsActive = !odul.IsActive;
                _db.SaveChanges();
                TempData["Mesaj"] = $"'{odul.Name}' ödülü {(odul.IsActive ? "aktifleştirildi" : "pasifleştirildi")}.";
            }
            return RedirectToAction("OdulYonetimi");
        }

        [Authorize(Roles = "superadmin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult OdulSil(int id)
        {
            var odul = _db.Rewards.Find(id);
            if (odul != null)
            {
                _db.Rewards.Remove(odul);
                _db.SaveChanges();
                TempData["Mesaj"] = $"'{odul.Name}' ödülü silindi.";
            }
            return RedirectToAction("OdulYonetimi");
        }


        [Authorize(Roles = "superadmin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ManuelOdulVer(int userId, int rewardId)
        {
            var odul    = _db.Rewards.Find(rewardId);
            var ogrenci = _db.Users.Find(userId);
            if (odul == null || ogrenci == null)
            {
                TempData["Hata"] = "Öğrenci veya ödül bulunamadı.";
                return RedirectToAction("OdulYonetimi");
            }

            var almisMi = _db.Badges.Any(b => b.UserId == userId && b.BadgeName == odul.Name);
            if (almisMi)
            {
                TempData["Hata"] = $"{ogrenci.FullName} zaten '{odul.Name}' ödülüne sahip.";
                return RedirectToAction("OdulYonetimi");
            }

            _db.Badges.Add(new Badge {
                UserId      = userId,
                BadgeName   = odul.Name,
                Description = odul.Description + " (Manuel verildi)",
                Icon        = odul.Icon,
                Color       = odul.Color,
                EarnedAt    = DateTime.UtcNow
            });

            _db.Notifications.Add(new Notification {
                UserId    = userId,
                Message   = $"🏆 Tebrikler! Dekan tarafından '{odul.Name}' ödülü size verildi!",
                IsRead    = false,
                CreatedAt = DateTime.UtcNow
            });

            _db.SaveChanges();
            TempData["Mesaj"] = $"'{odul.Name}' ödülü {ogrenci.FullName} adlı öğrenciye verildi.";
            return RedirectToAction("OdulYonetimi");
        }

        [Authorize(Roles = "superadmin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult OdulGeriAl(long badgeId)
        {
            var badge = _db.Badges.Include(b => b.User).FirstOrDefault(b => b.Id == badgeId);
            if (badge != null)
            {
                var adSoyad   = badge.User?.FullName ?? "Öğrenci";
                var odulAdi   = badge.BadgeName;

                _db.Notifications.Add(new Notification {
                    UserId    = badge.UserId,
                    Message   = $"'{odulAdi}' rozeti hesabınızdan kaldırıldı.",
                    IsRead    = false,
                    CreatedAt = DateTime.UtcNow
                });

                _db.Badges.Remove(badge);
                _db.SaveChanges();
                TempData["Mesaj"] = $"{adSoyad} adlı öğrencinin '{odulAdi}' rozeti geri alındı.";
            }
            return RedirectToAction("OdulYonetimi");
        }

        // Puan güncellenince otomatik ödül kontrolü
        private void OdulKontrolEt(int userId)
        {
            var toplamPuan = _db.Articles
                .Where(a => a.UserId == userId && a.Status == "Onaylandi")
                .Sum(a => (int?)a.Score) ?? 0;

            var aktifOduller = _db.Rewards
                .Where(r => r.IsActive && r.MinPoints <= toplamPuan)
                .ToList();

            foreach (var odul in aktifOduller)
            {
                // Bu ödülü daha önce almış mı?
                var almisMi = _db.Badges.Any(b => b.UserId == userId && b.BadgeName == odul.Name);
                if (!almisMi)
                {
                    _db.Badges.Add(new Badge {
                        UserId      = userId,
                        BadgeName   = odul.Name,
                        Description = odul.Description,
                        Icon        = odul.Icon,
                        Color       = odul.Color,
                        EarnedAt    = DateTime.UtcNow
                    });

                    _db.Notifications.Add(new Notification {
                        UserId    = userId,
                        Message   = $"🏆 Tebrikler! '{odul.Name}' ödülünü kazandınız! {toplamPuan} puana ulaştınız.",
                        IsRead    = false,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
            _db.SaveChanges();
        }

        [Authorize(Roles = "teacher,admin")]
        public IActionResult BolumSiralama()
        {
            var benimDept = _db.Users.Where(u => u.Id == UserId).Select(u => u.Department).FirstOrDefault() ?? "";
            ViewBag.BenimDept = benimDept;

            var siralama = _db.Users
                .Where(u => u.UserType != null && u.UserType.ToLower() == "ogrenci"
                         && u.IsActive && u.Department == benimDept)
                .Select(u => new RewardSystem.Models.RankingRow {
                    User  = u,
                    Score = _db.Articles.Count(a => a.UserId == u.Id && a.Status == "Onaylandi") * 100
                          + _db.Projects.Count(p => p.UserId == u.Id)      * 80
                          + _db.Presentations.Count(b => b.UserId == u.Id) * 40
                          + _db.Patents.Count(c => c.UserId == u.Id)       * 50
                })
                .OrderByDescending(r => r.Score)
                .ToList();

            return View(siralama);
        }

        [Authorize(Roles = "teacher,admin")]
        public IActionResult BolumRaporlar()
        {
            var benimDept = _db.Users.Where(u => u.Id == UserId).Select(u => u.Department).FirstOrDefault() ?? "";
            ViewBag.BenimDept = benimDept;

            var deptStudentIds = _db.Users
                .Where(u => u.UserType != null && u.UserType.ToLower() == "ogrenci"
                         && u.IsActive && u.Department == benimDept)
                .Select(u => u.Id).ToList();

            ViewBag.ToplamOgrenci       = deptStudentIds.Count;
            ViewBag.ToplamCalisma       = _db.Articles.Count(a => deptStudentIds.Contains(a.UserId));
            ViewBag.ToplamOnaylanan     = _db.Articles.Count(a => deptStudentIds.Contains(a.UserId) && a.Status == "Onaylandi");
            ViewBag.ToplamBekleyen      = _db.Articles.Count(a => deptStudentIds.Contains(a.UserId) && a.Status == "OnayBekliyor");
            ViewBag.ToplamDegerlendirme = _db.Articles.Count(a => deptStudentIds.Contains(a.UserId) && a.Status == "Degerlendirmede");
            ViewBag.BenimDegerlendirme  = _db.ArticleTeacherAssignments.Count(a => a.TeacherId == UserId && a.IsCompleted);
            ViewBag.BekleyenAtama       = _db.ArticleTeacherAssignments.Count(a => a.TeacherId == UserId && !a.IsCompleted);
            ViewBag.OrtPuan = _db.ArticleTeacherAssignments
                .Where(a => a.TeacherId == UserId && a.IsCompleted && a.GivenScore > 0)
                .Select(a => (double?)a.GivenScore).Average() ?? 0;

            var durumlar = new[] {
                new { durum = "Onaylandı",       sayi = _db.Articles.Count(a => deptStudentIds.Contains(a.UserId) && a.Status == "Onaylandi") },
                new { durum = "Onay Bekliyor",   sayi = _db.Articles.Count(a => deptStudentIds.Contains(a.UserId) && a.Status == "OnayBekliyor") },
                new { durum = "Değerlendirmede", sayi = _db.Articles.Count(a => deptStudentIds.Contains(a.UserId) && a.Status == "Degerlendirmede") }
            };
            ViewBag.DurumDagilimi = durumlar;

            var yillikTrend = _db.Articles
                .Where(a => deptStudentIds.Contains(a.UserId) && a.Year.HasValue)
                .GroupBy(a => a.Year)
                .Select(g => new { yil = g.Key, sayi = g.Count() })
                .OrderBy(x => x.yil)
                .ToList<object>();
            ViewBag.YillikTrend = yillikTrend;

            var deptStudents = _db.Users
                .Where(u => u.UserType != null && u.UserType.ToLower() == "ogrenci"
                         && u.IsActive && u.Department == benimDept)
                .ToList();

            var topOgrenciler = deptStudents
                .Select(u => new {
                    ad     = (u.FirstName + " " + u.LastName).Trim(),
                    puan   = _db.Articles.Where(a => a.UserId == u.Id && a.Status == "Onaylandi").Sum(a => (int?)a.Score) ?? 0,
                    makale = _db.Articles.Count(a => a.UserId == u.Id)
                })
                .OrderByDescending(x => x.puan)
                .Take(5).ToList<object>();
            ViewBag.TopOgrenciler = topOgrenciler;

            return View();
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult KullaniciDurumDegistir(int id)
        {
            var user = _db.Users.Find(id);
            if (user != null)
            {
                user.IsActive = !user.IsActive;
                _db.SaveChanges();
                var durum = user.IsActive ? "aktifleştirildi" : "pasifleştirildi";
                TempData["Mesaj"] = $"{user.FullName} başarıyla {durum}.";
            }
            return RedirectToAction("KullaniciYonetimi");
        }

        [Authorize(Roles = "superadmin")]
        public IActionResult KullaniciYonetimi()
        {
            var admins = _db.Users
                .Where(u => u.UserType != null && 
                    (u.UserType.ToLower() == "admin" || 
                     u.UserType.ToLower() == "superadmin" || 
                     u.UserType.ToLower() == "teacher"))
                .OrderByDescending(u => u.IsActive)
                .ThenBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
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
                try
                {
                    // Makaleye bağlı atamalar
                    var makaleler = _db.Articles.Where(a => a.UserId == id).ToList();
                    if (makaleler.Any())
                    {
                        var makaleIds = makaleler.Select(m => m.Id).ToList();
                        var atamalar = _db.ArticleTeacherAssignments.Where(a => makaleIds.Contains(a.ArticleId)).ToList();
                        _db.ArticleTeacherAssignments.RemoveRange(atamalar);
                        _db.Articles.RemoveRange(makaleler);
                    }

                    var projeler = _db.Projects.Where(p => p.UserId == id).ToList();
                    if (projeler.Any()) _db.Projects.RemoveRange(projeler);

                    var bildiriler = _db.Presentations.Where(b => b.UserId == id).ToList();
                    if (bildiriler.Any()) _db.Presentations.RemoveRange(bildiriler);

                    var patentler = _db.Patents.Where(p => p.UserId == id).ToList();
                    if (patentler.Any()) _db.Patents.RemoveRange(patentler);

                    var bildirimler = _db.Notifications.Where(n => n.UserId == id).ToList();
                    if (bildirimler.Any()) _db.Notifications.RemoveRange(bildirimler);

                    // Öğretmen olarak atanmışsa onları da sil
                    var ogretmenAta = _db.ArticleTeacherAssignments.Where(a => a.TeacherId == id).ToList();
                    if (ogretmenAta.Any()) _db.ArticleTeacherAssignments.RemoveRange(ogretmenAta);

                    _db.Users.Remove(user);
                    _db.SaveChanges();
                    TempData["Mesaj"] = user.FullName + " başarıyla silindi.";
                }
                catch (Exception)
                {
                    TempData["Hata"] = "Kullanıcı silinirken bir hata oluştu. Lütfen tekrar deneyin.";
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