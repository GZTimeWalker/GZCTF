using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GZCTF.Migrations
{
    /// <inheritdoc />
    public partial class AddSubmissionLimitFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add SubmissionLimit field to challenge tables
            migrationBuilder.AddColumn<int>(
                name: "SubmissionLimit",
                table: "GameChallenges",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SubmissionLimit",
                table: "ExerciseChallenges",
                type: "integer",
                nullable: true);

            // Add SubmissionCount field to instance tables
            migrationBuilder.AddColumn<int>(
                name: "SubmissionCount",
                table: "GameInstances",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SubmissionCount",
                table: "ExerciseInstances",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Add ConcurrencyStamp for optimistic concurrency control
            migrationBuilder.AddColumn<byte[]>(
                name: "ConcurrencyStamp",
                table: "GameInstances",
                type: "bytea",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "ConcurrencyStamp",
                table: "ExerciseInstances",
                type: "bytea",
                rowVersion: true,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove SubmissionLimit fields
            migrationBuilder.DropColumn(
                name: "SubmissionLimit",
                table: "GameChallenges");

            migrationBuilder.DropColumn(
                name: "SubmissionLimit",
                table: "ExerciseChallenges");

            // Remove SubmissionCount fields
            migrationBuilder.DropColumn(
                name: "SubmissionCount",
                table: "GameInstances");

            migrationBuilder.DropColumn(
                name: "SubmissionCount",
                table: "ExerciseInstances");

            // Remove ConcurrencyStamp fields
            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                table: "GameInstances");

            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                table: "ExerciseInstances");
        }
    }
}