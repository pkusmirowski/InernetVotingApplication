﻿// <auto-generated />
using System;
using InernetVotingApplication.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace InernetVotingApplication.Migrations
{
    [DbContext(typeof(InternetVotingContext))]
    [Migration("20210109180049_third")]
    partial class Third
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .UseIdentityColumns()
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("ProductVersion", "5.0.1");

            modelBuilder.Entity("InernetVotingApplication.Models.Administrator", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("id")
                        .UseIdentityColumn();

                    b.Property<int>("IdUzytkownik")
                        .HasColumnType("int")
                        .HasColumnName("id_uzytkownik");

                    b.HasKey("Id");

                    b.HasIndex("IdUzytkownik");

                    b.ToTable("Administrator");
                });

            modelBuilder.Entity("InernetVotingApplication.Models.DataWyborow", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("id")
                        .UseIdentityColumn();

                    b.Property<DateTime>("DataRozpoczecia")
                        .HasColumnType("datetime")
                        .HasColumnName("dataRozpoczecia");

                    b.Property<DateTime>("DataZakonczenia")
                        .HasColumnType("datetime")
                        .HasColumnName("dataZakonczenia");

                    b.Property<string>("Opis")
                        .IsRequired()
                        .IsUnicode(false)
                        .HasColumnType("varchar(max)")
                        .HasColumnName("opis");

                    b.HasKey("Id");

                    b.ToTable("DataWyborow");
                });

            modelBuilder.Entity("InernetVotingApplication.Models.GlosUzytkownika", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("id")
                        .UseIdentityColumn();

                    b.Property<bool>("Glos")
                        .HasColumnType("bit")
                        .HasColumnName("glos");

                    b.Property<int>("IdUzytkownik")
                        .HasColumnType("int")
                        .HasColumnName("id_uzytkownik");

                    b.Property<int>("IdWybory")
                        .HasColumnType("int")
                        .HasColumnName("id_wybory");

                    b.HasKey("Id");

                    b.HasIndex("IdUzytkownik");

                    b.HasIndex("IdWybory");

                    b.ToTable("GlosUzytkownika");
                });

            modelBuilder.Entity("InernetVotingApplication.Models.GlosowanieWyborcze", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("id")
                        .UseIdentityColumn();

                    b.Property<bool>("Glos")
                        .HasColumnType("bit")
                        .HasColumnName("glos");

                    b.Property<string>("Hash")
                        .IsRequired()
                        .IsUnicode(false)
                        .HasColumnType("varchar(max)")
                        .HasColumnName("hash");

                    b.Property<int>("IdKandydat")
                        .HasColumnType("int")
                        .HasColumnName("id_kandydat");

                    b.Property<int?>("IdPoprzednie")
                        .HasColumnType("int")
                        .HasColumnName("id_poprzednie");

                    b.Property<int>("IdWybory")
                        .HasColumnType("int")
                        .HasColumnName("id_wybory");

                    b.HasKey("Id");

                    b.HasIndex("IdKandydat");

                    b.HasIndex("IdWybory");

                    b.ToTable("GlosowanieWyborcze");
                });

            modelBuilder.Entity("InernetVotingApplication.Models.Kandydat", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("id")
                        .UseIdentityColumn();

                    b.Property<int>("IdWybory")
                        .HasColumnType("int")
                        .HasColumnName("id_wybory");

                    b.Property<string>("Imie")
                        .IsRequired()
                        .HasMaxLength(50)
                        .IsUnicode(false)
                        .HasColumnType("varchar(50)")
                        .HasColumnName("imie");

                    b.Property<string>("Nazwisko")
                        .IsRequired()
                        .HasMaxLength(50)
                        .IsUnicode(false)
                        .HasColumnType("varchar(50)")
                        .HasColumnName("nazwisko");

                    b.HasKey("Id");

                    b.HasIndex("IdWybory");

                    b.ToTable("Kandydat");
                });

            modelBuilder.Entity("InernetVotingApplication.Models.Uzytkownik", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("id")
                        .UseIdentityColumn();

                    b.Property<DateTime>("DataUrodzenia")
                        .HasColumnType("date")
                        .HasColumnName("dataUrodzenia");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(89)
                        .IsUnicode(false)
                        .HasColumnType("varchar(89)")
                        .HasColumnName("email");

                    b.Property<string>("Haslo")
                        .IsRequired()
                        .IsUnicode(false)
                        .HasColumnType("varchar(max)")
                        .HasColumnName("haslo");

                    b.Property<string>("Imie")
                        .IsRequired()
                        .HasMaxLength(50)
                        .IsUnicode(false)
                        .HasColumnType("varchar(50)")
                        .HasColumnName("imie");

                    b.Property<bool?>("JestAktywne")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasColumnName("jestAktywne")
                        .HasDefaultValueSql("((1))");

                    b.Property<Guid>("KodAktywacyjny")
                        .HasColumnType("uniqueidentifier")
                        .HasColumnName("kodAktywacyjny");

                    b.Property<string>("Nazwisko")
                        .IsRequired()
                        .HasMaxLength(50)
                        .IsUnicode(false)
                        .HasColumnType("varchar(50)")
                        .HasColumnName("nazwisko");

                    b.Property<string>("Pesel")
                        .IsRequired()
                        .HasMaxLength(11)
                        .HasColumnType("nchar(11)")
                        .HasColumnName("pesel")
                        .IsFixedLength(true);

                    b.HasKey("Id");

                    b.ToTable("Uzytkownik");
                });

            modelBuilder.Entity("InernetVotingApplication.Models.Administrator", b =>
                {
                    b.HasOne("InernetVotingApplication.Models.Uzytkownik", "IdUzytkownikNavigation")
                        .WithMany("Administrators")
                        .HasForeignKey("IdUzytkownik")
                        .HasConstraintName("FK_Administrator_Uzytkownik")
                        .IsRequired();

                    b.Navigation("IdUzytkownikNavigation");
                });

            modelBuilder.Entity("InernetVotingApplication.Models.GlosUzytkownika", b =>
                {
                    b.HasOne("InernetVotingApplication.Models.Uzytkownik", "IdUzytkownikNavigation")
                        .WithMany("GlosUzytkownikas")
                        .HasForeignKey("IdUzytkownik")
                        .HasConstraintName("FK_GlosUzytkownika_Uzytkownik")
                        .IsRequired();

                    b.HasOne("InernetVotingApplication.Models.DataWyborow", "IdWyboryNavigation")
                        .WithMany("GlosUzytkownikas")
                        .HasForeignKey("IdWybory")
                        .HasConstraintName("FK_GlosUzytkownika_Wybory")
                        .IsRequired();

                    b.Navigation("IdUzytkownikNavigation");

                    b.Navigation("IdWyboryNavigation");
                });

            modelBuilder.Entity("InernetVotingApplication.Models.GlosowanieWyborcze", b =>
                {
                    b.HasOne("InernetVotingApplication.Models.Kandydat", "IdKandydatNavigation")
                        .WithMany("GlosowanieWyborczes")
                        .HasForeignKey("IdKandydat")
                        .HasConstraintName("FK_GlosowanieWyborcze_Kandydat")
                        .IsRequired();

                    b.HasOne("InernetVotingApplication.Models.DataWyborow", "IdWyboryNavigation")
                        .WithMany("GlosowanieWyborczes")
                        .HasForeignKey("IdWybory")
                        .HasConstraintName("FK_GlosowanieWyborcze_DataWyborow")
                        .IsRequired();

                    b.Navigation("IdKandydatNavigation");

                    b.Navigation("IdWyboryNavigation");
                });

            modelBuilder.Entity("InernetVotingApplication.Models.Kandydat", b =>
                {
                    b.HasOne("InernetVotingApplication.Models.DataWyborow", "IdWyboryNavigation")
                        .WithMany("Kandydats")
                        .HasForeignKey("IdWybory")
                        .HasConstraintName("FK_Kandydat_DataWyborow")
                        .IsRequired();

                    b.Navigation("IdWyboryNavigation");
                });

            modelBuilder.Entity("InernetVotingApplication.Models.DataWyborow", b =>
                {
                    b.Navigation("GlosowanieWyborczes");

                    b.Navigation("GlosUzytkownikas");

                    b.Navigation("Kandydats");
                });

            modelBuilder.Entity("InernetVotingApplication.Models.Kandydat", b =>
                {
                    b.Navigation("GlosowanieWyborczes");
                });

            modelBuilder.Entity("InernetVotingApplication.Models.Uzytkownik", b =>
                {
                    b.Navigation("Administrators");

                    b.Navigation("GlosUzytkownikas");
                });
#pragma warning restore 612, 618
        }
    }
}
