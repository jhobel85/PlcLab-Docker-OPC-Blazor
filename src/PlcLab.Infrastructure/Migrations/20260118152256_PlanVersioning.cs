using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlcLab.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PlanVersioning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PlanVersion",
                table: "TestRuns",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "TestPlans",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlanVersion",
                table: "TestRuns");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "TestPlans");
        }
    }
}
