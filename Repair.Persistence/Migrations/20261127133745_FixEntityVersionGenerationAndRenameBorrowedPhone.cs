using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repair.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixEntityVersionGenerationAndRenameBorrowedPhone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "BorrowedPhone",
                table: "Orders",
                newName: "BorrowedDevice");

            migrationBuilder.AlterColumn<byte[]>(
                name: "Version",
                table: "Orders",
                type: "BLOB",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "BLOB",
                oldRowVersion: true,
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<byte[]>(
                name: "Version",
                table: "Customers",
                type: "BLOB",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "BLOB",
                oldRowVersion: true,
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.Sql("UPDATE Customers SET Version = randomblob(16);");
            migrationBuilder.Sql("UPDATE Orders SET Version = randomblob(16);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "BorrowedDevice",
                table: "Orders",
                newName: "BorrowedPhone");

            migrationBuilder.AlterColumn<string>(
                name: "Version",
                table: "Orders",
                type: "BLOB",
                rowVersion: true,
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(byte[]),
                oldType: "BLOB");

            migrationBuilder.AlterColumn<string>(
                name: "Version",
                table: "Customers",
                type: "BLOB",
                rowVersion: true,
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(byte[]),
                oldType: "BLOB");

            migrationBuilder.Sql("UPDATE Customers SET Version = CURRENT_TIMESTAMP;");
            migrationBuilder.Sql("UPDATE Orders SET Version = CURRENT_TIMESTAMP;");
        }
    }
}
