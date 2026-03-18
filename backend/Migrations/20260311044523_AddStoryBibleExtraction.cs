using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CanonGuard.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddStoryBibleExtraction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Entities_ProjectId",
                table: "Entities");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Entities",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateTable(
                name: "Embeddings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    ChapterId = table.Column<int>(type: "int", nullable: true),
                    Text = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VectorJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Embeddings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Facts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: true),
                    FactType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SourceChapterId = table.Column<int>(type: "int", nullable: false),
                    SourceQuote = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Confidence = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Facts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Facts_Entities_EntityId",
                        column: x => x.EntityId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Entities_ProjectId_Type_Name",
                table: "Entities",
                columns: new[] { "ProjectId", "Type", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Facts_EntityId",
                table: "Facts",
                column: "EntityId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Embeddings");

            migrationBuilder.DropTable(
                name: "Facts");

            migrationBuilder.DropIndex(
                name: "IX_Entities_ProjectId_Type_Name",
                table: "Entities");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Entities",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.CreateIndex(
                name: "IX_Entities_ProjectId",
                table: "Entities",
                column: "ProjectId");
        }
    }
}
