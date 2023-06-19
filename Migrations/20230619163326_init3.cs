using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternetVotingApplication.Migrations
{
    public partial class init3 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "jestAktywne",
                table: "Uzytkownik",
                type: "int",
                nullable: true,
                defaultValueSql: "((1))",
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValueSql: "((1))");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "jestAktywne",
                table: "Uzytkownik",
                type: "int",
                nullable: false,
                defaultValueSql: "((1))",
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true,
                oldDefaultValueSql: "((1))");
        }
    }
}
