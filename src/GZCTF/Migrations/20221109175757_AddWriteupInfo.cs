using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GZCTF.Migrations
{
    public partial class AddWriteupInfo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WriteupId",
                table: "Participations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "WriteupDeadline",
                table: "Games",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.CreateIndex(
                name: "IX_Participations_WriteupId",
                table: "Participations",
                column: "WriteupId");

            migrationBuilder.AddForeignKey(
                name: "FK_Participations_Files_WriteupId",
                table: "Participations",
                column: "WriteupId",
                principalTable: "Files",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Participations_Files_WriteupId",
                table: "Participations");

            migrationBuilder.DropIndex(
                name: "IX_Participations_WriteupId",
                table: "Participations");

            migrationBuilder.DropColumn(
                name: "WriteupId",
                table: "Participations");

            migrationBuilder.DropColumn(
                name: "WriteupDeadline",
                table: "Games");
        }
    }
}
