using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CanonGuard.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAppDbContextForAiExtraction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Facts_Entities_EntityId",
                table: "Facts");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Projects",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_OwnerId_CreatedAt",
                table: "Projects",
                columns: new[] { "OwnerId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Facts_ProjectId",
                table: "Facts",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Facts_ProjectId_FactType",
                table: "Facts",
                columns: new[] { "ProjectId", "FactType" });

            migrationBuilder.CreateIndex(
                name: "IX_Facts_SourceChapterId",
                table: "Facts",
                column: "SourceChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_Entities_ProjectId",
                table: "Entities",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Embeddings_ChapterId",
                table: "Embeddings",
                column: "ChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_Embeddings_ProjectId",
                table: "Embeddings",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Embeddings_ProjectId_CreatedAt",
                table: "Embeddings",
                columns: new[] { "ProjectId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_ProjectId_CreatedAt",
                table: "Chapters",
                columns: new[] { "ProjectId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_ProjectId_UpdatedAt",
                table: "Chapters",
                columns: new[] { "ProjectId", "UpdatedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_Embeddings_Chapters_ChapterId",
                table: "Embeddings",
                column: "ChapterId",
                principalTable: "Chapters",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Embeddings_Projects_ProjectId",
                table: "Embeddings",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Facts_Chapters_SourceChapterId",
                table: "Facts",
                column: "SourceChapterId",
                principalTable: "Chapters",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Facts_Entities_EntityId",
                table: "Facts",
                column: "EntityId",
                principalTable: "Entities",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Facts_Projects_ProjectId",
                table: "Facts",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Embeddings_Chapters_ChapterId",
                table: "Embeddings");

            migrationBuilder.DropForeignKey(
                name: "FK_Embeddings_Projects_ProjectId",
                table: "Embeddings");

            migrationBuilder.DropForeignKey(
                name: "FK_Facts_Chapters_SourceChapterId",
                table: "Facts");

            migrationBuilder.DropForeignKey(
                name: "FK_Facts_Entities_EntityId",
                table: "Facts");

            migrationBuilder.DropForeignKey(
                name: "FK_Facts_Projects_ProjectId",
                table: "Facts");

            migrationBuilder.DropIndex(
                name: "IX_Projects_OwnerId_CreatedAt",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Facts_ProjectId",
                table: "Facts");

            migrationBuilder.DropIndex(
                name: "IX_Facts_ProjectId_FactType",
                table: "Facts");

            migrationBuilder.DropIndex(
                name: "IX_Facts_SourceChapterId",
                table: "Facts");

            migrationBuilder.DropIndex(
                name: "IX_Entities_ProjectId",
                table: "Entities");

            migrationBuilder.DropIndex(
                name: "IX_Embeddings_ChapterId",
                table: "Embeddings");

            migrationBuilder.DropIndex(
                name: "IX_Embeddings_ProjectId",
                table: "Embeddings");

            migrationBuilder.DropIndex(
                name: "IX_Embeddings_ProjectId_CreatedAt",
                table: "Embeddings");

            migrationBuilder.DropIndex(
                name: "IX_Chapters_ProjectId_CreatedAt",
                table: "Chapters");

            migrationBuilder.DropIndex(
                name: "IX_Chapters_ProjectId_UpdatedAt",
                table: "Chapters");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Projects",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Facts_Entities_EntityId",
                table: "Facts",
                column: "EntityId",
                principalTable: "Entities",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
