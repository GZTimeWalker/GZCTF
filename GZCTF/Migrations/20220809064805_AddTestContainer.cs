using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CTFServer.Migrations
{
    public partial class AddTestContainer : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TestContainerId",
                table: "Challenges",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Challenges_TestContainerId",
                table: "Challenges",
                column: "TestContainerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Challenges_Containers_TestContainerId",
                table: "Challenges",
                column: "TestContainerId",
                principalTable: "Containers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Challenges_Containers_TestContainerId",
                table: "Challenges");

            migrationBuilder.DropIndex(
                name: "IX_Challenges_TestContainerId",
                table: "Challenges");

            migrationBuilder.DropColumn(
                name: "TestContainerId",
                table: "Challenges");
        }
    }
}
