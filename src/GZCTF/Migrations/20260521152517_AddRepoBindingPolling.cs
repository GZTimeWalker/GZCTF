using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GZCTF.Migrations
{
    /// <inheritdoc />
    public partial class AddRepoBindingPolling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IntervalSeconds",
                table: "GameRepoBindings",
                type: "integer",
                nullable: false,
                defaultValue: 600);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "NextScanUtc",
                table: "GameRepoBindings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "Status",
                table: "GameRepoBindings",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);

            // Backfill explicitly — defaultValue applies to new rows but
            // EF on PG occasionally leaves existing rows at the type
            // default (0) instead of the configured default. Belt and
            // suspenders so existing bindings don't show "0s" intervals.
            migrationBuilder.Sql(
                "UPDATE \"GameRepoBindings\" SET \"IntervalSeconds\" = 600 " +
                "WHERE \"IntervalSeconds\" = 0 OR \"IntervalSeconds\" IS NULL;");

            migrationBuilder.CreateIndex(
                name: "IX_GameRepoBindings_NextScanUtc_Status",
                table: "GameRepoBindings",
                columns: new[] { "NextScanUtc", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GameRepoBindings_NextScanUtc_Status",
                table: "GameRepoBindings");

            migrationBuilder.DropColumn(
                name: "IntervalSeconds",
                table: "GameRepoBindings");

            migrationBuilder.DropColumn(
                name: "NextScanUtc",
                table: "GameRepoBindings");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "GameRepoBindings");
        }
    }
}
