﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using SteelBot.Database;

#nullable disable

namespace SteelBot.Migrations
{
    [DbContext(typeof(SteelBotContext))]
    [Migration("20220217232551_ReducePetNameLength")]
    partial class ReducePetNameLength
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.2")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("SteelBot.Database.Models.CommandStatistic", b =>
                {
                    b.Property<long>("RowId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("RowId"));

                    b.Property<string>("CommandName")
                        .HasColumnType("text");

                    b.Property<DateTime>("LastUsed")
                        .HasColumnType("timestamp with time zone");

                    b.Property<long>("UsageCount")
                        .HasColumnType("bigint");

                    b.HasKey("RowId");

                    b.HasIndex("CommandName")
                        .IsUnique();

                    b.ToTable("CommandStatistics");
                });

            modelBuilder.Entity("SteelBot.Database.Models.ExceptionLog", b =>
                {
                    b.Property<long>("RowId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("RowId"));

                    b.Property<string>("FullDetail")
                        .HasColumnType("text");

                    b.Property<string>("Message")
                        .HasColumnType("text");

                    b.Property<string>("SourceMethod")
                        .HasColumnType("text");

                    b.Property<string>("StackTrace")
                        .HasColumnType("text");

                    b.Property<DateTime>("Timestamp")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("RowId");

                    b.ToTable("LoggedErrors");
                });

            modelBuilder.Entity("SteelBot.Database.Models.Guild", b =>
                {
                    b.Property<long>("RowId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("RowId"));

                    b.Property<int>("BadBotVotes")
                        .HasColumnType("integer");

                    b.Property<DateTime>("BotAddedTo")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("CommandPrefix")
                        .ValueGeneratedOnAdd()
                        .HasMaxLength(20)
                        .HasColumnType("character varying(20)")
                        .HasDefaultValue("+");

                    b.Property<decimal>("DiscordId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<int>("GoodBotVotes")
                        .HasColumnType("integer");

                    b.Property<decimal?>("LevelAnnouncementChannelId")
                        .HasColumnType("numeric(20,0)");

                    b.HasKey("RowId");

                    b.HasIndex("DiscordId")
                        .IsUnique();

                    b.ToTable("Guilds");
                });

            modelBuilder.Entity("SteelBot.Database.Models.OwnedStock", b =>
                {
                    b.Property<long>("RowId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("RowId"));

                    b.Property<decimal>("AmountOwned")
                        .HasColumnType("numeric(38,20)");

                    b.Property<DateTime>("LastUpdated")
                        .HasColumnType("timestamp with time zone");

                    b.Property<long>("ParentPortfolioRowId")
                        .HasColumnType("bigint");

                    b.Property<string>("Symbol")
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.HasKey("RowId");

                    b.HasIndex("ParentPortfolioRowId");

                    b.ToTable("OwnedStocks");
                });

            modelBuilder.Entity("SteelBot.Database.Models.Pets.Pet", b =>
                {
                    b.Property<long>("RowId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("RowId"));

                    b.Property<DateTime>("BornAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("CurrentLevel")
                        .HasColumnType("integer");

                    b.Property<double>("EarnedXp")
                        .HasColumnType("double precision");

                    b.Property<DateTime>("FoundAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Name")
                        .HasMaxLength(70)
                        .HasColumnType("character varying(70)");

                    b.Property<decimal>("OwnerDiscordId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<int>("Priority")
                        .HasColumnType("integer");

                    b.Property<int>("Rarity")
                        .HasColumnType("integer");

                    b.Property<int>("Size")
                        .HasColumnType("integer");

                    b.Property<int>("Species")
                        .HasColumnType("integer");

                    b.HasKey("RowId");

                    b.HasIndex("OwnerDiscordId");

                    b.ToTable("Pets");
                });

            modelBuilder.Entity("SteelBot.Database.Models.Pets.PetAttribute", b =>
                {
                    b.Property<long>("RowId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("RowId"));

                    b.Property<string>("Description")
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)");

                    b.Property<string>("Name")
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)");

                    b.Property<long>("PetId")
                        .HasColumnType("bigint");

                    b.HasKey("RowId");

                    b.HasIndex("PetId");

                    b.ToTable("PetAttributes");
                });

            modelBuilder.Entity("SteelBot.Database.Models.Pets.PetBonus", b =>
                {
                    b.Property<long>("RowId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("RowId"));

                    b.Property<int>("BonusType")
                        .HasColumnType("integer");

                    b.Property<double>("PercentageValue")
                        .HasColumnType("double precision");

                    b.Property<long>("PetId")
                        .HasColumnType("bigint");

                    b.HasKey("RowId");

                    b.HasIndex("PetId");

                    b.ToTable("PetBonuses");
                });

            modelBuilder.Entity("SteelBot.Database.Models.Poll", b =>
                {
                    b.Property<long>("RowId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("RowId"));

                    b.Property<decimal>("ChannelId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<bool>("IsLockedPoll")
                        .HasColumnType("boolean");

                    b.Property<decimal>("MessageId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<string>("Title")
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)");

                    b.Property<long>("UserRowId")
                        .HasColumnType("bigint");

                    b.HasKey("RowId");

                    b.HasIndex("UserRowId");

                    b.ToTable("Polls");
                });

            modelBuilder.Entity("SteelBot.Database.Models.PollOption", b =>
                {
                    b.Property<long>("RowId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("RowId"));

                    b.Property<int>("OptionNumber")
                        .HasColumnType("integer");

                    b.Property<string>("OptionText")
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)");

                    b.Property<long>("PollRowId")
                        .HasColumnType("bigint");

                    b.HasKey("RowId");

                    b.HasIndex("PollRowId");

                    b.ToTable("PollOptions");
                });

            modelBuilder.Entity("SteelBot.Database.Models.RankRole", b =>
                {
                    b.Property<long>("RowId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("RowId"));

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<long>("GuildRowId")
                        .HasColumnType("bigint");

                    b.Property<int>("LevelRequired")
                        .HasColumnType("integer");

                    b.Property<string>("RoleName")
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)");

                    b.HasKey("RowId");

                    b.HasIndex("GuildRowId");

                    b.ToTable("RankRoles");
                });

            modelBuilder.Entity("SteelBot.Database.Models.SelfRole", b =>
                {
                    b.Property<long>("RowId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("RowId"));

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Description")
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)");

                    b.Property<long>("GuildRowId")
                        .HasColumnType("bigint");

                    b.Property<bool>("Hidden")
                        .HasColumnType("boolean");

                    b.Property<string>("RoleName")
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)");

                    b.HasKey("RowId");

                    b.HasIndex("GuildRowId");

                    b.ToTable("SelfRoles");
                });

            modelBuilder.Entity("SteelBot.Database.Models.StockPortfolio", b =>
                {
                    b.Property<long>("RowId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("RowId"));

                    b.Property<DateTime>("Created")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime>("LastUpdated")
                        .HasColumnType("timestamp with time zone");

                    b.Property<long>("OwnerRowId")
                        .HasColumnType("bigint");

                    b.HasKey("RowId");

                    b.HasIndex("OwnerRowId")
                        .IsUnique();

                    b.ToTable("StockPortfolios");
                });

            modelBuilder.Entity("SteelBot.Database.Models.StockPortfolioSnapshot", b =>
                {
                    b.Property<long>("RowId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("RowId"));

                    b.Property<long>("ParentPortfolioRowId")
                        .HasColumnType("bigint");

                    b.Property<DateTime>("SnapshotTaken")
                        .HasColumnType("timestamp with time zone");

                    b.Property<decimal>("TotalValueDollars")
                        .HasColumnType("numeric(24,4)");

                    b.HasKey("RowId");

                    b.HasIndex("ParentPortfolioRowId");

                    b.ToTable("StockPortfolioSnapshots");
                });

            modelBuilder.Entity("SteelBot.Database.Models.Trigger", b =>
                {
                    b.Property<long>("RowId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("RowId"));

                    b.Property<decimal?>("ChannelDiscordId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<DateTime>("Created")
                        .HasColumnType("timestamp with time zone");

                    b.Property<long>("CreatorRowId")
                        .HasColumnType("bigint");

                    b.Property<bool>("ExactMatch")
                        .HasColumnType("boolean");

                    b.Property<long>("GuildRowId")
                        .HasColumnType("bigint");

                    b.Property<string>("Response")
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)");

                    b.Property<long>("TimesActivated")
                        .HasColumnType("bigint");

                    b.Property<string>("TriggerText")
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)");

                    b.HasKey("RowId");

                    b.HasIndex("CreatorRowId");

                    b.HasIndex("GuildRowId");

                    b.ToTable("Triggers");
                });

            modelBuilder.Entity("SteelBot.Database.Models.User", b =>
                {
                    b.Property<long>("RowId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("RowId"));

                    b.Property<DateTime?>("AfkStartTime")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("CurrentLevel")
                        .HasColumnType("integer");

                    b.Property<long?>("CurrentRankRoleRowId")
                        .HasColumnType("bigint");

                    b.Property<DateTime?>("DeafenedStartTime")
                        .HasColumnType("timestamp with time zone");

                    b.Property<decimal>("DeafenedXpEarned")
                        .HasColumnType("numeric(20,0)");

                    b.Property<DateTime?>("DisconnectedStartTime")
                        .HasColumnType("timestamp with time zone");

                    b.Property<decimal>("DisconnectedXpEarned")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("DiscordId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<long>("GuildRowId")
                        .HasColumnType("bigint");

                    b.Property<DateTime>("LastActivity")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime?>("LastMessageSent")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime?>("LastXpEarningMessage")
                        .HasColumnType("timestamp with time zone");

                    b.Property<long>("MessageCount")
                        .HasColumnType("bigint");

                    b.Property<decimal>("MessageXpEarned")
                        .HasColumnType("numeric(20,0)");

                    b.Property<DateTime?>("MutedStartTime")
                        .HasColumnType("timestamp with time zone");

                    b.Property<decimal>("MutedXpEarned")
                        .HasColumnType("numeric(20,0)");

                    b.Property<DateTime?>("StreamingStartTime")
                        .HasColumnType("timestamp with time zone");

                    b.Property<decimal>("StreamingXpEarned")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("TimeSpentAfkSeconds")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("TimeSpentDeafenedSeconds")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("TimeSpentDisconnectedSeconds")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("TimeSpentInVoiceSeconds")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("TimeSpentMutedSeconds")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("TimeSpentOnVideoSeconds")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("TimeSpentStreamingSeconds")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("TotalMessageLength")
                        .HasColumnType("numeric(20,0)");

                    b.Property<DateTime>("UserFirstSeen")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime?>("VideoStartTime")
                        .HasColumnType("timestamp with time zone");

                    b.Property<decimal>("VideoXpEarned")
                        .HasColumnType("numeric(20,0)");

                    b.Property<DateTime?>("VoiceStartTime")
                        .HasColumnType("timestamp with time zone");

                    b.Property<decimal>("VoiceXpEarned")
                        .HasColumnType("numeric(20,0)");

                    b.HasKey("RowId");

                    b.HasIndex("CurrentRankRoleRowId");

                    b.HasIndex("GuildRowId");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("SteelBot.Database.Models.OwnedStock", b =>
                {
                    b.HasOne("SteelBot.Database.Models.StockPortfolio", "ParentPortfolio")
                        .WithMany("OwnedStock")
                        .HasForeignKey("ParentPortfolioRowId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ParentPortfolio");
                });

            modelBuilder.Entity("SteelBot.Database.Models.Pets.PetAttribute", b =>
                {
                    b.HasOne("SteelBot.Database.Models.Pets.Pet", "Pet")
                        .WithMany("Attributes")
                        .HasForeignKey("PetId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Pet");
                });

            modelBuilder.Entity("SteelBot.Database.Models.Pets.PetBonus", b =>
                {
                    b.HasOne("SteelBot.Database.Models.Pets.Pet", "Pet")
                        .WithMany("Bonuses")
                        .HasForeignKey("PetId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Pet");
                });

            modelBuilder.Entity("SteelBot.Database.Models.Poll", b =>
                {
                    b.HasOne("SteelBot.Database.Models.User", "PollCreator")
                        .WithMany()
                        .HasForeignKey("UserRowId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("PollCreator");
                });

            modelBuilder.Entity("SteelBot.Database.Models.PollOption", b =>
                {
                    b.HasOne("SteelBot.Database.Models.Poll", "Poll")
                        .WithMany("Options")
                        .HasForeignKey("PollRowId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Poll");
                });

            modelBuilder.Entity("SteelBot.Database.Models.RankRole", b =>
                {
                    b.HasOne("SteelBot.Database.Models.Guild", "Guild")
                        .WithMany("RankRoles")
                        .HasForeignKey("GuildRowId")
                        .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired();

                    b.Navigation("Guild");
                });

            modelBuilder.Entity("SteelBot.Database.Models.SelfRole", b =>
                {
                    b.HasOne("SteelBot.Database.Models.Guild", "Guild")
                        .WithMany("SelfRoles")
                        .HasForeignKey("GuildRowId")
                        .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired();

                    b.Navigation("Guild");
                });

            modelBuilder.Entity("SteelBot.Database.Models.StockPortfolio", b =>
                {
                    b.HasOne("SteelBot.Database.Models.User", "Owner")
                        .WithOne("StockPortfolio")
                        .HasForeignKey("SteelBot.Database.Models.StockPortfolio", "OwnerRowId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Owner");
                });

            modelBuilder.Entity("SteelBot.Database.Models.StockPortfolioSnapshot", b =>
                {
                    b.HasOne("SteelBot.Database.Models.StockPortfolio", "ParentPortfolio")
                        .WithMany("Snapshots")
                        .HasForeignKey("ParentPortfolioRowId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ParentPortfolio");
                });

            modelBuilder.Entity("SteelBot.Database.Models.Trigger", b =>
                {
                    b.HasOne("SteelBot.Database.Models.User", "Creator")
                        .WithMany("CreatedTriggers")
                        .HasForeignKey("CreatorRowId")
                        .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired();

                    b.HasOne("SteelBot.Database.Models.Guild", "Guild")
                        .WithMany("Triggers")
                        .HasForeignKey("GuildRowId")
                        .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired();

                    b.Navigation("Creator");

                    b.Navigation("Guild");
                });

            modelBuilder.Entity("SteelBot.Database.Models.User", b =>
                {
                    b.HasOne("SteelBot.Database.Models.RankRole", "CurrentRankRole")
                        .WithMany("UsersWithRole")
                        .HasForeignKey("CurrentRankRoleRowId")
                        .OnDelete(DeleteBehavior.SetNull);

                    b.HasOne("SteelBot.Database.Models.Guild", "Guild")
                        .WithMany("UsersInGuild")
                        .HasForeignKey("GuildRowId")
                        .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired();

                    b.Navigation("CurrentRankRole");

                    b.Navigation("Guild");
                });

            modelBuilder.Entity("SteelBot.Database.Models.Guild", b =>
                {
                    b.Navigation("RankRoles");

                    b.Navigation("SelfRoles");

                    b.Navigation("Triggers");

                    b.Navigation("UsersInGuild");
                });

            modelBuilder.Entity("SteelBot.Database.Models.Pets.Pet", b =>
                {
                    b.Navigation("Attributes");

                    b.Navigation("Bonuses");
                });

            modelBuilder.Entity("SteelBot.Database.Models.Poll", b =>
                {
                    b.Navigation("Options");
                });

            modelBuilder.Entity("SteelBot.Database.Models.RankRole", b =>
                {
                    b.Navigation("UsersWithRole");
                });

            modelBuilder.Entity("SteelBot.Database.Models.StockPortfolio", b =>
                {
                    b.Navigation("OwnedStock");

                    b.Navigation("Snapshots");
                });

            modelBuilder.Entity("SteelBot.Database.Models.User", b =>
                {
                    b.Navigation("CreatedTriggers");

                    b.Navigation("StockPortfolio");
                });
#pragma warning restore 612, 618
        }
    }
}
