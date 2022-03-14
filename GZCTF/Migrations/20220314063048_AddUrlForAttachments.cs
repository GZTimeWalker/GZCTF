using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CTFServer.Migrations
{
    public partial class AddUrlForAttachments : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClientJS",
                table: "Challenges");

            migrationBuilder.AddColumn<string>(
                name: "Url",
                table: "FlagContexts",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<byte>(
                name: "Type",
                table: "Challenges",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Url",
                table: "FlagContexts");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Challenges");

            migrationBuilder.AddColumn<string>(
                name: "ClientJS",
                table: "Challenges",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
