using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class FinalizeSchemaUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Users_AnnotatorId",
                table: "Invoices");

            migrationBuilder.RenameColumn(
                name: "UnitPriceSnapshot",
                table: "Invoices",
                newName: "UnitPrice");

            migrationBuilder.RenameColumn(
                name: "TotalValidLabels",
                table: "Invoices",
                newName: "TotalLabels");

            migrationBuilder.RenameColumn(
                name: "PeriodStart",
                table: "Invoices",
                newName: "StartDate");

            migrationBuilder.RenameColumn(
                name: "PeriodEnd",
                table: "Invoices",
                newName: "EndDate");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Invoices",
                newName: "CreatedDate");

            migrationBuilder.RenameColumn(
                name: "AnnotatorId",
                table: "Invoices",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Invoices_AnnotatorId",
                table: "Invoices",
                newName: "IX_Invoices_UserId");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Projects",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "AllowGeometryTypes",
                table: "Projects",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "Projects",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Projects",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "MetaData",
                table: "DataItems",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UploadedDate",
                table: "DataItems",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Users_UserId",
                table: "Invoices",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Users_UserId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "AllowGeometryTypes",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "UploadedDate",
                table: "DataItems");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Invoices",
                newName: "AnnotatorId");

            migrationBuilder.RenameColumn(
                name: "UnitPrice",
                table: "Invoices",
                newName: "UnitPriceSnapshot");

            migrationBuilder.RenameColumn(
                name: "TotalLabels",
                table: "Invoices",
                newName: "TotalValidLabels");

            migrationBuilder.RenameColumn(
                name: "StartDate",
                table: "Invoices",
                newName: "PeriodStart");

            migrationBuilder.RenameColumn(
                name: "EndDate",
                table: "Invoices",
                newName: "PeriodEnd");

            migrationBuilder.RenameColumn(
                name: "CreatedDate",
                table: "Invoices",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_Invoices_UserId",
                table: "Invoices",
                newName: "IX_Invoices_AnnotatorId");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Projects",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "MetaData",
                table: "DataItems",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Users_AnnotatorId",
                table: "Invoices",
                column: "AnnotatorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
