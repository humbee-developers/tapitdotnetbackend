using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TapitAI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSpotlightVisibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSpotlightVisible",
                table: "UserDatingProfiles",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSpotlightVisible",
                table: "UserDatingProfiles");
        }
    }
}
