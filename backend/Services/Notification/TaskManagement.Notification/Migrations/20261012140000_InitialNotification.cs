using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskManagement.Notification.Migrations;

public partial class InitialNotification : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Notifications",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                LinkUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                IsRead = table.Column<bool>(type: "bit", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Notifications", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Notifications_CreatedAtUtc",
            table: "Notifications",
            column: "CreatedAtUtc");

        migrationBuilder.CreateIndex(
            name: "IX_Notifications_UserId_IsRead",
            table: "Notifications",
            columns: new[] { "UserId", "IsRead" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Notifications");
    }
}
