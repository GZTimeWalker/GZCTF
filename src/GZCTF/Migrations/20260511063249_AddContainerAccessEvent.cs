using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GZCTF.Migrations
{
    /// <inheritdoc />
    public partial class AddContainerAccessEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContainerAccessEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GameId = table.Column<int>(type: "integer", nullable: false),
                    ChallengeId = table.Column<int>(type: "integer", nullable: false),
                    ContainerOwnerParticipationId = table.Column<int>(type: "integer", nullable: false),
                    ContainerId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccessingUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    AccessingUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    AccessingParticipationId = table.Column<int>(type: "integer", nullable: true),
                    RemoteIp = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    UserAgent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ConnectedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContainerAccessEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContainerAccessEvents_GameChallenges_ChallengeId",
                        column: x => x.ChallengeId,
                        principalTable: "GameChallenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContainerAccessEvents_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContainerAccessEvents_AccessingUserId_ChallengeId",
                table: "ContainerAccessEvents",
                columns: new[] { "AccessingUserId", "ChallengeId" });

            migrationBuilder.CreateIndex(
                name: "IX_ContainerAccessEvents_ChallengeId_ConnectedAtUtc",
                table: "ContainerAccessEvents",
                columns: new[] { "ChallengeId", "ConnectedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ContainerAccessEvents_GameId_ConnectedAtUtc",
                table: "ContainerAccessEvents",
                columns: new[] { "GameId", "ConnectedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContainerAccessEvents");
        }
    }
}
