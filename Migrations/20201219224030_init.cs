using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace InernetVotingApplication.Migrations
{
    public partial class Init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DataWyborow",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    dataRozpoczecia = table.Column<DateTime>(type: "datetime", nullable: false),
                    dataZakonczenia = table.Column<DateTime>(type: "datetime", nullable: false),
                    opis = table.Column<string>(type: "varchar(max)", unicode: false, nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_DataWyborow", x => x.id));

            migrationBuilder.CreateTable(
                name: "Uzytkownik",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    imie = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    nazwisko = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    pesel = table.Column<string>(type: "nchar(11)", fixedLength: true, maxLength: 11, nullable: false),
                    email = table.Column<string>(type: "varchar(89)", unicode: false, maxLength: 89, nullable: false),
                    dataUrodzenia = table.Column<DateTime>(type: "date", nullable: false),
                    haslo = table.Column<string>(type: "varchar(max)", unicode: false, nullable: false),
                    jestAktywne = table.Column<bool>(type: "bit", nullable: true, defaultValueSql: "((1))")
                },
                constraints: table => table.PrimaryKey("PK_Uzytkownik", x => x.id));

            migrationBuilder.CreateTable(
                name: "Kandydat",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    imie = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    nazwisko = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    id_wybory = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kandydat", x => x.id);
                    table.ForeignKey(
                        name: "FK_Kandydat_DataWyborow",
                        column: x => x.id_wybory,
                        principalTable: "DataWyborow",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Administrator",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_uzytkownik = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Administrator", x => x.id);
                    table.ForeignKey(
                        name: "FK_Administrator_Uzytkownik",
                        column: x => x.id_uzytkownik,
                        principalTable: "Uzytkownik",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GlosUzytkownika",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_uzytkownik = table.Column<int>(type: "int", nullable: false),
                    id_wybory = table.Column<int>(type: "int", nullable: false),
                    glos = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GlosUzytkownika", x => x.id);
                    table.ForeignKey(
                        name: "FK_GlosUzytkownika_Uzytkownik",
                        column: x => x.id_uzytkownik,
                        principalTable: "Uzytkownik",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GlosUzytkownika_Wybory",
                        column: x => x.id_wybory,
                        principalTable: "DataWyborow",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GlosowanieWyborcze",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_kandydat = table.Column<int>(type: "int", nullable: false),
                    id_wybory = table.Column<int>(type: "int", nullable: false),
                    id_poprzednie = table.Column<int>(type: "int", nullable: true),
                    glos = table.Column<bool>(type: "bit", nullable: false),
                    hash = table.Column<string>(type: "varchar(max)", unicode: false, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GlosowanieWyborcze", x => x.id);
                    table.ForeignKey(
                        name: "FK_GlosowanieWyborcze_DataWyborow",
                        column: x => x.id_wybory,
                        principalTable: "DataWyborow",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GlosowanieWyborcze_Kandydat",
                        column: x => x.id_kandydat,
                        principalTable: "Kandydat",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Administrator_id_uzytkownik",
                table: "Administrator",
                column: "id_uzytkownik");

            migrationBuilder.CreateIndex(
                name: "IX_GlosowanieWyborcze_id_kandydat",
                table: "GlosowanieWyborcze",
                column: "id_kandydat");

            migrationBuilder.CreateIndex(
                name: "IX_GlosowanieWyborcze_id_wybory",
                table: "GlosowanieWyborcze",
                column: "id_wybory");

            migrationBuilder.CreateIndex(
                name: "IX_GlosUzytkownika_id_uzytkownik",
                table: "GlosUzytkownika",
                column: "id_uzytkownik");

            migrationBuilder.CreateIndex(
                name: "IX_GlosUzytkownika_id_wybory",
                table: "GlosUzytkownika",
                column: "id_wybory");

            migrationBuilder.CreateIndex(
                name: "IX_Kandydat_id_wybory",
                table: "Kandydat",
                column: "id_wybory");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Administrator");

            migrationBuilder.DropTable(
                name: "GlosowanieWyborcze");

            migrationBuilder.DropTable(
                name: "GlosUzytkownika");

            migrationBuilder.DropTable(
                name: "Kandydat");

            migrationBuilder.DropTable(
                name: "Uzytkownik");

            migrationBuilder.DropTable(
                name: "DataWyborow");
        }
    }
}
