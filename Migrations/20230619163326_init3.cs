using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternetVotingApplication.Migrations
{
    /// <summary>
    /// Legacy migration from the original schema (2023). It altered a column of a table that the current
    /// InitialCreate migration creates later, so applying its original body on an empty database fails.
    /// It is kept as a no-op only so that the file can be deleted by the repository owner; it is safe to delete
    /// together with its Designer file.
    /// </summary>
    public partial class init3 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty: superseded by InitialCreate.
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty: superseded by InitialCreate.
        }
    }
}
