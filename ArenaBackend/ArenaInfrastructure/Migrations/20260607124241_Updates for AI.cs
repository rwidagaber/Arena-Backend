using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArenaInfrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdatesforAI : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExrciseName",
                table: "WorkoutExercises",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ActivityLevel",
                table: "MemberProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DietaryRestrictions",
                table: "MemberProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Equipment",
                table: "MemberProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "MemberProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FitnessExperience",
                table: "MemberProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Goal",
                table: "MemberProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HealthConditions",
                table: "MemberProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Injuries",
                table: "MemberProfiles",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExrciseName",
                table: "WorkoutExercises");

            migrationBuilder.DropColumn(
                name: "ActivityLevel",
                table: "MemberProfiles");

            migrationBuilder.DropColumn(
                name: "DietaryRestrictions",
                table: "MemberProfiles");

            migrationBuilder.DropColumn(
                name: "Equipment",
                table: "MemberProfiles");

            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "MemberProfiles");

            migrationBuilder.DropColumn(
                name: "FitnessExperience",
                table: "MemberProfiles");

            migrationBuilder.DropColumn(
                name: "Goal",
                table: "MemberProfiles");

            migrationBuilder.DropColumn(
                name: "HealthConditions",
                table: "MemberProfiles");

            migrationBuilder.DropColumn(
                name: "Injuries",
                table: "MemberProfiles");
        }
    }
}
