using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GZCTF.Migrations
{
    /// <inheritdoc />
    public partial class AddGameRepoBinding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EventManifestPath",
                table: "Games",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RepoBindingId",
                table: "Games",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GameRepoBindings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RepoUrl = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Ref = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    GitHubTokenEncrypted = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LastScanUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastCommitSha = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    LastScanMessage = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameRepoBindings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Games_RepoBindingId",
                table: "Games",
                column: "RepoBindingId");

            migrationBuilder.AddForeignKey(
                name: "FK_Games_GameRepoBindings_RepoBindingId",
                table: "Games",
                column: "RepoBindingId",
                principalTable: "GameRepoBindings",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Games_GameRepoBindings_RepoBindingId",
                table: "Games");

            migrationBuilder.DropTable(
                name: "GameRepoBindings");

            migrationBuilder.DropIndex(
                name: "IX_Games_RepoBindingId",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "EventManifestPath",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "RepoBindingId",
                table: "Games");
        }
    }
}
