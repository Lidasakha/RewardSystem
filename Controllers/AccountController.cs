using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RewardSystem.Data;
using RewardSystem.Models;
using System.Security.Claims;

namespace RewardSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly RewardSystemDbContext _db;
        private readonly RewardSystem.Services.IEmailService _emailService;

        public AccountController(RewardSystemDbContext db, RewardSystem.Services.IEmailService emailService)
        {
            _db = db;
            _emailService = emailService;
        }

        [HttpGet]
        public IActionResult Giris() => View(new LoginViewModel());

        [HttpPost]
        public async Task<IActionResult> Giris(LoginViewModel model)
        {
            // Kullanıcıyı önce sadece kullanıcı adı ile buluyoruz.
            var user = await _db.Users
                .FromSqlRaw(@"SELECT u.* FROM public.users u
                  WHERE LOWER(u.username) = LOWER({0})",
                    model.Username)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                ModelState.AddModelError("", "Kullanıcı adı, şifre veya rol hatalı!");
                return View(model);
            }

            var userAuth = await _db.UserAuths.FirstOrDefaultAsync(ua => ua.UserId == user.Id);
            var validPassword = RewardSystemDbContext.VerifyPassword(model.Password, user.PasswordHash)
                || (userAuth != null && RewardSystemDbContext.VerifyPassword(model.Password, userAuth.PasswordHash ?? ""));

            if (!validPassword)
            {
                ModelState.AddModelError("", "Kullanıcı adı, şifre veya rol hatalı!");
                return View(model);
            }

            // Hesap onay kontrolü — sadece öğrenciler için
            if (user.Role.ToLower() == "ogrenci" && !user.IsActive)
            {
                ModelState.AddModelError("", "Hesabınız henüz onaylanmamış. Lütfen yönetici onayını bekleyin.");
                return View(model);
            }

            // Hesap aktiflik kontrolü — tüm kullanıcılar için
            if (!user.IsActive)
            {
                ModelState.AddModelError("", "Hesabınız devre dışı bırakılmış. Lütfen yönetici ile iletişime geçin.");
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim("Username", user.Username),
                new Claim(ClaimTypes.Role, user.Role.ToLower()),
                new Claim("Department", user.Department ?? ""),
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            var authProps = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = model.RememberMe
                    ? DateTimeOffset.UtcNow.AddDays(7)
                    : DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProps);

            return user.Role.ToLower() switch
            {
                "admin" or "teacher" or "superadmin" => RedirectToAction("Index", "Admin"),
                _ => RedirectToAction("Index", "Student")
            };
        }

        [HttpGet]
        public IActionResult KayitOl() => View(new RegisterViewModel());

        [HttpPost]
        public async Task<IActionResult> KayitOl(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (_db.Users.Any(u => u.Username == model.Username))
            {
                ModelState.AddModelError("", "Bu kullanıcı adı zaten kullanımda.");
                return View(model);
            }

            if (!string.IsNullOrEmpty(model.Email) && _db.Users.Any(u => u.Email == model.Email))
            {
                ModelState.AddModelError("", "Bu e-posta adresi zaten kullanımda.");
                return View(model);
            }

            var newUser = new User
            {
                FullName = model.FullName,
                Username = model.Username,
                Email = model.Email,
                Department = model.Department,
                StudentNumber = model.StudentNumber,
                PasswordHash = RewardSystemDbContext.HashPassword(model.Password),
                Role = "ogrenci",
                IsActive = false,
                CreatedAt = DateTime.UtcNow
            };

            await _db.Database.ExecuteSqlRawAsync(@"
                INSERT INTO public.users (username, email, password_hash, user_type, is_active, first_name, last_name, department, student_number, create_date, created_by)
                VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, (SELECT user_id FROM public.user_schemas WHERE schema_name = CURRENT_USER LIMIT 1))",
                newUser.Username,
                newUser.Email ?? "",
                newUser.PasswordHash,
                newUser.Role,
                newUser.IsActive,
                newUser.FirstName ?? "",
                newUser.LastName ?? "",
                newUser.Department ?? "",
                newUser.StudentNumber ?? "",
                newUser.CreatedAt);

            TempData["SuccessMessage"] = "Kayıt başarılı! Ancak sisteme giriş yapabilmeniz için kaydınızın yönetici tarafından onaylanması gerekmektedir.";

            // Öğrenciye hoş geldin maili
            try {
                if (!string.IsNullOrEmpty(newUser.Email))
                {
                    await _emailService.SendEmailAsync(
                        newUser.Email,
                        "TÖS — Kayıt Talebiniz Alındı",
                        $@"<div style='font-family:sans-serif;max-width:500px;margin:auto;padding:30px;border:1px solid #e2e8f0;border-radius:12px;'>
                            <h2 style='color:#1a2b4c;'>Merhaba {newUser.FirstName},</h2>
                            <p>Teşvik ve Ödüllendirme Sistemine kayıt talebiniz başarıyla alınmıştır.</p>
                            <p>Hesabınız yönetici onayından sonra aktifleştirilecektir. Onay sonrasında sisteme giriş yapabileceksiniz.</p>
                            <div style='background:#f0f4f8;padding:16px;border-radius:8px;margin:20px 0;'>
                                <b>Kullanıcı Adı:</b> {newUser.Username}<br/>
                                <b>Bölüm:</b> {newUser.Department ?? "—"}
                            </div>
                            <p style='color:#64748b;font-size:13px;'>Bu mail otomatik gönderilmiştir. Lütfen yanıtlamayın.</p>
                        </div>"
                    );
                }
            } catch { /* mail hatası kaydı engellemesin */ }

            return RedirectToAction("Giris");
        }

        [HttpGet]
        public IActionResult SifremiUnuttum() => View(new ForgotPasswordViewModel());

        [HttpPost]
        public async Task<IActionResult> SifremiUnuttum(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = _db.Users.FirstOrDefault(u => u.Email == model.Email);

            if (user != null)
            {
                var token = Guid.NewGuid().ToString();
                var hashedToken = RewardSystemDbContext.HashPassword(token);
                var expiry = DateTime.UtcNow.AddHours(1);

                // RLS kuralını aşabilmek için veritabanı kullanıcısının ID'sini subquery ile bulup veriyoruz
                await _db.Database.ExecuteSqlRawAsync(@"
                    INSERT INTO public.password_reset_tokens 
                        (platform_user_id, token, hashed_token, expires_at, is_used, create_date, created_by)
                    VALUES 
                        ({0}, {1}, {2}, {3}, {4}, NOW(), (SELECT user_id FROM public.user_schemas WHERE schema_name = CURRENT_USER LIMIT 1))",
                    user.Id,
                    token,
                    hashedToken,
                    expiry,
                    false);

                var callbackUrl = Url.Action("SifreSifirla", "Account", new { token, email = user.Email }, protocol: Request.Scheme);
                var body = $@"
                    <h3>Şifre Sıfırlama Talebi</h3>
                    <p>Belek Ödül Sistemi için şifre sıfırlama talebinde bulundunuz.</p>
                    <p>Şifrenizi sıfırlamak için lütfen aşağıdaki bağlantıya tıklayınız:</p>
                    <a href='{callbackUrl}' style='display:inline-block; padding:10px 20px; background-color:#184a32; color:white; text-decoration:none; border-radius:5px;'>Şifremi Sıfırla</a>
                    <p>Bu bağlantı 1 saat boyunca geçerlidir. Eğer bu talebi siz yapmadıysanız bu e-postayı görmezden gelebilirsiniz.</p>";

                try
                {
                    await _emailService.SendEmailAsync(user.Email!, "Şifre Sıfırlama", body);
                }
                catch
                {
                    // Log error
                }
            }

            TempData["SuccessMessage"] = "Eğer bu e-posta adresi sistemde kayıtlıysa, şifre sıfırlama bağlantısı gönderilecektir.";
            return View();
        }

        [HttpGet]
        public IActionResult SifreSifirla(string token, string email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
                return RedirectToAction("Giris");

            return View(new ResetPasswordViewModel { Token = token, Email = email });
        }

        [HttpPost]
        public async Task<IActionResult> SifreSifirla(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = _db.Users.FirstOrDefault(u => u.Email == model.Email);

            if (user == null)
            {
                ModelState.AddModelError("", "Kullanıcı bulunamadı.");
                return View(model);
            }

            var tokenRecord = _db.PasswordResetTokens.FirstOrDefault(t =>
                t.PlatformUserId == user.Id &&
                t.Token == model.Token &&
                !t.IsUsed &&
                t.ExpiresAt > DateTime.UtcNow);

            if (tokenRecord == null)
            {
                ModelState.AddModelError("", "Geçersiz veya süresi dolmuş sıfırlama kodu.");
                return View(model);
            }

            var newHash = RewardSystemDbContext.HashPassword(model.Password);

            // Şifreyi güncelle
            await _db.Database.ExecuteSqlRawAsync(@"
                UPDATE public.users SET password_hash = {0} WHERE id = {1}",
                newHash, user.Id);

            // EF cache'ini temizle — yoksa Giris metodu eski hash'i görür
            _db.ChangeTracker.Clear();

            // Token'ı kullanıldı olarak işaretle
            await _db.Database.ExecuteSqlRawAsync(@"
                UPDATE public.password_reset_tokens 
                SET is_used = true, update_date = NOW()
                WHERE id = {0}",
                tokenRecord.Id);

            TempData["SuccessMessage"] = "Şifreniz başarıyla sıfırlandı. Yeni şifrenizle giriş yapabilirsiniz.";
            return RedirectToAction("Giris");
        }

        public async Task<IActionResult> Cikis()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Giris");
        }
    }
}