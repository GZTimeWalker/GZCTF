using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CTFServer.Migrations
{
    /// <inheritdoc />
    public partial class UpdateParticipationStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // the string column
            migrationBuilder.RenameColumn(
                name: "Status",
                table: "Participations",
                newName: "Status_Old");

            // the int column
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Participations",
                type: "integer",
                nullable: true);

            // update the enum with the old values
            // Pending = 0,
            // Accepted = 1,
            // Denied = 2,
            // Forfeited = 3,
            // Unsubmitted = 4,
            migrationBuilder.Sql("UPDATE \"Participations\" SET \"Status\" = 0 WHERE \"Status_Old\" = 'Pending'");
            migrationBuilder.Sql("UPDATE \"Participations\" SET \"Status\" = 1 WHERE \"Status_Old\" = 'Accepted'");
            migrationBuilder.Sql("UPDATE \"Participations\" SET \"Status\" = 2 WHERE \"Status_Old\" = 'Denied'");
            migrationBuilder.Sql("UPDATE \"Participations\" SET \"Status\" = 3 WHERE \"Status_Old\" = 'Forfeited'");
            migrationBuilder.Sql("UPDATE \"Participations\" SET \"Status\" = 4 WHERE \"Status_Old\" = 'Unsubmitted'");

            // drop the old column
            migrationBuilder.DropColumn(
                name: "Status_Old",
                table: "Participations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // the int column
            migrationBuilder.RenameColumn(
                name: "Status",
                table: "Participations",
                newName: "Status_New");

            // the string column
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Participations",
                type: "text",
                nullable: true);

            // update the enum with the new values
            // Pending = 0,
            // Accepted = 1,
            // Denied = 2,
            // Forfeited = 3,
            // Unsubmitted = 4,
            migrationBuilder.Sql("UPDATE \"Participations\" SET \"Status\" = 'Pending' WHERE \"Status_New\" = 0");
            migrationBuilder.Sql("UPDATE \"Participations\" SET \"Status\" = 'Accepted' WHERE \"Status_New\" = 1");
            migrationBuilder.Sql("UPDATE \"Participations\" SET \"Status\" = 'Denied' WHERE \"Status_New\" = 2");
            migrationBuilder.Sql("UPDATE \"Participations\" SET \"Status\" = 'Forfeited' WHERE \"Status_New\" = 3");
            migrationBuilder.Sql("UPDATE \"Participations\" SET \"Status\" = 'Unsubmitted' WHERE \"Status_New\" = 4");

            // drop the old column
            migrationBuilder.DropColumn(
                name: "Status_New",
                table: "Participations");
        }
    }
}
