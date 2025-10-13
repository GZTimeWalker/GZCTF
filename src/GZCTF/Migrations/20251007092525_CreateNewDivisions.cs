using System;
using System.Net;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GZCTF.Migrations
{
    /// <inheritdoc />
    public partial class CreateNewDivisions : Migration
    {
        private void AlterTextToIPAddress(MigrationBuilder migrationBuilder, string table, string column, bool nullable)
        {
            var tempColumn = $"Temp{column}";

            // make a temp column
            migrationBuilder.AddColumn<IPAddress>(
                name: tempColumn,
                table: table,
                type: "inet",
                nullable: nullable);

            if (nullable)
            {
                migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION try_cast_inet(input_text TEXT)
                RETURNS INET AS $$
                BEGIN
                    RETURN input_text::inet;
                EXCEPTION WHEN OTHERS THEN
                    RETURN NULL;
                END; $$ LANGUAGE plpgsql;");
            }
            else
            {
                migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION try_cast_inet(input_text TEXT)
                RETURNS INET AS $$
                BEGIN
                    RETURN input_text::inet;
                EXCEPTION WHEN OTHERS THEN
                    RETURN '::'::inet;
                END; $$ LANGUAGE plpgsql;");
            }

            migrationBuilder.Sql($"UPDATE \"{table}\" SET \"{tempColumn}\" = try_cast_inet(\"{column}\") WHERE \"{column}\" IS NOT NULL;");
            migrationBuilder.Sql("DROP FUNCTION try_cast_inet(TEXT);");

            migrationBuilder.DropColumn(
                name: column,
                table: table);

            migrationBuilder.RenameColumn(
                name: tempColumn,
                table: table,
                newName: column);
        }

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            AlterTextToIPAddress(migrationBuilder, "Logs", "RemoteIP", true);

            AlterTextToIPAddress(migrationBuilder, "AspNetUsers", "IP", false);

            migrationBuilder.AddColumn<int>(
                name: "DivisionId",
                table: "Participations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeadlineUtc",
                table: "GameChallenges",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeadlineUtc",
                table: "ExerciseChallenges",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Divisions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GameId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(31)", maxLength: 31, nullable: false),
                    InviteCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    DefaultPermissions = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Divisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Divisions_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DivisionChallengeConfig",
                columns: table => new
                {
                    DivisionId = table.Column<int>(type: "integer", nullable: false),
                    ChallengeId = table.Column<int>(type: "integer", nullable: false),
                    Permissions = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DivisionChallengeConfig", x => new { x.ChallengeId, x.DivisionId });
                    table.ForeignKey(
                        name: "FK_DivisionChallengeConfig_Divisions_DivisionId",
                        column: x => x.DivisionId,
                        principalTable: "Divisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DivisionChallengeConfig_GameChallenges_ChallengeId",
                        column: x => x.ChallengeId,
                        principalTable: "GameChallenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Participations_DivisionId",
                table: "Participations",
                column: "DivisionId");

            migrationBuilder.CreateIndex(
                name: "IX_DivisionChallengeConfig_DivisionId",
                table: "DivisionChallengeConfig",
                column: "DivisionId");

            migrationBuilder.CreateIndex(
                name: "IX_Divisions_GameId",
                table: "Divisions",
                column: "GameId");

            migrationBuilder.AddForeignKey(
                name: "FK_Participations_Divisions_DivisionId",
                table: "Participations",
                column: "DivisionId",
                principalTable: "Divisions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Participations_Divisions_DivisionId",
                table: "Participations");

            migrationBuilder.DropTable(
                name: "DivisionChallengeConfig");

            migrationBuilder.DropTable(
                name: "Divisions");

            migrationBuilder.DropIndex(
                name: "IX_Participations_DivisionId",
                table: "Participations");

            migrationBuilder.DropColumn(
                name: "DivisionId",
                table: "Participations");

            migrationBuilder.DropColumn(
                name: "DeadlineUtc",
                table: "GameChallenges");

            migrationBuilder.DropColumn(
                name: "DeadlineUtc",
                table: "ExerciseChallenges");

            migrationBuilder.AlterColumn<string>(
                name: "RemoteIP",
                table: "Logs",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true,
                oldClrType: typeof(IPAddress),
                oldType: "inet",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "IP",
                table: "AspNetUsers",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(IPAddress),
                oldType: "inet");
        }
    }
}
