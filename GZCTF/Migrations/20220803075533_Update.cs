using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CTFServer.Migrations
{
    public partial class Update : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Instances_Games_GameId",
                table: "Instances");

            migrationBuilder.DropIndex(
                name: "IX_Instances_GameId",
                table: "Instances");

            migrationBuilder.DropColumn(
                name: "GameId",
                table: "Instances");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GameId",
                table: "Instances",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Instances_GameId",
                table: "Instances",
                column: "GameId");

            migrationBuilder.AddForeignKey(
                name: "FK_Instances_Games_GameId",
                table: "Instances",
                column: "GameId",
                principalTable: "Games",
                principalColumn: "Id");
        }
    }
}
