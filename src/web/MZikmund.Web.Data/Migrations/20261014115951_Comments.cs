using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MZikmund.Web.Data.Migrations;

/// <inheritdoc />
[SuppressMessage("Performance", "CA1861:Avoid constant arrays as arguments")]
public partial class Comments : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CommentAuthor",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                DisplayName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                AvatarUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                Email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                IsBanned = table.Column<bool>(type: "bit", nullable: false),
                NotifyOnReply = table.Column<bool>(type: "bit", nullable: false),
                CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                LastSeenOn = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CommentAuthor", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Comment",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PostId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                AuthorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Content = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                ContentHtml = table.Column<string>(type: "nvarchar(max)", nullable: false),
                MentionedDisplayName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                Status = table.Column<int>(type: "int", nullable: false),
                CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                EditedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                UpvoteCount = table.Column<int>(type: "int", nullable: false),
                ReplyCount = table.Column<int>(type: "int", nullable: false),
                ReportCount = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Comment", x => x.Id);
                table.ForeignKey(
                    name: "FK_Comment_CommentAuthor_AuthorId",
                    column: x => x.AuthorId,
                    principalTable: "CommentAuthor",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Comment_Comment_ParentId",
                    column: x => x.ParentId,
                    principalTable: "Comment",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Comment_Post_PostId",
                    column: x => x.PostId,
                    principalTable: "Post",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "CommentReport",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CommentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ReporterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Reason = table.Column<int>(type: "int", nullable: false),
                Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                ResolvedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CommentReport", x => x.Id);
                table.ForeignKey(
                    name: "FK_CommentReport_CommentAuthor_ReporterId",
                    column: x => x.ReporterId,
                    principalTable: "CommentAuthor",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_CommentReport_Comment_CommentId",
                    column: x => x.CommentId,
                    principalTable: "Comment",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "CommentVote",
            columns: table => new
            {
                CommentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                AuthorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CommentVote", x => new { x.CommentId, x.AuthorId });
                table.ForeignKey(
                    name: "FK_CommentVote_CommentAuthor_AuthorId",
                    column: x => x.AuthorId,
                    principalTable: "CommentAuthor",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_CommentVote_Comment_CommentId",
                    column: x => x.CommentId,
                    principalTable: "Comment",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Comment_AuthorId",
            table: "Comment",
            column: "AuthorId");

        migrationBuilder.CreateIndex(
            name: "IX_Comment_ParentId",
            table: "Comment",
            column: "ParentId");

        migrationBuilder.CreateIndex(
            name: "IX_Comment_PostId_ParentId_CreatedOn",
            table: "Comment",
            columns: new[] { "PostId", "ParentId", "CreatedOn" });

        migrationBuilder.CreateIndex(
            name: "IX_CommentReport_CommentId_ResolvedOn",
            table: "CommentReport",
            columns: new[] { "CommentId", "ResolvedOn" });

        migrationBuilder.CreateIndex(
            name: "IX_CommentReport_ReporterId",
            table: "CommentReport",
            column: "ReporterId");

        migrationBuilder.CreateIndex(
            name: "IX_CommentVote_AuthorId",
            table: "CommentVote",
            column: "AuthorId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "CommentReport");

        migrationBuilder.DropTable(
            name: "CommentVote");

        migrationBuilder.DropTable(
            name: "Comment");

        migrationBuilder.DropTable(
            name: "CommentAuthor");
    }
}
