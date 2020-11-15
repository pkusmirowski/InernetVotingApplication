using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

#nullable disable

namespace InernetVotingApplication.Models
{
    public partial class InternetVotingContext : DbContext
    {
        public InternetVotingContext()
        {
        }

        public InternetVotingContext(DbContextOptions<InternetVotingContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Administrator> Administrators { get; set; }
        public virtual DbSet<DataWyborow> DataWyborows { get; set; }
        public virtual DbSet<GlosUzytkownika> GlosUzytkownikas { get; set; }
        public virtual DbSet<GlosowanieWyborcze> GlosowanieWyborczes { get; set; }
        public virtual DbSet<Kandydat> Kandydats { get; set; }
        public virtual DbSet<Uzytkownik> Uzytkowniks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Administrator>(entity =>
            {
                entity.HasOne(d => d.IdUzytkownikNavigation)
                    .WithMany(p => p.Administrators)
                    .HasForeignKey(d => d.IdUzytkownik)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Administrator_Uzytkownik");
            });

            modelBuilder.Entity<DataWyborow>(entity =>
            {
                entity.Property(e => e.Opis).IsUnicode(false);
            });

            modelBuilder.Entity<GlosUzytkownika>(entity =>
            {
                entity.HasOne(d => d.IdUzytkownikNavigation)
                    .WithMany(p => p.GlosUzytkownikas)
                    .HasForeignKey(d => d.IdUzytkownik)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_GlosUzytkownika_Uzytkownik");

                entity.HasOne(d => d.IdWyboryNavigation)
                    .WithMany(p => p.GlosUzytkownikas)
                    .HasForeignKey(d => d.IdWybory)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_GlosUzytkownika_Wybory");
            });

            modelBuilder.Entity<GlosowanieWyborcze>(entity =>
            {
                entity.Property(e => e.Hash).IsUnicode(false);

                entity.HasOne(d => d.IdKandydatNavigation)
                    .WithMany(p => p.GlosowanieWyborczes)
                    .HasForeignKey(d => d.IdKandydat)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_GlosNaKandydata_Kandydat");

                entity.HasOne(d => d.IdWyboryNavigation)
                    .WithMany(p => p.GlosowanieWyborczes)
                    .HasForeignKey(d => d.IdWybory)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_GlosNaKandydata_Wybory");
            });

            modelBuilder.Entity<Kandydat>(entity =>
            {
                entity.Property(e => e.Imie).IsUnicode(false);

                entity.Property(e => e.Nazwisko).IsUnicode(false);

                entity.HasOne(d => d.IdWyboryNavigation)
                    .WithMany(p => p.Kandydats)
                    .HasForeignKey(d => d.IdWybory)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Kandydat_DataWyborow");
            });

            modelBuilder.Entity<Uzytkownik>(entity =>
            {
                entity.Property(e => e.Haslo).IsUnicode(false);

                entity.Property(e => e.Imie).IsUnicode(false);

                entity.Property(e => e.JestAktywne).HasDefaultValueSql("((1))");

                entity.Property(e => e.Nazwisko).IsUnicode(false);

                entity.Property(e => e.NumerDowodu).IsUnicode(false);

                entity.Property(e => e.Pesel).IsFixedLength(true);
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
