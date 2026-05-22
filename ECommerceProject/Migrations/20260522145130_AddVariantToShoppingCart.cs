using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerceProject.Migrations
{
    /// <inheritdoc />
    public partial class AddVariantToShoppingCart : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProductVariantId",
                table: "ShoppingCarts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "Orders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ZipCode",
                table: "Orders",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingCarts_ProductVariantId",
                table: "ShoppingCarts",
                column: "ProductVariantId");

            migrationBuilder.AddForeignKey(
                name: "FK_ShoppingCarts_ProductVariants_ProductVariantId",
                table: "ShoppingCarts",
                column: "ProductVariantId",
                principalTable: "ProductVariants",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShoppingCarts_ProductVariants_ProductVariantId",
                table: "ShoppingCarts");

            migrationBuilder.DropIndex(
                name: "IX_ShoppingCarts_ProductVariantId",
                table: "ShoppingCarts");

            migrationBuilder.DropColumn(
                name: "ProductVariantId",
                table: "ShoppingCarts");

            migrationBuilder.DropColumn(
                name: "State",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ZipCode",
                table: "Orders");
        }
    }
}
