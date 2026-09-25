using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternetVotingApplication.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DataWyborow",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    dataRozpoczecia = table.Column<DateTime>(type: "datetime2", nullable: false),
                    dataZakonczenia = table.Column<DateTime>(type: "datetime2", nullable: false),
                    opis = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    hashGlowy = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: true),
                    liczbaBlokow = table.Column<int>(type: "int", nullable: false),
                    wersja = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataWyborow", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "DziennikAudytu",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    data = table.Column<DateTime>(type: "datetime2", nullable: false),
                    id_uzytkownik = table.Column<int>(type: "int", nullable: true),
                    akcja = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    szczegoly = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DziennikAudytu", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Uzytkownik",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    imie = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    nazwisko = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    pesel = table.Column<string>(type: "char(11)", unicode: false, fixedLength: true, maxLength: 11, nullable: false),
                    email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    dataUrodzenia = table.Column<DateTime>(type: "date", nullable: false),
                    haslo = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    jestAktywne = table.Column<bool>(type: "bit", nullable: false),
                    kodAktywacyjny = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    tokenResetuHasla = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    tokenResetuWygasa = table.Column<DateTime>(type: "datetime2", nullable: true),
                    nieudaneLogowania = table.Column<int>(type: "int", nullable: false),
                    zablokowaneDo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    dataRejestracji = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Uzytkownik", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "WiadomoscEmail",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    odbiorca = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    temat = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    tresc = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    utworzono = table.Column<DateTime>(type: "datetime2", nullable: false),
                    wyslano = table.Column<DateTime>(type: "datetime2", nullable: true),
                    proby = table.Column<int>(type: "int", nullable: false),
                    nastepnaProba = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ostatniBlad = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WiadomoscEmail", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Kandydat",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    imie = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    nazwisko = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
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
                name: "KotwicaLancucha",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_wybory = table.Column<int>(type: "int", nullable: false),
                    liczbaBlokow = table.Column<int>(type: "int", nullable: false),
                    hashGlowy = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: true),
                    data = table.Column<DateTime>(type: "datetime2", nullable: false),
                    powod = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    odbiorcy = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    podpis = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KotwicaLancucha", x => x.id);
                    table.ForeignKey(
                        name: "FK_KotwicaLancucha_DataWyborow",
                        column: x => x.id_wybory,
                        principalTable: "DataWyborow",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WeryfikacjaLancucha",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_wybory = table.Column<int>(type: "int", nullable: false),
                    data = table.Column<DateTime>(type: "datetime2", nullable: false),
                    poprawny = table.Column<bool>(type: "bit", nullable: false),
                    liczbaBlokow = table.Column<int>(type: "int", nullable: false),
                    hashGlowy = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: true),
                    wyzwalacz = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    szczegoly = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeryfikacjaLancucha", x => x.id);
                    table.ForeignKey(
                        name: "FK_WeryfikacjaLancucha_DataWyborow",
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
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GlosUzytkownika",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_uzytkownik = table.Column<int>(type: "int", nullable: false),
                    id_wybory = table.Column<int>(type: "int", nullable: false),
                    dataOddania = table.Column<DateTime>(type: "datetime2", nullable: false)
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
                    indeks = table.Column<int>(type: "int", nullable: false),
                    id_kandydat = table.Column<int>(type: "int", nullable: false),
                    id_wybory = table.Column<int>(type: "int", nullable: false),
                    id_poprzednie = table.Column<int>(type: "int", nullable: true),
                    znacznikCzasu = table.Column<DateTime>(type: "datetime2", nullable: false),
                    nonce = table.Column<string>(type: "char(32)", unicode: false, fixedLength: true, maxLength: 32, nullable: false),
                    hash = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    podpis = table.Column<string>(type: "varchar(128)", unicode: false, maxLength: 128, nullable: false),
                    idKlucza = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false)
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
                column: "id_uzytkownik",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DataWyborow_opis",
                table: "DataWyborow",
                column: "opis",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DziennikAudytu_data",
                table: "DziennikAudytu",
                column: "data");

            migrationBuilder.CreateIndex(
                name: "IX_GlosowanieWyborcze_hash",
                table: "GlosowanieWyborcze",
                column: "hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GlosowanieWyborcze_id_kandydat",
                table: "GlosowanieWyborcze",
                column: "id_kandydat");

            migrationBuilder.CreateIndex(
                name: "IX_GlosowanieWyborcze_id_wybory_indeks",
                table: "GlosowanieWyborcze",
                columns: new[] { "id_wybory", "indeks" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GlosUzytkownika_id_uzytkownik_id_wybory",
                table: "GlosUzytkownika",
                columns: new[] { "id_uzytkownik", "id_wybory" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GlosUzytkownika_id_wybory",
                table: "GlosUzytkownika",
                column: "id_wybory");

            migrationBuilder.CreateIndex(
                name: "IX_Kandydat_id_wybory_imie_nazwisko",
                table: "Kandydat",
                columns: new[] { "id_wybory", "imie", "nazwisko" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KotwicaLancucha_id_wybory_data",
                table: "KotwicaLancucha",
                columns: new[] { "id_wybory", "data" });

            migrationBuilder.CreateIndex(
                name: "IX_Uzytkownik_email",
                table: "Uzytkownik",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Uzytkownik_kodAktywacyjny",
                table: "Uzytkownik",
                column: "kodAktywacyjny");

            migrationBuilder.CreateIndex(
                name: "IX_Uzytkownik_pesel",
                table: "Uzytkownik",
                column: "pesel",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Uzytkownik_tokenResetuHasla",
                table: "Uzytkownik",
                column: "tokenResetuHasla");

            migrationBuilder.CreateIndex(
                name: "IX_WeryfikacjaLancucha_id_wybory_data",
                table: "WeryfikacjaLancucha",
                columns: new[] { "id_wybory", "data" });

            migrationBuilder.CreateIndex(
                name: "IX_WiadomoscEmail_wyslano_nastepnaProba",
                table: "WiadomoscEmail",
                columns: new[] { "wyslano", "nastepnaProba" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Administrator");

            migrationBuilder.DropTable(
                name: "DziennikAudytu");

            migrationBuilder.DropTable(
                name: "GlosowanieWyborcze");

            migrationBuilder.DropTable(
                name: "GlosUzytkownika");

            migrationBuilder.DropTable(
                name: "KotwicaLancucha");

            migrationBuilder.DropTable(
                name: "WeryfikacjaLancucha");

            migrationBuilder.DropTable(
                name: "WiadomoscEmail");

            migrationBuilder.DropTable(
                name: "Kandydat");

            migrationBuilder.DropTable(
                name: "Uzytkownik");

            migrationBuilder.DropTable(
                name: "DataWyborow");
        }
    }
}
