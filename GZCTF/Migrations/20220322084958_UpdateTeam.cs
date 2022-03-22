using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CTFServer.Migrations
{
    public partial class UpdateTeam : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Avatar",
                table: "Teams",
                newName: "AvatarHash");

            migrationBuilder.AddColumn<string>(
                name: "InviteToken",
                table: "Teams",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InviteToken",
                table: "Teams");

            migrationBuilder.RenameColumn(
                name: "AvatarHash",
                table: "Teams",
                newName: "Avatar");
        }
    }
}
