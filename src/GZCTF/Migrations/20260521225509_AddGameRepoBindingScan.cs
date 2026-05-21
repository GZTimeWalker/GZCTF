using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GZCTF.Migrations
{
    /// <inheritdoc />
    public partial class AddGameRepoBindingScan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GameRepoBindingScans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BindingId = table.Column<int>(type: "integer", nullable: false),
                    RanAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CommitSha = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    GamesCreated = table.Column<int>(type: "integer", nullable: false),
                    GamesUpdated = table.Column<int>(type: "integer", nullable: false),
                    ChallengesImported = table.Column<int>(type: "integer", nullable: false),
                    ChallengesUpdated = table.Column<int>(type: "integer", nullable: false),
                    Failures = table.Column<int>(type: "integer", nullable: false),
                    Messages = table.Column<string>(type: "character varying(32768)", maxLength: 32768, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameRepoBindingScans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameRepoBindingScans_GameRepoBindings_BindingId",
                        column: x => x.BindingId,
                        principalTable: "GameRepoBindings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GameRepoBindingScans_BindingId_RanAtUtc",
                table: "GameRepoBindingScans",
                columns: new[] { "BindingId", "RanAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameRepoBindingScans");
        }
    }
}
