using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GZCTF.Migrations
{
    /// <inheritdoc />
    public partial class AddGameAllowUserSubmissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowUserSubmissions",
                table: "Games",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            // Backfill existing rows the EF column-default doesn't reach
            // on some providers; explicit to keep behaviour stable.
            migrationBuilder.Sql("UPDATE \"Games\" SET \"AllowUserSubmissions\" = true;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowUserSubmissions",
                table: "Games");
        }
    }
}
