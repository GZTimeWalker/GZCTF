using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GZCTF.Migrations
{
    /// <inheritdoc />
    public partial class RemoveOldDivisions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Migrate Games.Divisions (JSON array of names) into Divisions table
            migrationBuilder.Sql(@"
                WITH parsed AS (
                    SELECT g.""Id"" AS ""GameId"", v AS ""Name""
                    FROM ""Games"" g
                    CROSS JOIN LATERAL (
                        SELECT x FROM jsonb_array_elements_text(
                            CASE
                                WHEN g.""Divisions"" IS NULL OR btrim(g.""Divisions"") = '' THEN '[]'::jsonb
                                ELSE g.""Divisions""::jsonb
                            END
                        ) AS x
                    ) AS t(v)
                )
                INSERT INTO ""Divisions"" (""GameId"", ""Name"", ""DefaultPermissions"")
                SELECT DISTINCT p.""GameId"", LEFT(p.""Name"", 31), 2147483647
                FROM parsed p
                WHERE NULLIF(btrim(LEFT(p.""Name"", 31)), '') IS NOT NULL
                  AND NOT EXISTS (
                    SELECT 1 FROM ""Divisions"" d
                    WHERE d.""GameId"" = p.""GameId""
                      AND d.""Name"" = LEFT(p.""Name"", 31)
                  );
            ");

            // 2) Migrate Participations.Division (string) into Divisions table if missing
            migrationBuilder.Sql(@"
                INSERT INTO ""Divisions"" (""GameId"", ""Name"", ""DefaultPermissions"")
                SELECT DISTINCT p.""GameId"", LEFT(p.""Division"", 31), 2147483647
                FROM ""Participations"" p
                WHERE p.""Division"" IS NOT NULL
                  AND NULLIF(btrim(p.""Division""), '') IS NOT NULL
                  AND NOT EXISTS (
                    SELECT 1 FROM ""Divisions"" d
                    WHERE d.""GameId"" = p.""GameId""
                      AND d.""Name"" = LEFT(p.""Division"", 31)
                  );
            ");

            // 3) Set Participation.DivisionId based on migrated divisions
            migrationBuilder.Sql(@"
                UPDATE ""Participations"" AS p
                SET ""DivisionId"" = d.""Id""
                FROM ""Divisions"" d
                WHERE p.""Division"" IS NOT NULL
                  AND NULLIF(btrim(p.""Division""), '') IS NOT NULL
                  AND d.""GameId"" = p.""GameId""
                  AND d.""Name"" = LEFT(p.""Division"", 31);
            ");

            // 4) Drop legacy columns
            migrationBuilder.DropColumn(
                name: "Division",
                table: "Participations");

            migrationBuilder.DropColumn(
                name: "Divisions",
                table: "Games");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Recreate legacy columns
            migrationBuilder.AddColumn<string>(
                name: "Division",
                table: "Participations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Divisions",
                table: "Games",
                type: "text",
                nullable: true);

            // Repopulate legacy columns from new schema
            // 1) Restore Participations.Division from DivisionId
            migrationBuilder.Sql(@"
                UPDATE ""Participations"" p
                SET ""Division"" = d.""Name""
                FROM ""Divisions"" d
                WHERE p.""DivisionId"" IS NOT NULL
                  AND d.""Id"" = p.""DivisionId"";
            ");

            // 2) Restore Games.Divisions as JSON array of division names per game
            migrationBuilder.Sql(@"
                UPDATE ""Games"" g
                SET ""Divisions"" = (
                    SELECT COALESCE(to_json(array_agg(d.""Name""))::text, '[]')
                    FROM ""Divisions"" d
                    WHERE d.""GameId"" = g.""Id""
                );
            ");
        }
    }
}
