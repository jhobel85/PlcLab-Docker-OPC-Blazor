using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlcLab.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditFieldsToTestRun : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Thumbprint",
                table: "TestRuns",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EndpointUrl",
                table: "TestRuns",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserIdentity",
                table: "TestRuns",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Thumbprint",
                table: "TestRuns");

            migrationBuilder.DropColumn(
                name: "EndpointUrl",
                table: "TestRuns");

            migrationBuilder.DropColumn(
                name: "UserIdentity",
                table: "TestRuns");
        }
    }
}
