using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class RefactorSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DataItems_Users_AnnotatorId",
                table: "DataItems");

            migrationBuilder.DropForeignKey(
                name: "FK_DataItems_Users_ReviewerId",
                table: "DataItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Users_ManagerId",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_ManagerId",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_DataItems_AnnotatorId",
                table: "DataItems");

            migrationBuilder.DropIndex(
                name: "IX_DataItems_ReviewerId",
                table: "DataItems");

            migrationBuilder.DropColumn(
                name: "ManagerId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "AnnotatorId",
                table: "DataItems");

            migrationBuilder.DropColumn(
                name: "ReviewerId",
                table: "DataItems");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "DataItems");

            migrationBuilder.CreateTable(
                name: "Assignments",
                columns: table => new
                {
                    AssignmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DataItemId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    TaskType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assignments", x => x.AssignmentId);
                    table.ForeignKey(
                        name: "FK_Assignments_DataItems_DataItemId",
                        column: x => x.DataItemId,
                        principalTable: "DataItems",
                        principalColumn: "DataItemId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Assignments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectMembers",
                columns: table => new
                {
                    ProjectMemberId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ProjectRole = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectMembers", x => x.ProjectMemberId);
                    table.ForeignKey(
                        name: "FK_ProjectMembers_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "ProjectId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectMembers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowLogs",
                columns: table => new
                {
                    LogId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DataItemId = table.Column<int>(type: "int", nullable: false),
                    ActorId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowLogs", x => x.LogId);
                    table.ForeignKey(
                        name: "FK_WorkflowLogs_DataItems_DataItemId",
                        column: x => x.DataItemId,
                        principalTable: "DataItems",
                        principalColumn: "DataItemId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkflowLogs_Users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_DataItemId",
                table: "Assignments",
                column: "DataItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_UserId",
                table: "Assignments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMembers_ProjectId",
                table: "ProjectMembers",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMembers_UserId",
                table: "ProjectMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowLogs_ActorId",
                table: "WorkflowLogs",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowLogs_DataItemId",
                table: "WorkflowLogs",
                column: "DataItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Assignments");

            migrationBuilder.DropTable(
                name: "ProjectMembers");

            migrationBuilder.DropTable(
                name: "WorkflowLogs");

            migrationBuilder.AddColumn<int>(
                name: "ManagerId",
                table: "Projects",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AnnotatorId",
                table: "DataItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReviewerId",
                table: "DataItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "DataItems",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_ManagerId",
                table: "Projects",
                column: "ManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_DataItems_AnnotatorId",
                table: "DataItems",
                column: "AnnotatorId");

            migrationBuilder.CreateIndex(
                name: "IX_DataItems_ReviewerId",
                table: "DataItems",
                column: "ReviewerId");

            migrationBuilder.AddForeignKey(
                name: "FK_DataItems_Users_AnnotatorId",
                table: "DataItems",
                column: "AnnotatorId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DataItems_Users_ReviewerId",
                table: "DataItems",
                column: "ReviewerId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Users_ManagerId",
                table: "Projects",
                column: "ManagerId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
