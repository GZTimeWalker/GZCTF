using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GZCTF.Migrations
{
    /// <inheritdoc />
    public partial class AddOAuthSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserMetadata",
                table: "AspNetUsers",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "OAuthProviders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Key = table.Column<string>(type: "character varying(31)", maxLength: 31, nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    ClientId = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    ClientSecret = table.Column<string>(type: "character varying(800)", maxLength: 800, nullable: false),
                    AuthorizationEndpoint = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    TokenEndpoint = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    UserInformationEndpoint = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Scopes = table.Column<string>(type: "jsonb", nullable: false),
                    FieldMapping = table.Column<string>(type: "jsonb", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OAuthProviders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserMetadataFields",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Key = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Required = table.Column<bool>(type: "boolean", nullable: false),
                    Visible = table.Column<bool>(type: "boolean", nullable: false),
                    Placeholder = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    MaxLength = table.Column<int>(type: "integer", nullable: true),
                    MinValue = table.Column<int>(type: "integer", nullable: true),
                    MaxValue = table.Column<int>(type: "integer", nullable: true),
                    Pattern = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    Options = table.Column<string>(type: "jsonb", nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserMetadataFields", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OAuthProviders_Key",
                table: "OAuthProviders",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserMetadataFields_Key",
                table: "UserMetadataFields",
                column: "Key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OAuthProviders");

            migrationBuilder.DropTable(
                name: "UserMetadataFields");

            migrationBuilder.DropColumn(
                name: "UserMetadata",
                table: "AspNetUsers");
        }
    }
}
