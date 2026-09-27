using GZCTF.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GZCTF.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260927000100_AddChallengeCtfCategory")]
public sealed class AddChallengeCtfCategory : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<byte>(
            name: "CtfCategory",
            table: "Challenges",
            type: "smallint",
            nullable: false,
            defaultValue: (byte)0);

        migrationBuilder.Sql("""
            UPDATE "Challenges"
            SET "CtfCategory" = CASE lower("SourceMetadataJson"->>'category')
                WHEN 'crypto' THEN 1 WHEN 'pwn' THEN 2 WHEN 'web' THEN 3
                WHEN 'reverse' THEN 4 WHEN 'blockchain' THEN 5
                WHEN 'forensics' THEN 6 WHEN 'hardware' THEN 7
                WHEN 'mobile' THEN 8 WHEN 'ppc' THEN 9 WHEN 'ai' THEN 10
                WHEN 'pentest' THEN 11 WHEN 'osint' THEN 12 ELSE 0 END
            WHERE "SourceMetadataJson" ? 'category';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropColumn(name: "CtfCategory", table: "Challenges");
}
