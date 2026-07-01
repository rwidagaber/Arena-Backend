using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArenaInfrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTargetMacrosToMemberProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CurrentPlanFramework",
                table: "MemberProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TargetCalories",
                table: "MemberProfiles",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TargetCarbs",
                table: "MemberProfiles",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TargetFat",
                table: "MemberProfiles",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TargetProtein",
                table: "MemberProfiles",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentPlanFramework",
                table: "MemberProfiles");

            migrationBuilder.DropColumn(
                name: "TargetCalories",
                table: "MemberProfiles");

            migrationBuilder.DropColumn(
                name: "TargetCarbs",
                table: "MemberProfiles");

            migrationBuilder.DropColumn(
                name: "TargetFat",
                table: "MemberProfiles");

            migrationBuilder.DropColumn(
                name: "TargetProtein",
                table: "MemberProfiles");
        }
    }
}
