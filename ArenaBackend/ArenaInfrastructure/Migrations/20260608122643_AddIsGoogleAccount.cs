using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArenaInfrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIsGoogleAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsGoogleAccount",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsGoogleAccount",
                table: "AspNetUsers");
        }
    }
}
