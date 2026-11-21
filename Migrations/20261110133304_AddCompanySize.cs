using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sgspe.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanySize : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Size",
                table: "Companies",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Size",
                table: "Companies");
        }
    }
}
