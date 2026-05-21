using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GZCTF.Migrations
{
    /// <inheritdoc />
    public partial class AddTokenStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "TokenStatus",
                table: "RepoWatches",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<byte>(
                name: "TokenStatus",
                table: "GameRepoBindings",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);

            // Backfill: rows that have a stored token are presumed Ok
            // (the next poller tick will set DecryptFailed if it can't
            // unprotect the ciphertext). Rows without a token stay
            // NotConfigured (the default).
            migrationBuilder.Sql(
                "UPDATE \"RepoWatches\" SET \"TokenStatus\" = 1 WHERE \"GitHubTokenEncrypted\" IS NOT NULL;");
            migrationBuilder.Sql(
                "UPDATE \"GameRepoBindings\" SET \"TokenStatus\" = 1 WHERE \"GitHubTokenEncrypted\" IS NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TokenStatus",
                table: "RepoWatches");

            migrationBuilder.DropColumn(
                name: "TokenStatus",
                table: "GameRepoBindings");
        }
    }
}
