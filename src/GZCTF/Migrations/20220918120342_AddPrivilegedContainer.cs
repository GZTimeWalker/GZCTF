using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GZCTF.Migrations
{
    public partial class AddPrivilegedContainer : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PrivilegedContainer",
                table: "Challenges",
                type: "boolean",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrivilegedContainer",
                table: "Challenges");
        }
    }
}
