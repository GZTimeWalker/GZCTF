using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CTFServer.Migrations
{
    public partial class UpdateDeleteBehavior : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Instances_Containers_ContainerId",
                table: "Instances");

            migrationBuilder.AddForeignKey(
                name: "FK_Instances_Containers_ContainerId",
                table: "Instances",
                column: "ContainerId",
                principalTable: "Containers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Instances_Containers_ContainerId",
                table: "Instances");

            migrationBuilder.AddForeignKey(
                name: "FK_Instances_Containers_ContainerId",
                table: "Instances",
                column: "ContainerId",
                principalTable: "Containers",
                principalColumn: "Id");
        }
    }
}
