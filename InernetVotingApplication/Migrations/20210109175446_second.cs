using Microsoft.EntityFrameworkCore.Migrations;

namespace InernetVotingApplication.Migrations
{
    public partial class Second : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "kodAktywacyjny",
                table: "Uzytkownik",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "kodAktywacyjny",
                table: "Uzytkownik");
        }
    }
}
