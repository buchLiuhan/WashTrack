using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WashTrack.Migrations
{
    /// <inheritdoc />
    public partial class AddFlatAndBundlePricing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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
                name: "DueDate",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ConditionerItemId",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "ConditionerUsageMl",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "DetergentItemId",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "DurationDays",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Inventories");

            migrationBuilder.RenameColumn(
                name: "OtherUsageAmount",
                table: "Services",
                newName: "MinKilo");

            migrationBuilder.RenameColumn(
                name: "OtherItemId",
                table: "Services",
                newName: "BundleSize");

            migrationBuilder.RenameColumn(
                name: "DetergentUsageMl",
                table: "Services",
                newName: "FlatRate");

            migrationBuilder.AlterColumn<decimal>(
                name: "PricePerKilo",
                table: "Services",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "TEXT");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MinKilo",
                table: "Services",
                newName: "OtherUsageAmount");

            migrationBuilder.RenameColumn(
                name: "FlatRate",
                table: "Services",
                newName: "DetergentUsageMl");

            migrationBuilder.RenameColumn(
                name: "BundleSize",
                table: "Services",
                newName: "OtherItemId");

            migrationBuilder.AddColumn<DateTime>(
                name: "DueDate",
                table: "Transactions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "PricePerKilo",
                table: "Services",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ConditionerItemId",
                table: "Services",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ConditionerUsageMl",
                table: "Services",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Services",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DetergentItemId",
                table: "Services",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DurationDays",
                table: "Services",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Inventories",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

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
    }
}
