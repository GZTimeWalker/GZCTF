using GZCTF.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GZCTF.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260927000200_AddChallengeSubmissionLimit")]
public sealed class AddChallengeSubmissionLimit : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.AddColumn<int>(
            name: "SubmissionLimit",
            table: "Challenges",
            type: "integer",
            nullable: false,
            defaultValue: 0);

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropColumn(name: "SubmissionLimit", table: "Challenges");
}
