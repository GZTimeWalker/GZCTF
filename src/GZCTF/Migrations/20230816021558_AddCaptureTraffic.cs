using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GZCTF.Migrations
{
    /// <inheritdoc />
    public partial class AddTrafficCapture : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrivilegedContainer",
                table: "Challenges");

            migrationBuilder.AddColumn<bool>(
                name: "EnableTrafficCapture",
                table: "Challenges",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EnableTrafficCapture",
                table: "Challenges");

            migrationBuilder.AddColumn<bool>(
                name: "PrivilegedContainer",
                table: "Challenges",
                type: "boolean",
                nullable: true);
        }
    }
}
