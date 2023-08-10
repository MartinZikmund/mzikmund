﻿using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MZikmund.Web.Data.Migrations;

/// <inheritdoc />
public partial class Initial : Migration
{
	/// <inheritdoc />
	protected override void Up(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.CreateTable(
			name: "Category",
			columns: table => new
			{
				Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
				DisplayName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
				Description = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
				Icon = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
				RouteName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
			},
			constraints: table =>
			{
				table.PrimaryKey("PK_Category", x => x.Id);
			});

		migrationBuilder.CreateTable(
			name: "Post",
			columns: table => new
			{
				Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
				Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
				CreatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
				PublishedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
				LastModifiedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
				Status = table.Column<int>(type: "int", nullable: false),
				HeroImageUrl = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
				LanguageCode = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
				Content = table.Column<string>(type: "nvarchar(max)", maxLength: 10000, nullable: false),
				Abstract = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
				RouteName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
			},
			constraints: table =>
			{
				table.PrimaryKey("PK_Post", x => x.Id);
			});

		migrationBuilder.CreateTable(
			name: "Tag",
			columns: table => new
			{
				Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
				DisplayName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
				Description = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
				RouteName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
			},
			constraints: table =>
			{
				table.PrimaryKey("PK_Tag", x => x.Id);
			});

		migrationBuilder.CreateTable(
				name: "PostCategory",
				columns: table => new
				{
					PostId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
					CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_PostCategory", x => new { x.PostId, x.CategoryId });
					table.ForeignKey(
						name: "FK_PostCategory_Category_CategoryId",
						column: x => x.CategoryId,
						principalTable: "Category",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
					table.ForeignKey(
						name: "FK_PostCategory_Post_PostId",
						column: x => x.PostId,
						principalTable: "Post",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

		migrationBuilder.CreateTable(
			name: "PostTag",
			columns: table => new
			{
				PostId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
				TagId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
			},
			constraints: table =>
			{
				table.PrimaryKey("PK_PostTag", x => new { x.PostId, x.TagId });
				table.ForeignKey(
					name: "FK_PostTag_Post_PostId",
					column: x => x.PostId,
					principalTable: "Post",
					principalColumn: "Id",
					onDelete: ReferentialAction.Cascade);
				table.ForeignKey(
					name: "FK_PostTag_Tag_TagId",
					column: x => x.TagId,
					principalTable: "Tag",
					principalColumn: "Id",
					onDelete: ReferentialAction.Cascade);
			});

		migrationBuilder.CreateIndex(
			name: "IX_PostCategory_CategoryId",
			table: "PostCategory",
			column: "CategoryId");

		migrationBuilder.CreateIndex(
			name: "IX_PostTag_TagId",
			table: "PostTag",
			column: "TagId");
	}

	/// <inheritdoc />
	protected override void Down(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.DropTable(
			name: "PostCategory");

		migrationBuilder.DropTable(
			name: "PostTag");

		migrationBuilder.DropTable(
			name: "Category");

		migrationBuilder.DropTable(
			name: "Post");

		migrationBuilder.DropTable(
			name: "Tag");
	}
}