using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternetVotingApplication.Migrations
{
    public partial class init2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "jestAktywne",
                table: "Uzytkownik",
                type: "int",
                nullable: false,
                defaultValueSql: "((1))",
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValueSql: "((1))");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "jestAktywne",
                table: "Uzytkownik",
                type: "bit",
                nullable: false,
                defaultValueSql: "((1))",
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValueSql: "((1))");
        }
    }
}
