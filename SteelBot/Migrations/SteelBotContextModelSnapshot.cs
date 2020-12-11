﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SteelBot.Database;

namespace SteelBot.Migrations
{
    [DbContext(typeof(SteelBotContext))]
    partial class SteelBotContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .UseIdentityColumns()
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("ProductVersion", "5.0.1");

            modelBuilder.Entity("SteelBot.Database.Models.ExceptionLog", b =>
                {
                    b.Property<long>("RowId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .UseIdentityColumn();

                    b.Property<string>("FullDetail")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Message")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("SourceMethod")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("StackTrace")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("Timestamp")
                        .HasColumnType("datetime2");

                    b.HasKey("RowId");

                    b.ToTable("LoggedErrors");
                });

            modelBuilder.Entity("SteelBot.Database.Models.Guild", b =>
                {
                    b.Property<long>("RowId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .UseIdentityColumn();

                    b.Property<DateTime>("BotAddedTo")
                        .HasColumnType("datetime2");

                    b.Property<string>("CommandPrefix")
                        .ValueGeneratedOnAdd()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)")
                        .HasDefaultValue("+");

                    b.Property<decimal>("DiscordId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<decimal?>("LevelAnnouncementChannelId")
                        .HasColumnType("decimal(20,0)");

                    b.HasKey("RowId");

                    b.HasIndex("DiscordId")
                        .IsUnique();

                    b.ToTable("Guilds");
                });

            modelBuilder.Entity("SteelBot.Database.Models.Poll", b =>
                {
                    b.Property<long>("RowId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .UseIdentityColumn();

                    b.Property<decimal>("ChannelId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsLockedPoll")
                        .HasColumnType("bit");

                    b.Property<decimal>("MessageId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<string>("Title")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

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
                        .HasColumnType("bigint")
                        .UseIdentityColumn();

                    b.Property<int>("OptionNumber")
                        .HasColumnType("int");

                    b.Property<string>("OptionText")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

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
                        .HasColumnType("bigint")
                        .UseIdentityColumn();

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<long>("GuildRowId")
                        .HasColumnType("bigint");

                    b.Property<int>("LevelRequired")
                        .HasColumnType("int");

                    b.Property<string>("RoleName")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.HasKey("RowId");

                    b.HasIndex("GuildRowId");

                    b.ToTable("RankRoles");
                });

            modelBuilder.Entity("SteelBot.Database.Models.SelfRole", b =>
                {
                    b.Property<long>("RowId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .UseIdentityColumn();

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Description")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<long>("GuildRowId")
                        .HasColumnType("bigint");

                    b.Property<bool>("Hidden")
                        .HasColumnType("bit");

                    b.Property<string>("RoleName")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.HasKey("RowId");

                    b.HasIndex("GuildRowId");

                    b.ToTable("SelfRoles");
                });

            modelBuilder.Entity("SteelBot.Database.Models.User", b =>
                {
                    b.Property<long>("RowId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .UseIdentityColumn();

                    b.Property<decimal>("ActivityXpEarned")
                        .HasColumnType("decimal(20,0)");

                    b.Property<int>("CurrentLevel")
                        .HasColumnType("int");

                    b.Property<DateTime?>("DeafenedStartTime")
                        .HasColumnType("datetime2");

                    b.Property<decimal>("DiscordId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<long>("GuildRowId")
                        .HasColumnType("bigint");

                    b.Property<DateTime>("LastActivity")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("LastMessageSent")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("LastXpEarningMessage")
                        .HasColumnType("datetime2");

                    b.Property<long>("MessageCount")
                        .HasColumnType("bigint");

                    b.Property<decimal>("MessageXpEarned")
                        .HasColumnType("decimal(20,0)");

                    b.Property<DateTime?>("MutedStartTime")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("StreamingStartTime")
                        .HasColumnType("datetime2");

                    b.Property<decimal>("TimeSpentDeafenedSeconds")
                        .HasColumnType("decimal(20,0)");

                    b.Property<decimal>("TimeSpentInVoiceSeconds")
                        .HasColumnType("decimal(20,0)");

                    b.Property<decimal>("TimeSpentMutedSeconds")
                        .HasColumnType("decimal(20,0)");

                    b.Property<decimal>("TimeSpentStreamingSeconds")
                        .HasColumnType("decimal(20,0)");

                    b.Property<decimal>("TotalMessageLength")
                        .HasColumnType("decimal(20,0)");

                    b.Property<DateTime>("UserFirstSeen")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("VoiceStartTime")
                        .HasColumnType("datetime2");

                    b.HasKey("RowId");

                    b.HasIndex("GuildRowId");

                    b.ToTable("Users");
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
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Guild");
                });

            modelBuilder.Entity("SteelBot.Database.Models.SelfRole", b =>
                {
                    b.HasOne("SteelBot.Database.Models.Guild", "Guild")
                        .WithMany("SelfRoles")
                        .HasForeignKey("GuildRowId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Guild");
                });

            modelBuilder.Entity("SteelBot.Database.Models.User", b =>
                {
                    b.HasOne("SteelBot.Database.Models.Guild", "Guild")
                        .WithMany("UsersInGuild")
                        .HasForeignKey("GuildRowId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Guild");
                });

            modelBuilder.Entity("SteelBot.Database.Models.Guild", b =>
                {
                    b.Navigation("RankRoles");

                    b.Navigation("SelfRoles");

                    b.Navigation("UsersInGuild");
                });

            modelBuilder.Entity("SteelBot.Database.Models.Poll", b =>
                {
                    b.Navigation("Options");
                });
#pragma warning restore 612, 618
        }
    }
}
