﻿using Microsoft.EntityFrameworkCore;
using SteelBot.Database.Models;

namespace SteelBot.Database
{
    public class SteelBotContext : DbContext
    {
        public DbSet<Guild> Guilds { get; set; }

        public DbSet<User> Users { get; set; }

        public DbSet<SelfRole> SelfRoles { get; set; }
        public DbSet<RankRole> RankRoles { get; set; }

        public DbSet<Poll> Polls { get; set; }

        public DbSet<PollOption> PollOptions { get; set; }
        public DbSet<Trigger> Triggers { get; set; }
        public DbSet<StockPortfolio> StockPortfolios { get; set; }
        public DbSet<OwnedStock> OwnedStocks { get; set; }
        public DbSet<StockPortfolioSnapshot> StockPortfolioSnapshots { get; set; }

        public DbSet<ExceptionLog> LoggedErrors { get; set; }
        public DbSet<CommandStatistic> CommandStatistics { get; set; }

        public SteelBotContext(DbContextOptions<SteelBotContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Guild>(entity =>
            {
                entity.HasKey(g => g.RowId);
                entity.HasIndex(g => g.DiscordId).IsUnique();
                entity.Property(g => g.CommandPrefix).HasDefaultValue("+");
                entity.HasMany(g => g.UsersInGuild).WithOne(u => u.Guild).HasForeignKey(u => u.GuildRowId).OnDelete(DeleteBehavior.NoAction);
                entity.HasMany(g => g.SelfRoles).WithOne(sr => sr.Guild).HasForeignKey(sr => sr.GuildRowId).OnDelete(DeleteBehavior.NoAction);
                entity.HasMany(g => g.RankRoles).WithOne(rr => rr.Guild).HasForeignKey(rr => rr.GuildRowId).OnDelete(DeleteBehavior.NoAction);
                entity.HasMany(g => g.Triggers).WithOne(t => t.Guild).HasForeignKey(t => t.GuildRowId).OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.RowId);
                entity.Ignore(u => u.TotalXp);
                entity.Ignore(u => u.TimeSpentInVoice);
                entity.Ignore(u => u.TimeSpentDeafened);
                entity.Ignore(u => u.TimeSpentMuted);
                entity.Ignore(u => u.TimeSpentStreaming);
                entity.Ignore(u => u.TimeSpentOnVideo);

                entity.HasMany(u => u.CreatedTriggers).WithOne(t => t.Creator).HasForeignKey(t => t.CreatorRowId).OnDelete(DeleteBehavior.NoAction);
                entity.HasOne(u => u.CurrentRankRole).WithMany(rr => rr.UsersWithRole).HasForeignKey(u => u.CurrentRankRoleRowId).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(u => u.StockPortfolio).WithOne(pf => pf.Owner).HasForeignKey<StockPortfolio>(pf => pf.OwnerRowId);
            });

            modelBuilder.Entity<SelfRole>(entity =>
            {
                entity.HasKey(sr => sr.RowId);
            });

            modelBuilder.Entity<Poll>(entity =>
            {
                entity.HasKey(sr => sr.RowId);
                entity.HasMany(p => p.Options).WithOne(po => po.Poll).HasForeignKey(po => po.PollRowId);
            });

            modelBuilder.Entity<PollOption>(entity =>
            {
                entity.HasKey(sr => sr.RowId);
            });

            modelBuilder.Entity<ExceptionLog>(entity =>
            {
                entity.HasKey(e => e.RowId);
            });

            modelBuilder.Entity<RankRole>(entity =>
            {
                entity.HasKey(rr => rr.RowId);
            });

            modelBuilder.Entity<Trigger>(entity =>
            {
                entity.HasKey(rr => rr.RowId);
            });

            modelBuilder.Entity<CommandStatistic>(entity =>
            {
                entity.HasKey(cs => cs.RowId);
                entity.HasIndex(cs => cs.CommandName).IsUnique();
            });

            modelBuilder.Entity<StockPortfolio>(entity =>
            {
                entity.HasKey(pf => pf.RowId);
                entity.Ignore(pf => pf.OwnedStockBySymbol);

                entity.HasMany(pf => pf.OwnedStock).WithOne(os => os.ParentPortfolio).HasForeignKey(os => os.ParentPortfolioRowId);
                entity.HasMany(pf => pf.Snapshots).WithOne(ss => ss.ParentPortfolio).HasForeignKey(ss => ss.ParentPortfolioRowId);
            });

            modelBuilder.Entity<OwnedStock>(entity =>
            {
                entity.HasKey(os => os.RowId);
                entity.Property(os => os.Symbol).HasMaxLength(100);
            });

            modelBuilder.Entity<StockPortfolioSnapshot>(entity =>
            {
                entity.HasKey(os => os.RowId);
            });
        }
    }
}