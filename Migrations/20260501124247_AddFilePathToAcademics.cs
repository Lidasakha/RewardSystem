using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RewardSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddFilePathToAcademics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "belek_reward_system");

            migrationBuilder.CreateTable(
                name: "users",
                schema: "belek_reward_system",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    full_name = table.Column<string>(type: "text", nullable: false),
                    username = table.Column<string>(type: "text", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    role = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: true),
                    department = table.Column<string>(type: "text", nullable: true),
                    grade = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "articles",
                schema: "belek_reward_system",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    journal = table.Column<string>(type: "text", nullable: true),
                    doi = table.Column<string>(type: "text", nullable: true),
                    year = table.Column<int>(type: "integer", nullable: true),
                    score = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    FilePath = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_articles", x => x.id);
                    table.ForeignKey(
                        name: "FK_articles_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "belek_reward_system",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "badges",
                schema: "belek_reward_system",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    badge_name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    icon = table.Column<string>(type: "text", nullable: true),
                    color = table.Column<string>(type: "text", nullable: true),
                    earned_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_badges", x => x.id);
                    table.ForeignKey(
                        name: "FK_badges_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "belek_reward_system",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "competitions",
                schema: "belek_reward_system",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    organization = table.Column<string>(type: "text", nullable: true),
                    category = table.Column<string>(type: "text", nullable: true),
                    start_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    end_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    status = table.Column<string>(type: "text", nullable: true),
                    gives_certificate = table.Column<bool>(type: "boolean", nullable: false),
                    reward_details = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    approved_by_manager = table.Column<bool>(type: "boolean", nullable: false),
                    approved_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_competitions", x => x.id);
                    table.ForeignKey(
                        name: "FK_competitions_users_created_by",
                        column: x => x.created_by,
                        principalSchema: "belek_reward_system",
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "notes",
                schema: "belek_reward_system",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notes", x => x.id);
                    table.ForeignKey(
                        name: "FK_notes_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "belek_reward_system",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                schema: "belek_reward_system",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    message = table.Column<string>(type: "text", nullable: false),
                    is_read = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notifications", x => x.id);
                    table.ForeignKey(
                        name: "FK_notifications_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "belek_reward_system",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "patents",
                schema: "belek_reward_system",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    patent_number = table.Column<string>(type: "text", nullable: true),
                    year = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    FilePath = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_patents", x => x.id);
                    table.ForeignKey(
                        name: "FK_patents_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "belek_reward_system",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "presentations",
                schema: "belek_reward_system",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    conference = table.Column<string>(type: "text", nullable: true),
                    year = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    FilePath = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_presentations", x => x.id);
                    table.ForeignKey(
                        name: "FK_presentations_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "belek_reward_system",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "projects",
                schema: "belek_reward_system",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    start_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    end_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    status = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    FilePath = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_projects", x => x.id);
                    table.ForeignKey(
                        name: "FK_projects_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "belek_reward_system",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "article_teacher_assignments",
                schema: "belek_reward_system",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    article_id = table.Column<long>(type: "bigint", nullable: false),
                    teacher_id = table.Column<long>(type: "bigint", nullable: false),
                    weight_percentage = table.Column<int>(type: "integer", nullable: false),
                    given_score = table.Column<int>(type: "integer", nullable: false),
                    is_completed = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_article_teacher_assignments", x => x.id);
                    table.ForeignKey(
                        name: "FK_article_teacher_assignments_articles_article_id",
                        column: x => x.article_id,
                        principalSchema: "belek_reward_system",
                        principalTable: "articles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_article_teacher_assignments_users_teacher_id",
                        column: x => x.teacher_id,
                        principalSchema: "belek_reward_system",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "certificates",
                schema: "belek_reward_system",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    competition_id = table.Column<long>(type: "bigint", nullable: false),
                    certificate_name = table.Column<string>(type: "text", nullable: false),
                    ranking = table.Column<int>(type: "integer", nullable: true),
                    issued_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    approved_by_manager = table.Column<bool>(type: "boolean", nullable: false),
                    approved_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_certificates", x => x.id);
                    table.ForeignKey(
                        name: "FK_certificates_competitions_competition_id",
                        column: x => x.competition_id,
                        principalSchema: "belek_reward_system",
                        principalTable: "competitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_certificates_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "belek_reward_system",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "competition_applications",
                schema: "belek_reward_system",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    competition_id = table.Column<long>(type: "bigint", nullable: false),
                    applied_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    status = table.Column<string>(type: "text", nullable: true),
                    ranking = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_competition_applications", x => x.id);
                    table.ForeignKey(
                        name: "FK_competition_applications_competitions_competition_id",
                        column: x => x.competition_id,
                        principalSchema: "belek_reward_system",
                        principalTable: "competitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_competition_applications_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "belek_reward_system",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_article_teacher_assignments_article_id",
                schema: "belek_reward_system",
                table: "article_teacher_assignments",
                column: "article_id");

            migrationBuilder.CreateIndex(
                name: "IX_article_teacher_assignments_teacher_id",
                schema: "belek_reward_system",
                table: "article_teacher_assignments",
                column: "teacher_id");

            migrationBuilder.CreateIndex(
                name: "IX_articles_user_id",
                schema: "belek_reward_system",
                table: "articles",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_badges_user_id",
                schema: "belek_reward_system",
                table: "badges",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_certificates_competition_id",
                schema: "belek_reward_system",
                table: "certificates",
                column: "competition_id");

            migrationBuilder.CreateIndex(
                name: "IX_certificates_user_id",
                schema: "belek_reward_system",
                table: "certificates",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_competition_applications_competition_id",
                schema: "belek_reward_system",
                table: "competition_applications",
                column: "competition_id");

            migrationBuilder.CreateIndex(
                name: "IX_competition_applications_user_id",
                schema: "belek_reward_system",
                table: "competition_applications",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_competitions_created_by",
                schema: "belek_reward_system",
                table: "competitions",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_notes_user_id",
                schema: "belek_reward_system",
                table: "notes",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_user_id",
                schema: "belek_reward_system",
                table: "notifications",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_patents_user_id",
                schema: "belek_reward_system",
                table: "patents",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_presentations_user_id",
                schema: "belek_reward_system",
                table: "presentations",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_projects_user_id",
                schema: "belek_reward_system",
                table: "projects",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "article_teacher_assignments",
                schema: "belek_reward_system");

            migrationBuilder.DropTable(
                name: "badges",
                schema: "belek_reward_system");

            migrationBuilder.DropTable(
                name: "certificates",
                schema: "belek_reward_system");

            migrationBuilder.DropTable(
                name: "competition_applications",
                schema: "belek_reward_system");

            migrationBuilder.DropTable(
                name: "notes",
                schema: "belek_reward_system");

            migrationBuilder.DropTable(
                name: "notifications",
                schema: "belek_reward_system");

            migrationBuilder.DropTable(
                name: "patents",
                schema: "belek_reward_system");

            migrationBuilder.DropTable(
                name: "presentations",
                schema: "belek_reward_system");

            migrationBuilder.DropTable(
                name: "projects",
                schema: "belek_reward_system");

            migrationBuilder.DropTable(
                name: "articles",
                schema: "belek_reward_system");

            migrationBuilder.DropTable(
                name: "competitions",
                schema: "belek_reward_system");

            migrationBuilder.DropTable(
                name: "users",
                schema: "belek_reward_system");
        }
    }
}
