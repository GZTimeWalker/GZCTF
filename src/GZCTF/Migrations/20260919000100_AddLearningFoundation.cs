using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GZCTF.Migrations
{
    /// <inheritdoc />
    public partial class AddLearningFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Challenges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<byte>(type: "smallint", nullable: false),
                    Difficulty = table.Column<byte>(type: "smallint", nullable: false),
                    PublicationState = table.Column<byte>(type: "smallint", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    ExpectedMinutes = table.Column<int>(type: "integer", nullable: false),
                    SourceType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SourceId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SourceName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    SourceMetadataJson = table.Column<string>(type: "jsonb", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Challenges", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lessons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lessons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChallengeFlags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChallengeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<byte>(type: "smallint", nullable: false),
                    Value = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Template = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    AttachmentPoolKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    MetadataJson = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChallengeFlags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChallengeFlags_Challenges_ChallengeId",
                        column: x => x.ChallengeId,
                        principalTable: "Challenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChallengeHints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChallengeId = table.Column<Guid>(type: "uuid", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Locale = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChallengeHints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChallengeHints_Challenges_ChallengeId",
                        column: x => x.ChallengeId,
                        principalTable: "Challenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChallengeLocalizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChallengeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Locale = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Summary = table.Column<string>(type: "text", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChallengeLocalizations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChallengeLocalizations_Challenges_ChallengeId",
                        column: x => x.ChallengeId,
                        principalTable: "Challenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChallengeProgress",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChallengeId = table.Column<Guid>(type: "uuid", nullable: false),
                    SolvedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SolveMode = table.Column<byte>(type: "smallint", nullable: false),
                    AttributionMetadataJson = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChallengeProgress", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChallengeProgress_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChallengeProgress_Challenges_ChallengeId",
                        column: x => x.ChallengeId,
                        principalTable: "Challenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChallengeWriteups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChallengeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Locale = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChallengeWriteups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChallengeWriteups_Challenges_ChallengeId",
                        column: x => x.ChallengeId,
                        principalTable: "Challenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LessonLocalizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LessonId = table.Column<Guid>(type: "uuid", nullable: false),
                    Locale = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LessonLocalizations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LessonLocalizations_Lessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "Lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LessonProgress",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LessonId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LessonProgress", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LessonProgress_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LessonProgress_Lessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "Lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Enrollments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PathId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsCurrent = table.Column<bool>(type: "boolean", nullable: false),
                    EnrolledAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Enrollments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Enrollments_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LearningModuleLocalizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Locale = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Summary = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningModuleLocalizations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LearningModules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    ExpectedMinutes = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningModules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ModuleItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    LessonId = table.Column<Guid>(type: "uuid", nullable: true),
                    ChallengeId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModuleItems", x => x.Id);
                    table.CheckConstraint("CK_ModuleItems_ExactlyOneContent", "(\"LessonId\" IS NOT NULL AND \"ChallengeId\" IS NULL) OR (\"LessonId\" IS NULL AND \"ChallengeId\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_ModuleItems_Challenges_ChallengeId",
                        column: x => x.ChallengeId,
                        principalTable: "Challenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ModuleItems_LearningModules_ModuleId",
                        column: x => x.ModuleId,
                        principalTable: "LearningModules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModuleItems_Lessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "Lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LearningPathLocalizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PathId = table.Column<Guid>(type: "uuid", nullable: false),
                    Locale = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Summary = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningPathLocalizations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LearningPathRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PathId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<byte>(type: "smallint", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PublishedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningPathRevisions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LearningPaths",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Slug = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CurrentPublishedRevisionId = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningPaths", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LearningPaths_LearningPathRevisions_CurrentPublishedRevisio~",
                        column: x => x.CurrentPublishedRevisionId,
                        principalTable: "LearningPathRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeFlags_ChallengeId",
                table: "ChallengeFlags",
                column: "ChallengeId");

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeHints_ChallengeId_Locale_SortOrder",
                table: "ChallengeHints",
                columns: new[] { "ChallengeId", "Locale", "SortOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeLocalizations_ChallengeId_Locale",
                table: "ChallengeLocalizations",
                columns: new[] { "ChallengeId", "Locale" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeProgress_ChallengeId",
                table: "ChallengeProgress",
                column: "ChallengeId");

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeProgress_SolvedAtUtc",
                table: "ChallengeProgress",
                column: "SolvedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeProgress_UserId_ChallengeId",
                table: "ChallengeProgress",
                columns: new[] { "UserId", "ChallengeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeWriteups_ChallengeId_Locale",
                table: "ChallengeWriteups",
                columns: new[] { "ChallengeId", "Locale" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_PathId",
                table: "Enrollments",
                column: "PathId");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_UserId",
                table: "Enrollments",
                column: "UserId",
                unique: true,
                filter: "\"IsCurrent\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_UserId_PathId",
                table: "Enrollments",
                columns: new[] { "UserId", "PathId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LearningModuleLocalizations_ModuleId_Locale",
                table: "LearningModuleLocalizations",
                columns: new[] { "ModuleId", "Locale" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LearningModules_RevisionId_SortOrder",
                table: "LearningModules",
                columns: new[] { "RevisionId", "SortOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LearningPathLocalizations_PathId_Locale",
                table: "LearningPathLocalizations",
                columns: new[] { "PathId", "Locale" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LearningPathRevisions_PathId",
                table: "LearningPathRevisions",
                column: "PathId",
                unique: true,
                filter: "\"Status\" = 0");

            migrationBuilder.CreateIndex(
                name: "IX_LearningPaths_CurrentPublishedRevisionId",
                table: "LearningPaths",
                column: "CurrentPublishedRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_LearningPaths_Slug",
                table: "LearningPaths",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LessonLocalizations_LessonId_Locale",
                table: "LessonLocalizations",
                columns: new[] { "LessonId", "Locale" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LessonProgress_LessonId",
                table: "LessonProgress",
                column: "LessonId");

            migrationBuilder.CreateIndex(
                name: "IX_LessonProgress_UserId_LessonId",
                table: "LessonProgress",
                columns: new[] { "UserId", "LessonId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModuleItems_ChallengeId",
                table: "ModuleItems",
                column: "ChallengeId");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleItems_LessonId",
                table: "ModuleItems",
                column: "LessonId");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleItems_ModuleId_SortOrder",
                table: "ModuleItems",
                columns: new[] { "ModuleId", "SortOrder" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollments_LearningPaths_PathId",
                table: "Enrollments",
                column: "PathId",
                principalTable: "LearningPaths",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LearningModuleLocalizations_LearningModules_ModuleId",
                table: "LearningModuleLocalizations",
                column: "ModuleId",
                principalTable: "LearningModules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LearningModules_LearningPathRevisions_RevisionId",
                table: "LearningModules",
                column: "RevisionId",
                principalTable: "LearningPathRevisions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LearningPathLocalizations_LearningPaths_PathId",
                table: "LearningPathLocalizations",
                column: "PathId",
                principalTable: "LearningPaths",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LearningPathRevisions_LearningPaths_PathId",
                table: "LearningPathRevisions",
                column: "PathId",
                principalTable: "LearningPaths",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddColumn<Guid>(
                name: "CohortId",
                table: "AspNetUsers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Cohorts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cohorts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Dashboards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    TopCount = table.Column<int>(type: "integer", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    DisplaySettingsJson = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dashboards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LearnerDailySolveStats",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    SolveCount = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearnerDailySolveStats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LearnerDailySolveStats_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MigrationBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PackageFingerprintSha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    State = table.Column<byte>(type: "smallint", nullable: false),
                    ChallengeCount = table.Column<int>(type: "integer", nullable: false),
                    PathCount = table.Column<int>(type: "integer", nullable: false),
                    WarningCount = table.Column<int>(type: "integer", nullable: false),
                    ErrorCount = table.Column<int>(type: "integer", nullable: false),
                    WarningsJson = table.Column<string>(type: "jsonb", nullable: true),
                    ErrorsJson = table.Column<string>(type: "jsonb", nullable: true),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MigrationBatches", x => x.Id);
                    table.CheckConstraint("CK_MigrationBatches_State", "\"State\" BETWEEN 0 AND 3");
                });

            migrationBuilder.CreateTable(
                name: "DashboardTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DashboardId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevokedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastUsedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DashboardTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DashboardTokens_Dashboards_DashboardId",
                        column: x => x.DashboardId,
                        principalTable: "Dashboards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LegacyChallengeMaps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SourceId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ChallengeId = table.Column<Guid>(type: "uuid", nullable: false),
                    MigrationBatchId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegacyChallengeMaps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LegacyChallengeMaps_Challenges_ChallengeId",
                        column: x => x.ChallengeId,
                        principalTable: "Challenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LegacyChallengeMaps_MigrationBatches_MigrationBatchId",
                        column: x => x.MigrationBatchId,
                        principalTable: "MigrationBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "LegacyPathMaps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SourceId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PathId = table.Column<Guid>(type: "uuid", nullable: false),
                    MigrationBatchId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegacyPathMaps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LegacyPathMaps_LearningPaths_PathId",
                        column: x => x.PathId,
                        principalTable: "LearningPaths",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LegacyPathMaps_MigrationBatches_MigrationBatchId",
                        column: x => x.MigrationBatchId,
                        principalTable: "MigrationBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_CohortId",
                table: "AspNetUsers",
                column: "CohortId");

            migrationBuilder.CreateIndex(
                name: "IX_Cohorts_Name",
                table: "Cohorts",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DashboardTokens_DashboardId",
                table: "DashboardTokens",
                column: "DashboardId");

            migrationBuilder.CreateIndex(
                name: "IX_DashboardTokens_TokenHash",
                table: "DashboardTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LearnerDailySolveStats_UserId_Date",
                table: "LearnerDailySolveStats",
                columns: new[] { "UserId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LegacyChallengeMaps_ChallengeId",
                table: "LegacyChallengeMaps",
                column: "ChallengeId");

            migrationBuilder.CreateIndex(
                name: "IX_LegacyChallengeMaps_MigrationBatchId",
                table: "LegacyChallengeMaps",
                column: "MigrationBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_LegacyChallengeMaps_SourceType_SourceId",
                table: "LegacyChallengeMaps",
                columns: new[] { "SourceType", "SourceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LegacyPathMaps_MigrationBatchId",
                table: "LegacyPathMaps",
                column: "MigrationBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_LegacyPathMaps_PathId",
                table: "LegacyPathMaps",
                column: "PathId");

            migrationBuilder.CreateIndex(
                name: "IX_LegacyPathMaps_SourceType_SourceId",
                table: "LegacyPathMaps",
                columns: new[] { "SourceType", "SourceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MigrationBatches_PackageFingerprintSha256",
                table: "MigrationBatches",
                column: "PackageFingerprintSha256",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Cohorts_CohortId",
                table: "AspNetUsers",
                column: "CohortId",
                principalTable: "Cohorts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddColumn<string>(
                name: "RuntimeConfigurationJson",
                table: "Challenges",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Cohorts_CohortId",
                table: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "DashboardTokens");

            migrationBuilder.DropTable(
                name: "LearnerDailySolveStats");

            migrationBuilder.DropTable(
                name: "LegacyChallengeMaps");

            migrationBuilder.DropTable(
                name: "LegacyPathMaps");

            migrationBuilder.DropTable(
                name: "Cohorts");

            migrationBuilder.DropTable(
                name: "Dashboards");

            migrationBuilder.DropTable(
                name: "MigrationBatches");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_CohortId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CohortId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "RuntimeConfigurationJson",
                table: "Challenges");

            migrationBuilder.DropForeignKey(
                name: "FK_LearningPathRevisions_LearningPaths_PathId",
                table: "LearningPathRevisions");

            migrationBuilder.DropTable(
                name: "ChallengeFlags");

            migrationBuilder.DropTable(
                name: "ChallengeHints");

            migrationBuilder.DropTable(
                name: "ChallengeLocalizations");

            migrationBuilder.DropTable(
                name: "ChallengeProgress");

            migrationBuilder.DropTable(
                name: "ChallengeWriteups");

            migrationBuilder.DropTable(
                name: "Enrollments");

            migrationBuilder.DropTable(
                name: "LearningModuleLocalizations");

            migrationBuilder.DropTable(
                name: "LearningPathLocalizations");

            migrationBuilder.DropTable(
                name: "LessonLocalizations");

            migrationBuilder.DropTable(
                name: "LessonProgress");

            migrationBuilder.DropTable(
                name: "ModuleItems");

            migrationBuilder.DropTable(
                name: "Challenges");

            migrationBuilder.DropTable(
                name: "LearningModules");

            migrationBuilder.DropTable(
                name: "Lessons");

            migrationBuilder.DropTable(
                name: "LearningPaths");

            migrationBuilder.DropTable(
                name: "LearningPathRevisions");
        }
    }
}
