using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GZCTF.Migrations
{
    /// <inheritdoc />
    public partial class AddUserMetadataField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            AlterTextToJsonb(migrationBuilder, "Posts", "Tags", true);
            AlterTextToJsonb(migrationBuilder, "GameNotices", "Values", false);
            AlterTextToJsonb(migrationBuilder, "GameEvents", "Values", false);
            AlterTextToJsonb(migrationBuilder, "GameChallenges", "Hints", true);
            AlterTextToJsonb(migrationBuilder, "ExerciseChallenges", "Tags", true);
            AlterTextToJsonb(migrationBuilder, "ExerciseChallenges", "Hints", true);

            migrationBuilder.AlterColumn<string>(
                name: "Image",
                table: "Containers",
                type: "character varying(400)",
                maxLength: 400,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "IP",
                table: "Containers",
                type: "character varying(45)",
                maxLength: 45,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "ContainerId",
                table: "Containers",
                type: "character varying(127)",
                maxLength: 127,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.RenameColumn(
                name: "PublicIP",
                table: "Containers",
                newName: "PublicHost");

            migrationBuilder.AlterColumn<string>(
                name: "PublicHost",
                table: "Containers",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "Metadata",
                table: "AspNetUsers",
                type: "jsonb",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UserMetadataFields",
                columns: table => new
                {
                    Key = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Type = table.Column<byte>(type: "smallint", nullable: false, defaultValue: (byte)0),
                    Required = table.Column<bool>(type: "boolean", nullable: false),
                    Hidden = table.Column<bool>(type: "boolean", nullable: false),
                    Locked = table.Column<bool>(type: "boolean", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    DefaultValue = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    MaxLength = table.Column<int>(type: "integer", nullable: true),
                    MinValue = table.Column<int>(type: "integer", nullable: true),
                    MaxValue = table.Column<int>(type: "integer", nullable: true),
                    Options = table.Column<string>(type: "jsonb", nullable: true),
                    Pattern = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserMetadataFields", x => x.Key);
                });

            string realNameDisplay = StaticLocalizer[nameof(Resources.Program.Header_RealName)];
            string stdNumberDisplay = StaticLocalizer[nameof(Resources.Program.Header_StdNumber)];

            migrationBuilder.InsertData(
                table: "UserMetadataFields",
                columns: ["Key", "DisplayName", "Type", "Required", "Hidden", "Locked", "Order", "DefaultValue", "MaxLength"],
                values: ["realName", realNameDisplay, 0, false, false, false, 0, "\"\"", 128]);

            migrationBuilder.InsertData(
                table: "UserMetadataFields",
                columns: ["Key", "DisplayName", "Type", "Required", "Hidden", "Locked", "Order", "DefaultValue", "MaxLength"],
                values: ["stdNumber", stdNumberDisplay, 0, false, false, false, 1, "\"\"", 64]);

            // Step 4: Migrate existing RealName and StdNumber into Metadata JSON column
            migrationBuilder.Sql(@"
                UPDATE ""AspNetUsers""
                SET ""Metadata"" = jsonb_build_object(
                    'realName', COALESCE(""RealName"", ''),
                    'stdNumber', COALESCE(""StdNumber"", '')
                )
                WHERE ""RealName"" IS NOT NULL OR ""StdNumber"" IS NOT NULL;
            ");

            migrationBuilder.DropColumn(
                name: "RealName",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "StdNumber",
                table: "AspNetUsers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserMetadataFields");

            migrationBuilder.DropColumn(
                name: "Metadata",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<string>(
                name: "Tags",
                table: "Posts",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Participations",
                type: "integer",
                nullable: false,
                oldClrType: typeof(byte),
                oldType: "smallint");

            migrationBuilder.AlterColumn<string>(
                name: "Values",
                table: "GameNotices",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<string>(
                name: "Values",
                table: "GameEvents",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<string>(
                name: "Hints",
                table: "GameChallenges",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Tags",
                table: "ExerciseChallenges",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Hints",
                table: "ExerciseChallenges",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Image",
                table: "Containers",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(400)",
                oldMaxLength: 400);

            migrationBuilder.AlterColumn<string>(
                name: "IP",
                table: "Containers",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(45)",
                oldMaxLength: 45);

            migrationBuilder.AlterColumn<string>(
                name: "ContainerId",
                table: "Containers",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(127)",
                oldMaxLength: 127);

            migrationBuilder.AlterColumn<string>(
                name: "PublicHost",
                table: "Containers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.RenameColumn(
                name: "PublicHost",
                table: "Containers",
                newName: "PublicIP");

            migrationBuilder.AddColumn<string>(
                name: "RealName",
                table: "AspNetUsers",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StdNumber",
                table: "AspNetUsers",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");
        }

        private void AlterTextToJsonb(MigrationBuilder migrationBuilder, string table, string column, bool nullable)
        {
            var tempColumn = $"Temp{column}";

            // make a temp column (must be nullable to avoid failure on existing rows)
            migrationBuilder.AddColumn<string>(
                name: tempColumn,
                table: table,
                type: "jsonb",
                nullable: true);

            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION try_cast_jsonb(input_text TEXT)
                RETURNS JSONB AS $$
                BEGIN
                    RETURN input_text::jsonb;
                EXCEPTION WHEN OTHERS THEN
                    RETURN NULL;
                END; $$ LANGUAGE plpgsql;");

            if (nullable)
            {
                // For nullable target, just try-cast; invalid values become NULL
                migrationBuilder.Sql($"UPDATE \"{table}\" SET \"{tempColumn}\" = try_cast_jsonb(\"{column}\");");
            }
            else
            {
                // For non-nullable target, coalesce NULL (e.g., original NULLs or invalid JSON) to a default '[]'
                migrationBuilder.Sql($"UPDATE \"{table}\" SET \"{tempColumn}\" = COALESCE(try_cast_jsonb(\"{column}\"), '[]'::jsonb);");
            }

            migrationBuilder.Sql("DROP FUNCTION try_cast_jsonb(TEXT);");

            migrationBuilder.DropColumn(
                name: column,
                table: table);

            migrationBuilder.RenameColumn(
                name: tempColumn,
                table: table,
                newName: column);

            // enforce NOT NULL after data has been migrated
            if (!nullable)
            {
                migrationBuilder.AlterColumn<string>(
                    name: column,
                    table: table,
                    type: "jsonb",
                    nullable: false,
                    oldClrType: typeof(string),
                    oldType: "jsonb",
                    oldNullable: true);
            }
        }
    }
}
