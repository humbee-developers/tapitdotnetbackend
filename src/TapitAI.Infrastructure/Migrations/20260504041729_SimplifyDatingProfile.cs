using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TapitAI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyDatingProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserDatingProfiles_LookupOptions_AgeRangeOptionId",
                table: "UserDatingProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_UserDatingProfiles_LookupOptions_PreferHeightOptionId",
                table: "UserDatingProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_UserDatingProfiles_LookupOptions_SelfGenderOptionId",
                table: "UserDatingProfiles");

            migrationBuilder.DropTable(
                name: "UserProfileInterestedGenders");

            migrationBuilder.DropTable(
                name: "UserProfileLifestyles");

            migrationBuilder.DropTable(
                name: "UserProfileLookingFors");

            migrationBuilder.DropTable(
                name: "LookupOptions");

            migrationBuilder.DropIndex(
                name: "IX_UserDatingProfiles_AgeRangeOptionId",
                table: "UserDatingProfiles");

            migrationBuilder.DropIndex(
                name: "IX_UserDatingProfiles_PreferHeightOptionId",
                table: "UserDatingProfiles");

            migrationBuilder.DropIndex(
                name: "IX_UserDatingProfiles_SelfGenderOptionId",
                table: "UserDatingProfiles");

            migrationBuilder.DropColumn(
                name: "AgeRangeOptionId",
                table: "UserDatingProfiles");

            migrationBuilder.DropColumn(
                name: "PreferHeightOptionId",
                table: "UserDatingProfiles");

            migrationBuilder.DropColumn(
                name: "SelfGenderOptionId",
                table: "UserDatingProfiles");

            migrationBuilder.RenameColumn(
                name: "HeightInch",
                table: "UserDatingProfiles",
                newName: "HeightIn");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "UserDatingProfiles",
                newName: "Bio");

            migrationBuilder.AddColumn<string>(
                name: "AgeRange",
                table: "UserDatingProfiles",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "UserDatingProfiles",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string[]>(
                name: "GenderPreference",
                table: "UserDatingProfiles",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.AddColumn<string[]>(
                name: "HeightPreference",
                table: "UserDatingProfiles",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.AddColumn<string[]>(
                name: "Lifestyle",
                table: "UserDatingProfiles",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.AddColumn<string[]>(
                name: "LookingFor",
                table: "UserDatingProfiles",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AgeRange",
                table: "UserDatingProfiles");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "UserDatingProfiles");

            migrationBuilder.DropColumn(
                name: "GenderPreference",
                table: "UserDatingProfiles");

            migrationBuilder.DropColumn(
                name: "HeightPreference",
                table: "UserDatingProfiles");

            migrationBuilder.DropColumn(
                name: "Lifestyle",
                table: "UserDatingProfiles");

            migrationBuilder.DropColumn(
                name: "LookingFor",
                table: "UserDatingProfiles");

            migrationBuilder.RenameColumn(
                name: "HeightIn",
                table: "UserDatingProfiles",
                newName: "HeightInch");

            migrationBuilder.RenameColumn(
                name: "Bio",
                table: "UserDatingProfiles",
                newName: "Description");

            migrationBuilder.AddColumn<Guid>(
                name: "AgeRangeOptionId",
                table: "UserDatingProfiles",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "PreferHeightOptionId",
                table: "UserDatingProfiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SelfGenderOptionId",
                table: "UserDatingProfiles",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "LookupOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Value = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LookupOptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserProfileInterestedGenders",
                columns: table => new
                {
                    InterestedGendersId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserDatingProfileId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfileInterestedGenders", x => new { x.InterestedGendersId, x.UserDatingProfileId });
                    table.ForeignKey(
                        name: "FK_UserProfileInterestedGenders_LookupOptions_InterestedGender~",
                        column: x => x.InterestedGendersId,
                        principalTable: "LookupOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserProfileInterestedGenders_UserDatingProfiles_UserDatingP~",
                        column: x => x.UserDatingProfileId,
                        principalTable: "UserDatingProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserProfileLifestyles",
                columns: table => new
                {
                    LifestylesId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserDatingProfile1Id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfileLifestyles", x => new { x.LifestylesId, x.UserDatingProfile1Id });
                    table.ForeignKey(
                        name: "FK_UserProfileLifestyles_LookupOptions_LifestylesId",
                        column: x => x.LifestylesId,
                        principalTable: "LookupOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserProfileLifestyles_UserDatingProfiles_UserDatingProfile1~",
                        column: x => x.UserDatingProfile1Id,
                        principalTable: "UserDatingProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserProfileLookingFors",
                columns: table => new
                {
                    LookingForsId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserDatingProfile2Id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfileLookingFors", x => new { x.LookingForsId, x.UserDatingProfile2Id });
                    table.ForeignKey(
                        name: "FK_UserProfileLookingFors_LookupOptions_LookingForsId",
                        column: x => x.LookingForsId,
                        principalTable: "LookupOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserProfileLookingFors_UserDatingProfiles_UserDatingProfile~",
                        column: x => x.UserDatingProfile2Id,
                        principalTable: "UserDatingProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserDatingProfiles_AgeRangeOptionId",
                table: "UserDatingProfiles",
                column: "AgeRangeOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserDatingProfiles_PreferHeightOptionId",
                table: "UserDatingProfiles",
                column: "PreferHeightOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserDatingProfiles_SelfGenderOptionId",
                table: "UserDatingProfiles",
                column: "SelfGenderOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_LookupOptions_Category_IsActive",
                table: "LookupOptions",
                columns: new[] { "Category", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_UserProfileInterestedGenders_UserDatingProfileId",
                table: "UserProfileInterestedGenders",
                column: "UserDatingProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfileLifestyles_UserDatingProfile1Id",
                table: "UserProfileLifestyles",
                column: "UserDatingProfile1Id");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfileLookingFors_UserDatingProfile2Id",
                table: "UserProfileLookingFors",
                column: "UserDatingProfile2Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserDatingProfiles_LookupOptions_AgeRangeOptionId",
                table: "UserDatingProfiles",
                column: "AgeRangeOptionId",
                principalTable: "LookupOptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserDatingProfiles_LookupOptions_PreferHeightOptionId",
                table: "UserDatingProfiles",
                column: "PreferHeightOptionId",
                principalTable: "LookupOptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserDatingProfiles_LookupOptions_SelfGenderOptionId",
                table: "UserDatingProfiles",
                column: "SelfGenderOptionId",
                principalTable: "LookupOptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
