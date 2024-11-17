using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GZCTF.Migrations
{
    /// <inheritdoc />
    public partial class RenameOrgToDiv : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Organization",
                table: "Participations",
                newName: "Division");

            migrationBuilder.RenameColumn(
                name: "Organizations",
                table: "Games",
                newName: "Divisions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Division",
                table: "Participations",
                newName: "Organization");

            migrationBuilder.RenameColumn(
                name: "Divisions",
                table: "Games",
                newName: "Organizations");
        }
    }
}
