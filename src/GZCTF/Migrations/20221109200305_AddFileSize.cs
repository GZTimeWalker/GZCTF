using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GZCTF.Migrations
{
    public partial class AddFileSize : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "FileSize",
                table: "Files",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UploadTimeUTC",
                table: "Files",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileSize",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "UploadTimeUTC",
                table: "Files");
        }
    }
}
