using System.ComponentModel.DataAnnotations;

namespace RewardSystem.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Kullanıcı adı gerekli")]
        public string Username { get; set; } = "";

        [Required(ErrorMessage = "Şifre gerekli")]
        public string Password { get; set; } = "";

        public string Role { get; set; } = "Ogrenci";
        public bool RememberMe { get; set; }
    }
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Ad gerekli")]
        public string FirstName { get; set; } = "";

        [Required(ErrorMessage = "Soyad gerekli")]
        public string LastName { get; set; } = "";

        // Username otomatik oluşturulur
        public string Username { get; set; } = "";

        [Required(ErrorMessage = "E-posta gerekli")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Fakülte gerekli")]
        public string Faculty { get; set; } = "";

        [Required(ErrorMessage = "Bölüm gerekli")]
        public string Department { get; set; } = "";

        [Required(ErrorMessage = "Sınıf/Derece gerekli")]
        public string Grade { get; set; } = "";

        [Required(ErrorMessage = "Öğrenci Numarası gerekli")]
        [RegularExpression(@"^[0-9]{10,12}$", ErrorMessage = "Öğrenci numarası 10-12 haneli rakamlardan oluşmalıdır")]
        public string StudentNumber { get; set; } = "";

        [Required(ErrorMessage = "Şifre gerekli")]
        public string Password { get; set; } = "";
        
        [Required(ErrorMessage = "Şifre tekrarı gerekli")]
        [Compare("Password", ErrorMessage = "Şifreler uyuşmuyor")]
        public string ConfirmPassword { get; set; } = "";
    }

    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "E-posta gerekli")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
        public string Email { get; set; } = "";
    }

    public class ResetPasswordViewModel
    {
        public string Email { get; set; } = "";
        public string Token { get; set; } = "";

        [Required(ErrorMessage = "Yeni şifre gerekli")]
        [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır")]
        public string Password { get; set; } = "";

        [Required(ErrorMessage = "Şifre tekrarı gerekli")]
        [Compare("Password", ErrorMessage = "Şifreler uyuşmuyor")]
        public string ConfirmPassword { get; set; } = "";
    }

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Mevcut şifrenizi giriniz")]
        public string CurrentPassword { get; set; } = "";

        [Required(ErrorMessage = "Yeni şifre gerekli")]
        [MinLength(6, ErrorMessage = "Yeni şifre en az 6 karakter olmalıdır")]
        public string NewPassword { get; set; } = "";

        [Required(ErrorMessage = "Yeni şifre tekrarı gerekli")]
        [Compare("NewPassword", ErrorMessage = "Yeni şifreler uyuşmuyor")]
        public string ConfirmNewPassword { get; set; } = "";
    }

    public class RankingRow
    {
        public User User { get; set; } = new();
        public int CertificateCount { get; set; }
        public int Score { get; set; }
    }

    public class StudentDashboardViewModel
    {
        public User User { get; set; } = new();
        public int CompetitionCount { get; set; }
        public int CertificateCount { get; set; }
        public int BadgeCount { get; set; }
        public List<Competition> ActiveCompetitions { get; set; } = new();
        public List<Certificate> RecentCertificates { get; set; } = new();
        public List<RankingRow> Ranking { get; set; } = new();
        public List<Notification> Notifications { get; set; } = new();
    }

    public class OgrenciDashboardViewModel
    {
        public User Kullanici { get; set; } = new();
        public int MakaleSayisi { get; set; }
        public int ProjeSayisi { get; set; }
        public int BildiriSayisi { get; set; }
        public dynamic? SonMakaleler { get; set; }
        public List<Notification> Bildirimler { get; set; } = new();
        public List<RankingRow> Siralama { get; set; } = new();
         public int PatentSayisi   { get; set; }
         public int AkademikPuan   { get; set; }
        public int OkunmamisSayisi { get; set; }
        public List<SonGonderiRow> SonGonderiler { get; set; } = new();
    }
    public class SonGonderiRow
    {
        public string Baslik   { get; set; } = "";
        public string Tur      { get; set; } = ""; // "Makale" | "Proje" | "Bildiri" | "Patent"
        public string Status   { get; set; } = "";
        public DateTime Tarih  { get; set; } 
        public string TarihStr { get; set; } = "";
    }

    public class AkademikViewModel
    {
        public List<Article> Makaleler { get; set; } = new();
        public List<Project> Projeler { get; set; } = new();
        public List<Presentation> Bildiriler { get; set; } = new();
        public List<Patent> Patentler { get; set; } = new();
    }
}