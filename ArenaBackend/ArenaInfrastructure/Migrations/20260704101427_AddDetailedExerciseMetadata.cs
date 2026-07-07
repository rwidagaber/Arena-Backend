using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArenaInfrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDetailedExerciseMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Breathing",
                table: "Exercises",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Exercises",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CommonMistakes",
                table: "Exercises",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Difficulty",
                table: "Exercises",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Instructions",
                table: "Exercises",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrimaryMuscles",
                table: "Exercises",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SafetyTips",
                table: "Exercises",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecondaryMuscles",
                table: "Exercises",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Breathing",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "CommonMistakes",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "Difficulty",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "Instructions",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "PrimaryMuscles",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "SafetyTips",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "SecondaryMuscles",
                table: "Exercises");
        }
    }
}
