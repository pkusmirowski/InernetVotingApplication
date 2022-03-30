using Microsoft.EntityFrameworkCore.Migrations;

namespace InernetVotingApplication.Migrations
{
    public partial class fourth : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "jestAktywne",
                table: "Uzytkownik",
                type: "bit",
                nullable: false,
                defaultValueSql: "((1))",
                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true,
                oldDefaultValueSql: "((1))");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "jestAktywne",
                table: "Uzytkownik",
                type: "bit",
                nullable: true,
                defaultValueSql: "((1))",
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValueSql: "((1))");
        }
    }
}
