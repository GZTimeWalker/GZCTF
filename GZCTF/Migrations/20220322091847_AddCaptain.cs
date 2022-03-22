using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CTFServer.Migrations
{
    public partial class AddCaptain : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CaptainId",
                table: "Teams",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_CaptainId",
                table: "Teams",
                column: "CaptainId");

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_AspNetUsers_CaptainId",
                table: "Teams",
                column: "CaptainId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Teams_AspNetUsers_CaptainId",
                table: "Teams");

            migrationBuilder.DropIndex(
                name: "IX_Teams_CaptainId",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "CaptainId",
                table: "Teams");
        }
    }
}
