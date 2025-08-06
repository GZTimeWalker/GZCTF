using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GZCTF.Migrations
{
    /// <inheritdoc />
    public partial class AddApiToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApiTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, comment: "The unique identifier for the token."),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false, comment: "A user-friendly name for the token."),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: false, comment: "The ID of the user who created the token."),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "The timestamp when the token was created."),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "The timestamp when the token expires. A null value means it never expires."),
                    LastUsedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "The timestamp when the token was last used."),
                    IsRevoked = table.Column<bool>(type: "boolean", nullable: false, comment: "Indicates whether the token has been revoked.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApiTokens_AspNetUsers_CreatorId",
                        column: x => x.CreatorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Stores API tokens for programmatic access.");

            migrationBuilder.CreateIndex(
                name: "IX_ApiTokens_CreatorId",
                table: "ApiTokens",
                column: "CreatorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApiTokens");
        }
    }
}
