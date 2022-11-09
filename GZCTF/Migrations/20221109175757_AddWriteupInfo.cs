using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CTFServer.Migrations
{
    public partial class AddWriteupInfo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WriteUpId",
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
                name: "IX_Participations_WriteUpId",
                table: "Participations",
                column: "WriteUpId");

            migrationBuilder.AddForeignKey(
                name: "FK_Participations_Files_WriteUpId",
                table: "Participations",
                column: "WriteUpId",
                principalTable: "Files",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Participations_Files_WriteUpId",
                table: "Participations");

            migrationBuilder.DropIndex(
                name: "IX_Participations_WriteUpId",
                table: "Participations");

            migrationBuilder.DropColumn(
                name: "WriteUpId",
                table: "Participations");

            migrationBuilder.DropColumn(
                name: "WriteupDeadline",
                table: "Games");
        }
    }
}
