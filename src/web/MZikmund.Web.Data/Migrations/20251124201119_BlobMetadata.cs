using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MZikmund.Web.Data.Migrations;

/// <inheritdoc />
[SuppressMessage("Performance", "CA1861:Avoid constant arrays as arguments")]
public partial class BlobMetadata : Migration
{
	/// <inheritdoc />
	protected override void Up(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.CreateTable(
			name: "BlobMetadata",
			columns: table => new
			{
				Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
				Kind = table.Column<int>(type: "int", nullable: false),
				BlobPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
				FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
				LastModified = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
				Size = table.Column<long>(type: "bigint", nullable: false),
				ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
			},
			constraints: table =>
			{
				table.PrimaryKey("PK_BlobMetadata", x => x.Id);
			});

		migrationBuilder.CreateIndex(
			name: "IX_BlobMetadata_BlobPath",
			table: "BlobMetadata",
			column: "BlobPath",
			unique: true);

		migrationBuilder.CreateIndex(
			name: "IX_BlobMetadata_Kind_LastModified",
			table: "BlobMetadata",
			columns: new[] { "Kind", "LastModified" });
	}

	/// <inheritdoc />
	protected override void Down(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.DropTable(
			name: "BlobMetadata");
	}
}
