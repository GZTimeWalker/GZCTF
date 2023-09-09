using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GZCTF.Migrations
{
    /// <inheritdoc />
    public partial class AdjustNamingConventions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PublicIP",
                table: "Containers",
                newName: "PublicIp");

            migrationBuilder.RenameColumn(
                name: "IP",
                table: "Containers",
                newName: "Ip");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PublicIp",
                table: "Containers",
                newName: "PublicIP");

            migrationBuilder.RenameColumn(
                name: "Ip",
                table: "Containers",
                newName: "IP");
        }
    }
}
