using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GZCTF.Migrations
{
    /// <inheritdoc />
    public partial class AddSuspicionSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SuspicionScore",
                table: "Participations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "SuspicionEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ParticipationId = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    ScoreDelta = table.Column<int>(type: "integer", nullable: false),
                    Details = table.Column<string>(type: "text", nullable: false),
                    TimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuspicionEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SuspicionEvents_Participations_ParticipationId",
                        column: x => x.ParticipationId,
                        principalTable: "Participations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SuspicionRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RuleCode = table.Column<string>(type: "text", nullable: false),
                    Weight = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuspicionRules", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SuspicionEvents_ParticipationId",
                table: "SuspicionEvents",
                column: "ParticipationId");

            migrationBuilder.CreateIndex(
                name: "IX_SuspicionRules_RuleCode",
                table: "SuspicionRules",
                column: "RuleCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SuspicionEvents");

            migrationBuilder.DropTable(
                name: "SuspicionRules");

            migrationBuilder.DropColumn(
                name: "SuspicionScore",
                table: "Participations");
        }
    }
}
