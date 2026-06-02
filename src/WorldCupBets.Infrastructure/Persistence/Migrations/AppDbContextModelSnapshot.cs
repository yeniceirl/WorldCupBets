#nullable disable

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace WorldCupBets.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
public partial class AppDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder
            .HasAnnotation("ProductVersion", "10.0.8")
            .HasAnnotation("Relational:MaxIdentifierLength", 63);

        NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

        modelBuilder.Entity("WorldCupBets.Domain.Entities.ChampionBet", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("integer")
                    .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                b.Property<DateTime>("PlacedAtUtc")
                    .HasColumnType("timestamp with time zone");

                b.Property<int>("StakeAmountCc")
                    .HasColumnType("integer");

                b.Property<string>("TeamName")
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnType("character varying(100)");

                b.Property<int>("UserId")
                    .HasColumnType("integer");

                b.HasKey("Id");

                b.HasIndex("UserId")
                    .IsUnique();

                b.ToTable("champion_bets", (string)null);
            });

        modelBuilder.Entity("WorldCupBets.Domain.Entities.LookupItem", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("integer")
                    .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                b.Property<string>("Category")
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasColumnType("character varying(50)");

                b.Property<string>("Detail")
                    .HasColumnType("text");

                b.Property<bool>("IsActive")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("boolean")
                    .HasDefaultValue(true);

                b.Property<string>("Key")
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasColumnType("character varying(50)");

                b.Property<int>("SortOrder")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("integer")
                    .HasDefaultValue(0);

                b.Property<string>("Value")
                    .IsRequired()
                    .HasMaxLength(200)
                    .HasColumnType("character varying(200)");

                b.HasKey("Id");

                b.HasIndex("Category", "Key")
                    .IsUnique();

                b.ToTable("lookup_items", (string)null);
            });

        modelBuilder.Entity("WorldCupBets.Domain.Entities.Match", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("integer")
                    .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                b.Property<string>("AwayTeamName")
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnType("character varying(100)");

                b.Property<string>("HomeTeamName")
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnType("character varying(100)");

                b.Property<WorldCupBets.Domain.Entities.MatchPhase>("Phase")
                    .HasColumnType("character varying(32)")
                    .HasColumnName("Stage")
                    .HasMaxLength(32);

                b.Property<DateTime>("StartsAtUtc")
                    .HasColumnType("timestamp with time zone");

                b.Property<string>("Venue")
                    .IsRequired()
                    .HasMaxLength(150)
                    .HasColumnType("character varying(150)");

                b.HasKey("Id");

                b.ToTable("matches", (string)null);

                b.HasData(
                    new
                    {
                        Id = 1,
                        AwayTeamName = "Japan",
                        HomeTeamName = "Argentina",
                        Phase = WorldCupBets.Domain.Entities.MatchPhase.GroupStage,
                        StartsAtUtc = new DateTime(2026, 6, 14, 18, 0, 0, DateTimeKind.Utc),
                        Venue = "MetLife Stadium"
                    },
                    new
                    {
                        Id = 2,
                        AwayTeamName = "Mexico",
                        HomeTeamName = "Spain",
                        Phase = WorldCupBets.Domain.Entities.MatchPhase.GroupStage,
                        StartsAtUtc = new DateTime(2026, 6, 15, 21, 0, 0, DateTimeKind.Utc),
                        Venue = "Estadio Akron"
                    },
                    new
                    {
                        Id = 3,
                        AwayTeamName = "France",
                        HomeTeamName = "United States",
                        Phase = WorldCupBets.Domain.Entities.MatchPhase.GroupStage,
                        StartsAtUtc = new DateTime(2026, 6, 16, 1, 0, 0, DateTimeKind.Utc),
                        Venue = "AT&T Stadium"
                    });
            });

        modelBuilder.Entity("WorldCupBets.Domain.Entities.MatchBet", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("integer")
                    .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                b.Property<int>("MatchId")
                    .HasColumnType("integer");

                b.Property<DateTime>("PlacedAtUtc")
                    .HasColumnType("timestamp with time zone");

                b.Property<WorldCupBets.Domain.Entities.MatchBetSelection>("Selection")
                    .HasColumnType("character varying(16)")
                    .HasMaxLength(16);

                b.Property<int>("StakeAmountCc")
                    .HasColumnType("integer");

                b.Property<int>("UserId")
                    .HasColumnType("integer");

                b.HasKey("Id");

                b.HasIndex("MatchId");

                b.HasIndex("UserId", "MatchId")
                    .IsUnique();

                b.ToTable("match_bets", (string)null);
            });

        modelBuilder.Entity("WorldCupBets.Domain.Entities.Role", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("integer")
                    .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                b.Property<string>("Name")
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasColumnType("character varying(50)");

                b.HasKey("Id");

                b.HasIndex("Name")
                    .IsUnique();

                b.ToTable("roles", (string)null);

                b.HasData(
                    new
                    {
                        Id = 1,
                        Name = "Admin"
                    },
                    new
                    {
                        Id = 2,
                        Name = "Bettor"
                    });
            });

        modelBuilder.Entity("WorldCupBets.Domain.Entities.User", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("integer")
                    .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                b.Property<int>("CurrentBalanceCc")
                    .HasColumnType("integer");

                b.Property<string>("DisplayName")
                    .IsRequired()
                    .HasMaxLength(200)
                    .HasColumnType("character varying(200)");

                b.Property<string>("Email")
                    .IsRequired()
                    .HasMaxLength(320)
                    .HasColumnType("character varying(320)");

                b.Property<string>("GoogleSubject")
                    .IsRequired()
                    .HasMaxLength(200)
                    .HasColumnType("character varying(200)");

                b.Property<int>("RescueCount")
                    .HasColumnType("integer");

                b.Property<int>("RescueDebtCc")
                    .HasColumnType("integer");

                b.HasKey("Id");

                b.HasIndex("Email")
                    .IsUnique();

                b.HasIndex("GoogleSubject")
                    .IsUnique();

                b.ToTable("users", (string)null);
            });

        modelBuilder.Entity("WorldCupBets.Domain.Entities.UserRole", b =>
            {
                b.Property<int>("UserId")
                    .HasColumnType("integer");

                b.Property<int>("RoleId")
                    .HasColumnType("integer");

                b.HasKey("UserId", "RoleId");

                b.HasIndex("RoleId");

                b.ToTable("user_roles", (string)null);
            });

        modelBuilder.Entity("WorldCupBets.Domain.Entities.ChampionBet", b =>
            {
                b.HasOne("WorldCupBets.Domain.Entities.User", "User")
                    .WithMany()
                    .HasForeignKey("UserId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.Navigation("User");
            });

        modelBuilder.Entity("WorldCupBets.Domain.Entities.MatchBet", b =>
            {
                b.HasOne("WorldCupBets.Domain.Entities.Match", "Match")
                    .WithMany()
                    .HasForeignKey("MatchId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.HasOne("WorldCupBets.Domain.Entities.User", "User")
                    .WithMany()
                    .HasForeignKey("UserId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.Navigation("Match");

                b.Navigation("User");
            });

        modelBuilder.Entity("WorldCupBets.Domain.Entities.UserRole", b =>
            {
                b.HasOne("WorldCupBets.Domain.Entities.Role", "Role")
                    .WithMany("UserRoles")
                    .HasForeignKey("RoleId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.HasOne("WorldCupBets.Domain.Entities.User", "User")
                    .WithMany("UserRoles")
                    .HasForeignKey("UserId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.Navigation("Role");

                b.Navigation("User");
            });

        modelBuilder.Entity("WorldCupBets.Domain.Entities.Role", b =>
            {
                b.Navigation("UserRoles");
            });

        modelBuilder.Entity("WorldCupBets.Domain.Entities.User", b =>
            {
                b.Navigation("UserRoles");
            });
#pragma warning restore 612, 618
    }
}
