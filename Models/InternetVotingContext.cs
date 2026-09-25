using Microsoft.EntityFrameworkCore;

namespace InternetVotingApplication.Models
{
    public class InternetVotingContext(DbContextOptions<InternetVotingContext> options) : DbContext(options)
    {
        public DbSet<Administrator> Administrators => Set<Administrator>();

        public DbSet<DataWyborow> DataWyborows => Set<DataWyborow>();

        public DbSet<GlosUzytkownika> GlosUzytkownikas => Set<GlosUzytkownika>();

        public DbSet<GlosowanieWyborcze> GlosowanieWyborczes => Set<GlosowanieWyborcze>();

        public DbSet<Kandydat> Kandydats => Set<Kandydat>();

        public DbSet<Uzytkownik> Uzytkowniks => Set<Uzytkownik>();

        public DbSet<KotwicaLancucha> Kotwice => Set<KotwicaLancucha>();

        public DbSet<WiadomoscEmail> WiadomosciEmail => Set<WiadomoscEmail>();

        public DbSet<DziennikAudytu> DziennikAudytu => Set<DziennikAudytu>();

        public DbSet<WeryfikacjaLancucha> Weryfikacje => Set<WeryfikacjaLancucha>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Uzytkownik>(entity =>
            {
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.Pesel).IsUnique();
                entity.HasIndex(e => e.KodAktywacyjny);
                entity.HasIndex(e => e.TokenResetuHasla);
                entity.Property(e => e.Pesel).IsFixedLength().IsUnicode(false);
                entity.Property(e => e.Haslo).IsUnicode(false);
            });

            modelBuilder.Entity<Administrator>(entity =>
            {
                entity.HasIndex(e => e.IdUzytkownik).IsUnique();
                entity.HasOne(d => d.IdUzytkownikNavigation)
                    .WithMany(p => p.Administrators)
                    .HasForeignKey(d => d.IdUzytkownik)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_Administrator_Uzytkownik");
            });

            modelBuilder.Entity<DataWyborow>(entity =>
            {
                entity.HasIndex(e => e.Opis).IsUnique();
                entity.Property(e => e.HashGlowy).IsFixedLength().IsUnicode(false);
            });

            modelBuilder.Entity<KotwicaLancucha>(entity =>
            {
                entity.HasIndex(e => new { e.IdWybory, e.Data });
                entity.Property(e => e.HashGlowy).IsFixedLength().IsUnicode(false);
                entity.HasOne(d => d.IdWyboryNavigation)
                    .WithMany(p => p.Kotwice)
                    .HasForeignKey(d => d.IdWybory)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_KotwicaLancucha_DataWyborow");
            });

            modelBuilder.Entity<WeryfikacjaLancucha>(entity =>
            {
                entity.HasIndex(e => new { e.IdWybory, e.Data });
                entity.Property(e => e.HashGlowy).IsFixedLength().IsUnicode(false);
                entity.HasOne(d => d.IdWyboryNavigation)
                    .WithMany(p => p.Weryfikacje)
                    .HasForeignKey(d => d.IdWybory)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_WeryfikacjaLancucha_DataWyborow");
            });

            modelBuilder.Entity<WiadomoscEmail>(entity => entity.HasIndex(e => new { e.Wyslano, e.NastepnaProba }));

            modelBuilder.Entity<DziennikAudytu>(entity => entity.HasIndex(e => e.Data));

            modelBuilder.Entity<Kandydat>(entity =>
            {
                entity.HasIndex(e => new { e.IdWybory, e.Imie, e.Nazwisko }).IsUnique();
                entity.HasOne(d => d.IdWyboryNavigation)
                    .WithMany(p => p.Kandydats)
                    .HasForeignKey(d => d.IdWybory)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_Kandydat_DataWyborow");
            });

            modelBuilder.Entity<GlosowanieWyborcze>(entity =>
            {
                entity.HasIndex(e => e.Hash).IsUnique();
                entity.HasIndex(e => new { e.IdWybory, e.Indeks }).IsUnique();
                entity.Property(e => e.Hash).IsFixedLength().IsUnicode(false);
                entity.Property(e => e.Nonce).IsFixedLength().IsUnicode(false);
                entity.Property(e => e.Podpis).IsUnicode(false);
                entity.Property(e => e.IdKlucza).IsUnicode(false);

                entity.HasOne(d => d.IdKandydatNavigation)
                    .WithMany(p => p.GlosowanieWyborczes)
                    .HasForeignKey(d => d.IdKandydat)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_GlosowanieWyborcze_Kandydat");

                entity.HasOne(d => d.IdWyboryNavigation)
                    .WithMany(p => p.GlosowanieWyborczes)
                    .HasForeignKey(d => d.IdWybory)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_GlosowanieWyborcze_DataWyborow");
            });

            modelBuilder.Entity<GlosUzytkownika>(entity =>
            {
                entity.HasIndex(e => new { e.IdUzytkownik, e.IdWybory }).IsUnique();

                entity.HasOne(d => d.IdUzytkownikNavigation)
                    .WithMany(p => p.GlosUzytkownikas)
                    .HasForeignKey(d => d.IdUzytkownik)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_GlosUzytkownika_Uzytkownik");

                entity.HasOne(d => d.IdWyboryNavigation)
                    .WithMany(p => p.GlosUzytkownikas)
                    .HasForeignKey(d => d.IdWybory)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_GlosUzytkownika_Wybory");
            });
        }
    }
}
