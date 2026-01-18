using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlcLab.Infrastructure.Migrations
{
    public partial class AddPlanVersion : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>
            (
                name: "PlanVersion",
                table: "TestRuns",
                type: "integer",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder.AddColumn<int>
            (
                name: "Version",
                table: "TestPlans",
                type: "integer",
                nullable: false,
                defaultValue: 1
            );

            // Initialize existing runs with version 1 for historical consistency
            migrationBuilder.Sql("UPDATE \"TestRuns\" SET \"PlanVersion\" = 1 WHERE \"PlanVersion\" = 0;");
        }

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
