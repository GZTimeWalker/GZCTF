using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GZCTF.Migrations
{
    /// <inheritdoc />
    public partial class AddSkillTrees : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing lessons are already live content, so backfill them as Published (1)
            // and keep Draft (0) as the default for future rows.
            migrationBuilder.AddColumn<byte>(
                name: "PublicationState",
                table: "Lessons",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)1);

            migrationBuilder.AlterColumn<byte>(
                name: "PublicationState",
                table: "Lessons",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);

            // Lessons gain a concurrency token. Npgsql maps it to the xmin system column,
            // which the SQL generator omits because every PostgreSQL table already has it.
            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Lessons",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.CreateTable(
                name: "SkillCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Summary = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    IconKey = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    MergedIntoCategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillCategories", x => x.Id);
                    table.CheckConstraint("CK_SkillCategories_IconKey", "\"IconKey\" IN ('flag','web','crypto','pwn','brain','ai')");
                    table.ForeignKey(
                        name: "FK_SkillCategories_SkillCategories_MergedIntoCategoryId",
                        column: x => x.MergedIntoCategoryId,
                        principalTable: "SkillCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CategoryContents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    LessonId = table.Column<Guid>(type: "uuid", nullable: true),
                    ChallengeId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoryContents", x => x.Id);
                    table.CheckConstraint("CK_CategoryContents_ExactlyOneContent", "(\"LessonId\" IS NOT NULL AND \"ChallengeId\" IS NULL) OR (\"LessonId\" IS NULL AND \"ChallengeId\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_CategoryContents_Challenges_ChallengeId",
                        column: x => x.ChallengeId,
                        principalTable: "Challenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CategoryContents_Lessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "Lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CategoryContents_SkillCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "SkillCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LearningPathRedirects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LearningPathId = table.Column<Guid>(type: "uuid", nullable: false),
                    OldSlug = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SkillTreeId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningPathRedirects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SkillTreeCategoryRefs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillTreeCategoryRefs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SkillTreeCategoryRefs_SkillCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "SkillCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SkillTreeEnrollments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkillTreeId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsCurrent = table.Column<bool>(type: "boolean", nullable: false),
                    EnrolledAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillTreeEnrollments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SkillTreeEnrollments_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SkillTreeRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SkillTreeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<byte>(type: "smallint", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PublishedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillTreeRevisions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SkillTrees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Summary = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    IconKey = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CurrentPublishedRevisionId = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillTrees", x => x.Id);
                    table.CheckConstraint("CK_SkillTrees_IconKey", "\"IconKey\" IN ('flag','web','crypto','pwn','brain','ai')");
                    table.ForeignKey(
                        name: "FK_SkillTrees_SkillTreeRevisions_CurrentPublishedRevisionId",
                        column: x => x.CurrentPublishedRevisionId,
                        principalTable: "SkillTreeRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CategoryContents_CategoryId_ChallengeId",
                table: "CategoryContents",
                columns: new[] { "CategoryId", "ChallengeId" },
                unique: true,
                filter: "\"ChallengeId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryContents_CategoryId_LessonId",
                table: "CategoryContents",
                columns: new[] { "CategoryId", "LessonId" },
                unique: true,
                filter: "\"LessonId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryContents_CategoryId_SortOrder",
                table: "CategoryContents",
                columns: new[] { "CategoryId", "SortOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CategoryContents_ChallengeId",
                table: "CategoryContents",
                column: "ChallengeId");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryContents_LessonId",
                table: "CategoryContents",
                column: "LessonId");

            migrationBuilder.CreateIndex(
                name: "IX_LearningPathRedirects_LearningPathId",
                table: "LearningPathRedirects",
                column: "LearningPathId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LearningPathRedirects_OldSlug",
                table: "LearningPathRedirects",
                column: "OldSlug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LearningPathRedirects_SkillTreeId",
                table: "LearningPathRedirects",
                column: "SkillTreeId");

            migrationBuilder.CreateIndex(
                name: "IX_SkillCategories_DeletedAtUtc",
                table: "SkillCategories",
                column: "DeletedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SkillCategories_MergedIntoCategoryId",
                table: "SkillCategories",
                column: "MergedIntoCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_SkillTreeCategoryRefs_CategoryId",
                table: "SkillTreeCategoryRefs",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_SkillTreeCategoryRefs_RevisionId_CategoryId",
                table: "SkillTreeCategoryRefs",
                columns: new[] { "RevisionId", "CategoryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SkillTreeCategoryRefs_RevisionId_SortOrder",
                table: "SkillTreeCategoryRefs",
                columns: new[] { "RevisionId", "SortOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SkillTreeEnrollments_SkillTreeId",
                table: "SkillTreeEnrollments",
                column: "SkillTreeId");

            migrationBuilder.CreateIndex(
                name: "IX_SkillTreeEnrollments_UserId",
                table: "SkillTreeEnrollments",
                column: "UserId",
                unique: true,
                filter: "\"IsCurrent\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_SkillTreeEnrollments_UserId_SkillTreeId",
                table: "SkillTreeEnrollments",
                columns: new[] { "UserId", "SkillTreeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SkillTreeRevisions_SkillTreeId",
                table: "SkillTreeRevisions",
                column: "SkillTreeId",
                unique: true,
                filter: "\"Status\" = 0");

            migrationBuilder.CreateIndex(
                name: "IX_SkillTrees_CurrentPublishedRevisionId",
                table: "SkillTrees",
                column: "CurrentPublishedRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_SkillTrees_DeletedAtUtc",
                table: "SkillTrees",
                column: "DeletedAtUtc");

            migrationBuilder.AddForeignKey(
                name: "FK_LearningPathRedirects_SkillTrees_SkillTreeId",
                table: "LearningPathRedirects",
                column: "SkillTreeId",
                principalTable: "SkillTrees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SkillTreeCategoryRefs_SkillTreeRevisions_RevisionId",
                table: "SkillTreeCategoryRefs",
                column: "RevisionId",
                principalTable: "SkillTreeRevisions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SkillTreeEnrollments_SkillTrees_SkillTreeId",
                table: "SkillTreeEnrollments",
                column: "SkillTreeId",
                principalTable: "SkillTrees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SkillTreeRevisions_SkillTrees_SkillTreeId",
                table: "SkillTreeRevisions",
                column: "SkillTreeId",
                principalTable: "SkillTrees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SkillTreeRevisions_SkillTrees_SkillTreeId",
                table: "SkillTreeRevisions");

            migrationBuilder.DropTable(
                name: "CategoryContents");

            migrationBuilder.DropTable(
                name: "LearningPathRedirects");

            migrationBuilder.DropTable(
                name: "SkillTreeCategoryRefs");

            migrationBuilder.DropTable(
                name: "SkillTreeEnrollments");

            migrationBuilder.DropTable(
                name: "SkillCategories");

            migrationBuilder.DropTable(
                name: "SkillTrees");

            migrationBuilder.DropTable(
                name: "SkillTreeRevisions");

            migrationBuilder.DropColumn(
                name: "PublicationState",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Lessons");
        }
    }
}
