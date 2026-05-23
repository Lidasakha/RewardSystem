using Microsoft.EntityFrameworkCore;
using RewardSystem.Models;

namespace RewardSystem.Data
{
    public class RewardSystemDbContext : DbContext
    {
        public RewardSystemDbContext(DbContextOptions<RewardSystemDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Competition> Competitions { get; set; }
        public DbSet<CompetitionApplication> CompetitionApplications { get; set; }
        public DbSet<Certificate> Certificates { get; set; }
        public DbSet<Badge> Badges { get; set; }
        public DbSet<Note> Notes { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Article> Articles { get; set; }
        public DbSet<ArticleTeacherAssignment> ArticleTeacherAssignments { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<Presentation> Presentations { get; set; }
        public DbSet<Patent> Patents { get; set; }
        public DbSet<UserAuth> UserAuths { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasDefaultSchema("belek_reward_system");

            // --- users ---
            modelBuilder.Entity<User>().ToTable("users", "public");

            // --- competitions ---
            modelBuilder.Entity<Competition>().ToTable("competitions");
            modelBuilder.Entity<Competition>(e => {
                e.Property(x => x.Id).HasColumnName("id");
                e.Property(x => x.Name).HasColumnName("name");
                e.Property(x => x.Description).HasColumnName("description");
                e.Property(x => x.Organization).HasColumnName("organization");
                e.Property(x => x.Category).HasColumnName("category");
                e.Property(x => x.StartAt).HasColumnName("start_at").HasColumnType("timestamp without time zone");
                e.Property(x => x.EndAt).HasColumnName("end_at").HasColumnType("timestamp without time zone");
                e.Property(x => x.Status).HasColumnName("status");
                e.Property(x => x.GivesCertificate).HasColumnName("gives_certificate");
                e.Property(x => x.RewardDetails).HasColumnName("reward_details");
                e.Property(x => x.CreatedBy).HasColumnName("created_by");
                e.Property(x => x.ApprovedByManager).HasColumnName("approved_by_manager");
                e.Property(x => x.ApprovedAt).HasColumnName("approved_at");
                e.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedBy);
            });

            // --- competition_applications ---
            modelBuilder.Entity<CompetitionApplication>().ToTable("competition_applications");
            modelBuilder.Entity<CompetitionApplication>(e => {
                e.Property(x => x.Id).HasColumnName("id");
                e.Property(x => x.UserId).HasColumnName("user_id");
                e.Property(x => x.CompetitionId).HasColumnName("competition_id");
                e.Property(x => x.AppliedAt).HasColumnName("applied_at");
                e.Property(x => x.Status).HasColumnName("status");
                e.Property(x => x.Ranking).HasColumnName("ranking");
                e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
                e.HasOne(x => x.Competition).WithMany().HasForeignKey(x => x.CompetitionId);
            });

            // --- certificates ---
            modelBuilder.Entity<Certificate>().ToTable("certificates");
            modelBuilder.Entity<Certificate>(e => {
                e.Property(x => x.Id).HasColumnName("id");
                e.Property(x => x.UserId).HasColumnName("user_id");
                e.Property(x => x.CompetitionId).HasColumnName("competition_id");
                e.Property(x => x.CertificateName).HasColumnName("certificate_name");
                e.Property(x => x.Ranking).HasColumnName("ranking");
                e.Property(x => x.IssuedAt).HasColumnName("issued_at");
                e.Property(x => x.ApprovedByManager).HasColumnName("approved_by_manager");
                e.Property(x => x.ApprovedAt).HasColumnName("approved_at");
                e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
                e.HasOne(x => x.Competition).WithMany().HasForeignKey(x => x.CompetitionId);
            });

            // --- badges ---
            modelBuilder.Entity<Badge>().ToTable("badges");
            modelBuilder.Entity<Badge>(e => {
                e.Property(x => x.Id).HasColumnName("id");
                e.Property(x => x.UserId).HasColumnName("user_id");
                e.Property(x => x.BadgeName).HasColumnName("badge_name");
                e.Property(x => x.Description).HasColumnName("description");
                e.Property(x => x.Icon).HasColumnName("icon");
                e.Property(x => x.Color).HasColumnName("color");
                e.Property(x => x.EarnedAt).HasColumnName("earned_at");
                e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
            });

            // --- notes ---
            modelBuilder.Entity<Note>().ToTable("notes");
            modelBuilder.Entity<Note>(e => {
                e.Property(x => x.Id).HasColumnName("id");
                e.Property(x => x.UserId).HasColumnName("user_id");
                e.Property(x => x.Content).HasColumnName("content");
                e.Property(x => x.CreatedAt).HasColumnName("created_at");
                e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
            });

            // --- notifications ---
            modelBuilder.Entity<Notification>().ToTable("notifications");
            modelBuilder.Entity<Notification>(e => {
                e.Property(x => x.Id).HasColumnName("id");
                e.Property(x => x.UserId).HasColumnName("user_id");
                e.Property(x => x.Message).HasColumnName("message");
                e.Property(x => x.IsRead).HasColumnName("is_read");
                e.Property(x => x.CreatedAt).HasColumnName("created_at");
                e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
            });

            // --- articles ---
            modelBuilder.Entity<Article>().ToTable("articles");
            modelBuilder.Entity<Article>(e => {
                e.Property(x => x.Id).HasColumnName("id");
                e.Property(x => x.UserId).HasColumnName("user_id");
                e.Property(x => x.Title).HasColumnName("title");
                e.Property(x => x.Journal).HasColumnName("journal");
                e.Property(x => x.DOI).HasColumnName("doi");
                e.Property(x => x.Year).HasColumnName("year");
                e.Property(x => x.Score).HasColumnName("score");
                e.Property(x => x.Status).HasColumnName("status");
                e.Property(x => x.CreatedAt).HasColumnName("created_at");
                e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
                e.Property(x => x.FilePath).HasColumnName("file_path");
            });

            // --- article_teacher_assignments ---
            modelBuilder.Entity<ArticleTeacherAssignment>().ToTable("article_teacher_assignments");
            modelBuilder.Entity<ArticleTeacherAssignment>(e => {
                e.Property(x => x.Id).HasColumnName("id");
                e.Property(x => x.ArticleId).HasColumnName("article_id");
                e.Property(x => x.TeacherId).HasColumnName("teacher_id");
                e.Property(x => x.WeightPercentage).HasColumnName("weight_percentage");
                e.Property(x => x.GivenScore).HasColumnName("given_score");
                e.Property(x => x.IsCompleted).HasColumnName("is_completed");
                e.HasOne(x => x.Article).WithMany().HasForeignKey(x => x.ArticleId);
                e.HasOne(x => x.Teacher).WithMany().HasForeignKey(x => x.TeacherId);
            });

            // --- projects ---
            modelBuilder.Entity<Project>().ToTable("projects");
            modelBuilder.Entity<Project>(e => {
                e.Property(x => x.Id).HasColumnName("id");
                e.Property(x => x.UserId).HasColumnName("user_id");
                e.Property(x => x.Title).HasColumnName("title");
                e.Property(x => x.Description).HasColumnName("description");
                e.Property(x => x.StartDate).HasColumnName("start_date");
                e.Property(x => x.EndDate).HasColumnName("end_date");
                e.Property(x => x.Status).HasColumnName("status");
                e.Property(x => x.CreatedAt).HasColumnName("created_at");
                e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
                e.Property(x => x.FilePath).HasColumnName("file_path");
            });

            // --- presentations ---
            modelBuilder.Entity<Presentation>().ToTable("presentations");
            modelBuilder.Entity<Presentation>(e => {
                e.Property(x => x.Id).HasColumnName("id");
                e.Property(x => x.UserId).HasColumnName("user_id");
                e.Property(x => x.Title).HasColumnName("title");
                e.Property(x => x.Conference).HasColumnName("conference");
                e.Property(x => x.Year).HasColumnName("year");
                e.Property(x => x.Status).HasColumnName("status");
                e.Property(x => x.CreatedAt).HasColumnName("created_at");
                e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
                e.Property(x => x.FilePath).HasColumnName("file_path");
            });

            // --- patents ---
            modelBuilder.Entity<Patent>().ToTable("patents");
            modelBuilder.Entity<Patent>(e => {
                e.Property(x => x.Id).HasColumnName("id");
                e.Property(x => x.UserId).HasColumnName("user_id");
                e.Property(x => x.Title).HasColumnName("title");
                e.Property(x => x.PatentNumber).HasColumnName("patent_number");
                e.Property(x => x.Year).HasColumnName("year");
                e.Property(x => x.Status).HasColumnName("status");
                e.Property(x => x.CreatedAt).HasColumnName("created_at");
                e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
                e.Property(x => x.FilePath).HasColumnName("file_path");
            });
        }

        public static string HashPassword(string password)
        {
            return Convert.ToBase64String(
                System.Security.Cryptography.SHA256.HashData(
                    System.Text.Encoding.UTF8.GetBytes(password + "TOS_SALT_2025")
                )
            );
        }

        public static bool VerifyPassword(string password, string storedHash)
        {
            if (string.IsNullOrWhiteSpace(storedHash))
                return false;

            if (storedHash == password)
                return true;

            if (storedHash.StartsWith("$2a$") || storedHash.StartsWith("$2b$") || storedHash.StartsWith("$2y$"))
            {
                try
                {
                    return BCrypt.Net.BCrypt.Verify(password, storedHash);
                }
                catch
                {
                    return false;
                }
            }

            return HashPassword(password) == storedHash;
        }
    }
}
