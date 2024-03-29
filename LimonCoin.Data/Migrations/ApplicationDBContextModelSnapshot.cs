﻿// <auto-generated />
using System;
using System.Collections.Generic;
using LimonCoin.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LimonCoin.Data.Migrations
{
    [DbContext(typeof(ApplicationDBContext))]
    partial class ApplicationDBContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.1")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("LimonCoin.Models.ApplicationUser", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<List<Guid>>("AwardedTasks")
                        .IsRequired()
                        .HasColumnType("uuid[]");

                    b.Property<int>("ClickerLevel")
                        .HasColumnType("integer");

                    b.Property<decimal>("Coins")
                        .HasColumnType("numeric(20,0)");

                    b.Property<long>("CoinsPerClick")
                        .HasColumnType("bigint");

                    b.Property<decimal>("CoinsThisDay")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("CoinsThisWeek")
                        .HasColumnType("numeric(20,0)");

                    b.Property<List<Guid>>("CompletedTasks")
                        .IsRequired()
                        .HasColumnType("uuid[]");

                    b.Property<long>("Energy")
                        .HasColumnType("bigint");

                    b.Property<int>("EnergyCapacityLevel")
                        .HasColumnType("integer");

                    b.Property<long>("EnergyPerSecond")
                        .HasColumnType("bigint");

                    b.Property<int>("EnergyRecoveryLevel")
                        .HasColumnType("integer");

                    b.Property<DateTime>("LastTimeClicked")
                        .HasColumnType("timestamp with time zone");

                    b.Property<long>("MaxEnergy")
                        .HasColumnType("bigint");

                    b.Property<int?>("ReferrerId")
                        .HasColumnType("integer");

                    b.Property<long>("TelegramId")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("ReferrerId");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("LimonCoin.Models.ApplicationUser", b =>
                {
                    b.HasOne("LimonCoin.Models.ApplicationUser", "Referrer")
                        .WithMany()
                        .HasForeignKey("ReferrerId");

                    b.Navigation("Referrer");
                });
#pragma warning restore 612, 618
        }
    }
}
