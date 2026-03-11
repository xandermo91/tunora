using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tunora.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveQuartzJobKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QuartzJobKey",
                table: "Schedules");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "QuartzJobKey",
                table: "Schedules",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }
    }
}
