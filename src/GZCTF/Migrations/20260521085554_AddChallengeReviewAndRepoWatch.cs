using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GZCTF.Migrations
{
    /// <inheritdoc />
    public partial class AddChallengeReviewAndRepoWatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReviewNote",
                table: "GameChallenges",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "ReviewStatus",
                table: "GameChallenges",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReviewedAtUtc",
                table: "GameChallenges",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SubmittedAtUtc",
                table: "GameChallenges",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SubmittedByUserId",
                table: "GameChallenges",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewNote",
                table: "ExerciseChallenges",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "ReviewStatus",
                table: "ExerciseChallenges",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReviewedAtUtc",
                table: "ExerciseChallenges",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SubmittedAtUtc",
                table: "ExerciseChallenges",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SubmittedByUserId",
                table: "ExerciseChallenges",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RepoWatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GameId = table.Column<int>(type: "integer", nullable: false),
                    RepoUrl = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Ref = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Subpath = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    IntervalSeconds = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<byte>(type: "smallint", nullable: false),
                    NextRunUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastRunUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastCommitSha = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepoWatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepoWatches_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RepoWatchSyncs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RepoWatchId = table.Column<int>(type: "integer", nullable: false),
                    RanAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CommitSha = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Imported = table.Column<int>(type: "integer", nullable: false),
                    Updated = table.Column<int>(type: "integer", nullable: false),
                    Skipped = table.Column<int>(type: "integer", nullable: false),
                    Failed = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepoWatchSyncs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepoWatchSyncs_RepoWatches_RepoWatchId",
                        column: x => x.RepoWatchId,
                        principalTable: "RepoWatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RepoWatches_GameId",
                table: "RepoWatches",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_RepoWatches_NextRunUtc_Status",
                table: "RepoWatches",
                columns: new[] { "NextRunUtc", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_RepoWatchSyncs_RepoWatchId_RanAtUtc",
                table: "RepoWatchSyncs",
                columns: new[] { "RepoWatchId", "RanAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RepoWatchSyncs");

            migrationBuilder.DropTable(
                name: "RepoWatches");

            migrationBuilder.DropColumn(
                name: "ReviewNote",
                table: "GameChallenges");

            migrationBuilder.DropColumn(
                name: "ReviewStatus",
                table: "GameChallenges");

            migrationBuilder.DropColumn(
                name: "ReviewedAtUtc",
                table: "GameChallenges");

            migrationBuilder.DropColumn(
                name: "SubmittedAtUtc",
                table: "GameChallenges");

            migrationBuilder.DropColumn(
                name: "SubmittedByUserId",
                table: "GameChallenges");

            migrationBuilder.DropColumn(
                name: "ReviewNote",
                table: "ExerciseChallenges");

            migrationBuilder.DropColumn(
                name: "ReviewStatus",
                table: "ExerciseChallenges");

            migrationBuilder.DropColumn(
                name: "ReviewedAtUtc",
                table: "ExerciseChallenges");

            migrationBuilder.DropColumn(
                name: "SubmittedAtUtc",
                table: "ExerciseChallenges");

            migrationBuilder.DropColumn(
                name: "SubmittedByUserId",
                table: "ExerciseChallenges");
        }
    }
}
