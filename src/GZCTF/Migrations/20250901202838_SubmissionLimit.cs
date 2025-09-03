using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GZCTF.Migrations
{
    /// <inheritdoc />
    public partial class SubmissionLimit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                table: "GameChallenges");

            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                table: "ExerciseChallenges");

            migrationBuilder.AddColumn<int>(
                name: "SubmissionCount",
                table: "GameInstances",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "GameInstances",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<int>(
                name: "SubmissionLimit",
                table: "GameChallenges",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "GameChallenges",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<int>(
                name: "SubmissionCount",
                table: "ExerciseInstances",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "ExerciseInstances",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<int>(
                name: "SubmissionLimit",
                table: "ExerciseChallenges",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "ExerciseChallenges",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SubmissionCount",
                table: "GameInstances");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "GameInstances");

            migrationBuilder.DropColumn(
                name: "SubmissionLimit",
                table: "GameChallenges");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "GameChallenges");

            migrationBuilder.DropColumn(
                name: "SubmissionCount",
                table: "ExerciseInstances");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "ExerciseInstances");

            migrationBuilder.DropColumn(
                name: "SubmissionLimit",
                table: "ExerciseChallenges");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "ExerciseChallenges");

            migrationBuilder.AddColumn<Guid>(
                name: "ConcurrencyStamp",
                table: "GameChallenges",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ConcurrencyStamp",
                table: "ExerciseChallenges",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }
    }
}
