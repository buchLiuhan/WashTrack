using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WashTrack.Migrations
{
    /// <inheritdoc />
    public partial class LinkServiceToSpecificInventoryItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ConditionerUsageMl",
                table: "Services",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DetergentUsageMl",
                table: "Services",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Inventories",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConditionerUsageMl",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "DetergentUsageMl",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Inventories");
        }
    }
}
