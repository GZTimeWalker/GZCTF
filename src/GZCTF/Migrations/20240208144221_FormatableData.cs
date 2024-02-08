using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GZCTF.Migrations
{
    /// <inheritdoc />
    public partial class FormatableData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Content",
                table: "GameNotices",
                newName: "Values");

            migrationBuilder.RenameColumn(
                name: "Content",
                table: "GameEvents",
                newName: "Values");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Values",
                table: "GameNotices",
                newName: "Content");

            migrationBuilder.RenameColumn(
                name: "Values",
                table: "GameEvents",
                newName: "Content");
        }
    }
}
