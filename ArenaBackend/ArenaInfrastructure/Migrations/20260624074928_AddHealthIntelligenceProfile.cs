using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArenaInfrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHealthIntelligenceProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HealthProfileJson",
                table: "MemberProfiles",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HealthProfileJson",
                table: "MemberProfiles");
        }
    }
}
