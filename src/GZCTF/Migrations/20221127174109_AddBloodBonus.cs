using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GZCTF.Migrations
{
    /// <inheritdoc />
    public partial class AddBloodBonus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "BloodBonus",
                table: "Games",
                type: "bigint",
                nullable: false,
                defaultValue: 52459530L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BloodBonus",
                table: "Games");
        }
    }
}
