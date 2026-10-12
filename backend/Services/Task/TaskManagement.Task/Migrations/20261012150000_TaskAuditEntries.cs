using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskManagement.Task.Migrations;

public partial class TaskAuditEntries : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "TaskAuditEntries",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                OldValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                NewValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TaskAuditEntries", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_TaskAuditEntries_CreatedAtUtc",
            table: "TaskAuditEntries",
            column: "CreatedAtUtc");

        migrationBuilder.CreateIndex(
            name: "IX_TaskAuditEntries_TaskId",
            table: "TaskAuditEntries",
            column: "TaskId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "TaskAuditEntries");
    }
}
