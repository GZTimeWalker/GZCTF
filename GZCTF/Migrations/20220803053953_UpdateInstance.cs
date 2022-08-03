using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CTFServer.Migrations
{
    public partial class UpdateInstance : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Instances",
                table: "Instances");

            migrationBuilder.DropIndex(
                name: "IX_Instances_ChallengeId",
                table: "Instances");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "Instances");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Instances",
                table: "Instances",
                columns: new[] { "ChallengeId", "ParticipationId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Instances",
                table: "Instances");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "Instances",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Instances",
                table: "Instances",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Instances_ChallengeId",
                table: "Instances",
                column: "ChallengeId");
        }
    }
}
