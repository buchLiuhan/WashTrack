using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WashTrack.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyServiceInventoryTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ConditionerItemId",
                table: "Services",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DetergentItemId",
                table: "Services",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OtherItemId",
                table: "Services",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OtherUsageAmount",
                table: "Services",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Services_ConditionerItemId",
                table: "Services",
                column: "ConditionerItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Services_DetergentItemId",
                table: "Services",
                column: "DetergentItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Services_OtherItemId",
                table: "Services",
                column: "OtherItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_Services_Inventories_ConditionerItemId",
                table: "Services",
                column: "ConditionerItemId",
                principalTable: "Inventories",
                principalColumn: "InventoryId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Services_Inventories_DetergentItemId",
                table: "Services",
                column: "DetergentItemId",
                principalTable: "Inventories",
                principalColumn: "InventoryId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Services_Inventories_OtherItemId",
                table: "Services",
                column: "OtherItemId",
                principalTable: "Inventories",
                principalColumn: "InventoryId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Services_Inventories_ConditionerItemId",
                table: "Services");

            migrationBuilder.DropForeignKey(
                name: "FK_Services_Inventories_DetergentItemId",
                table: "Services");

            migrationBuilder.DropForeignKey(
                name: "FK_Services_Inventories_OtherItemId",
                table: "Services");

            migrationBuilder.DropIndex(
                name: "IX_Services_ConditionerItemId",
                table: "Services");

            migrationBuilder.DropIndex(
                name: "IX_Services_DetergentItemId",
                table: "Services");

            migrationBuilder.DropIndex(
                name: "IX_Services_OtherItemId",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "ConditionerItemId",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "DetergentItemId",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "OtherItemId",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "OtherUsageAmount",
                table: "Services");
        }
    }
}
