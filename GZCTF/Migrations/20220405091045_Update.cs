using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CTFServer.Migrations
{
    public partial class Update : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Answer",
                table: "Challenges");

            migrationBuilder.RenameColumn(
                name: "Url",
                table: "FlagContexts",
                newName: "RemoteUrl");

            migrationBuilder.AddColumn<bool>(
                name: "IsEnabled",
                table: "Challenges",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsEnabled",
                table: "Challenges");

            migrationBuilder.RenameColumn(
                name: "RemoteUrl",
                table: "FlagContexts",
                newName: "Url");

            migrationBuilder.AddColumn<string>(
                name: "Answer",
                table: "Challenges",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
