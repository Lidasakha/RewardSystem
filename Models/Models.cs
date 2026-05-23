using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RewardSystem.Models
{
    [Table("users", Schema = "public")]
    public class User
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("username")]
        public string Username { get; set; } = "";

        [Column("email")]
        public string Email { get; set; } = "";

        [Column("password_hash")]
        public string PasswordHash { get; set; } = "";

        [Column("first_name")]
        public string? FirstName { get; set; }

        [Column("last_name")]
        public string? LastName { get; set; }

        [Column("user_type")]
        public string? UserType { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_by")]
        public int? CreatedBy { get; set; }

        [Column("updated_by")]
        public int? UpdatedBy { get; set; }

        [Column("create_date")]
        public DateTime CreateDate { get; set; } = DateTime.UtcNow;

        [Column("update_date")]
        public DateTime UpdateDate { get; set; } = DateTime.UtcNow;

        [Column("profile_image_url")]
        public string? ProfileImageUrl { get; set; }

        [Column("phone")]
        public string? Phone { get; set; }

        [Column("is_email_verified")]
        public bool IsEmailVerified { get; set; } = false;

        [Column("email_verified_at")]
        public DateTime? EmailVerifiedAt { get; set; }

        [Column("language_code")]
        public string? LanguageCode { get; set; } = "tr";

        [Column("gender")]
        public string? Gender { get; set; }

        [Column("title")]
        public string Title { get; set; } = "";

        [Column("faculty")]
        public string Faculty { get; set; } = "";

        [Column("department")]
        public string Department { get; set; } = "";

        [Column("student_number")]
        public string? StudentNumber { get; set; }

        // --- Geriye Dönük Uyumluluk (UI ve Mantık Bozulmaması İçin) ---
        [NotMapped]
        public string FullName 
        { 
            get => $"{FirstName} {LastName}".Trim(); 
            set 
            {
                var parts = value?.Split(' ', 2);
                FirstName = parts?.Length > 0 ? parts[0] : "";
                LastName = parts?.Length > 1 ? parts[1] : "";
            }
        }

        [NotMapped]
        public string Role { get => UserType ?? ""; set => UserType = value; }

        [NotMapped]
        public string? Grade { get => StudentNumber; set => StudentNumber = value; }

        [NotMapped]
        public DateTime CreatedAt { get => CreateDate; set => CreateDate = value; }

        [NotMapped]
        public string? ResetPasswordToken { get; set; }

        [NotMapped]
        public DateTime? ResetPasswordTokenExpiry { get; set; }
    }

    // Tablo: competitions
    public class Competition
    {
        public long Id { get; set; }
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public string? Organization { get; set; }
        public string? Category { get; set; }
        public DateTime? StartAt { get; set; }
        public DateTime? EndAt { get; set; }
        public string? Status { get; set; }
        public bool GivesCertificate { get; set; }
        public string? RewardDetails { get; set; }
        public int? CreatedBy { get; set; }
        public bool ApprovedByManager { get; set; }
        public DateTime? ApprovedAt { get; set; }

        public User? CreatedByUser { get; set; }
    }

    // Tablo: competition_applications
    public class CompetitionApplication
    {
        public long Id { get; set; }
        public int UserId { get; set; }
        public long CompetitionId { get; set; }
        public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
        public string? Status { get; set; }
        public int? Ranking { get; set; }

        public User? User { get; set; }
        public Competition? Competition { get; set; }
    }

    
    public class Certificate
    {
        public long Id { get; set; }
        public int UserId { get; set; }
        public long CompetitionId { get; set; }
        public string CertificateName { get; set; } = "";
        public int? Ranking { get; set; }
        public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
        public bool ApprovedByManager { get; set; }
        public DateTime? ApprovedAt { get; set; }

        public User? User { get; set; }
        public Competition? Competition { get; set; }
    }

    // Tablo: badges
    public class Badge
    {
        public long Id { get; set; }
        public int UserId { get; set; }
        public string BadgeName { get; set; } = "";
        public string? Description { get; set; }
        public string? Icon { get; set; }
        public string? Color { get; set; }
        public DateTime EarnedAt { get; set; } = DateTime.UtcNow;

        public User? User { get; set; }
    }

    // Tablo: notes
    public class Note
    {
        public long Id { get; set; }
        public int UserId { get; set; }
        public string Content { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User? User { get; set; }
    }

    // Tablo: notifications
    public class Notification
    {
        public long Id { get; set; }
        public int UserId { get; set; }
        public string Message { get; set; } = "";
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User? User { get; set; }
    }

    // Tablo: articles
    public class Article
    {
        public long Id { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = "";
        public string? Journal { get; set; }
        public string? DOI { get; set; }
        public int? Year { get; set; }
        public int? Score { get; set; }
        public string? Status { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? FilePath { get; set; }

        public User? User { get; set; }
    }

    // Tablo: article_teacher_assignments
    public class ArticleTeacherAssignment
    {
        public long Id { get; set; }
        public long ArticleId { get; set; }
        public int TeacherId { get; set; }
        public int WeightPercentage { get; set; }
        public int GivenScore { get; set; }
        public bool IsCompleted { get; set; }

        public Article? Article { get; set; }
        public User? Teacher { get; set; }
    }

    // Tablo: projects
    public class Project
    {
        public long Id { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = "";
        public string? Description { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Status { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? FilePath { get; set; }

        public User? User { get; set; }
    }

    // Tablo: presentations
    public class Presentation
    {
        public long Id { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = "";
        public string? Conference { get; set; }
        public int? Year { get; set; }
        public string? Status { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? FilePath { get; set; }

        public User? User { get; set; }
    }

    // Tablo: patents
    public class Patent
    {
        public long Id { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = "";
        public string? PatentNumber { get; set; }
        public int? Year { get; set; }
        public string? Status { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? FilePath { get; set; }

        public User? User { get; set; }
    }

    [Table("user_auth", Schema = "public")]
    public class UserAuth
    {
        [Key]
        [Column("user_id")]
        public int UserId { get; set; }

        [Column("password_hash")]
        public string? PasswordHash { get; set; }

        [Column("password_reset_token")]
        public string? PasswordResetToken { get; set; }

        [Column("password_reset_expires")]
        public DateTime? PasswordResetExpires { get; set; }

        [Column("temp_password")]
        public string? TempPassword { get; set; }

        [Column("must_change_password")]
        public bool MustChangePassword { get; set; }

        [Column("last_login")]
        public DateTime? LastLogin { get; set; }

        [Column("is_staff")]
        public bool IsStaff { get; set; }

        [Column("is_superuser")]
        public bool IsSuperuser { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("created_by")]
        public int? CreatedBy { get; set; }
    }

    [Table("password_reset_tokens", Schema = "public")]
    public class PasswordResetToken
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("platform_user_id")]
        public int PlatformUserId { get; set; }

        [Column("token")]
        public string Token { get; set; } = "";

        [Column("hashed_token")]
        public string HashedToken { get; set; } = "";

        [Column("expires_at")]
        public DateTime ExpiresAt { get; set; }

        [Column("is_used")]
        public bool IsUsed { get; set; } = false;

        [Column("create_date")]
        public DateTime CreateDate { get; set; } = DateTime.UtcNow;

        [Column("update_date")]
        public DateTime UpdateDate { get; set; } = DateTime.UtcNow;

        [Column("created_by")]
        public int? CreatedBy { get; set; }

        [Column("updated_by")]
        public int? UpdatedBy { get; set; }

        [ForeignKey("PlatformUserId")]
        public User? User { get; set; }
    }
}
