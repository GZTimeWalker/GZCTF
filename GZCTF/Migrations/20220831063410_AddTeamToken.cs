using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CTFServer.Migrations
{
    public partial class AddTeamToken : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Score",
                table: "Participations");

            migrationBuilder.AddColumn<string>(
                name: "Token",
                table: "Participations",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Score",
                table: "Instances",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Token",
                table: "Participations");

            migrationBuilder.DropColumn(
                name: "Score",
                table: "Instances");

            migrationBuilder.AddColumn<int>(
                name: "Score",
                table: "Participations",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
