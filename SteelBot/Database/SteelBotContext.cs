using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SteelBot.Database.Models;
using SteelBot.Database.Models.Pets;
using SteelBot.Database.Models.Users;
using System;

namespace SteelBot.Database
{
    public class SteelBotContext : DbContext
    {
        public DbSet<Guild> Guilds { get; set; }

        public DbSet<User> Users { get; set; }

        public DbSet<SelfRole> SelfRoles { get; set; }
        public DbSet<RankRole> RankRoles { get; set; }
        public DbSet<Trigger> Triggers { get; set; }
        public DbSet<ExceptionLog> LoggedErrors { get; set; }
        public DbSet<CommandStatistic> CommandStatistics { get; set; }
        public DbSet<Pet> Pets { get; set; }
        public DbSet<PetAttribute> PetAttributes { get; set; }
        public DbSet<PetBonus> PetBonuses { get; set; }
        public DbSet<UserAudit> UserAudits { get; set; }

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
                entity.Ignore(u => u.TimeSpentAfk);
                entity.Ignore(u => u.TimeSpentDisconnected);

                entity.HasMany(u => u.CreatedTriggers).WithOne(t => t.Creator).HasForeignKey(t => t.CreatorRowId).OnDelete(DeleteBehavior.NoAction);
                entity.HasOne(u => u.CurrentRankRole).WithMany(rr => rr.UsersWithRole).HasForeignKey(u => u.CurrentRankRoleRowId).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<UserAudit>(entity =>
            {
                entity.HasKey(u => u.RowId);
                entity.Ignore(u => u.TotalXp);
            });

            modelBuilder.Entity<SelfRole>(entity =>
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

            modelBuilder.Entity<Pet>(entity =>
            {
                entity.HasKey(p => p.RowId);
                entity.HasIndex(p => p.OwnerDiscordId);

                entity.HasMany(p => p.Attributes).WithOne(pa => pa.Pet).HasForeignKey(pa => pa.PetId);
                entity.HasMany(p => p.Bonuses).WithOne(pb => pb.Pet).HasForeignKey(pb => pb.PetId);
            });

            modelBuilder.Entity<PetAttribute>(entity =>
            {
                entity.HasKey(pa => pa.RowId);
            });

            modelBuilder.Entity<PetBonus>(entity =>
            {
                entity.HasKey(pb => pb.RowId);
            });

            ApplyDateTimeConverters(modelBuilder);
        }

        private void ApplyDateTimeConverters(ModelBuilder modelBuilder)
        {
            //Always UTC dates.
            var dateTimeConverter = new ValueConverter<DateTime, DateTime>(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

            var nullableDateTimeConverter = new ValueConverter<DateTime?, DateTime?>(
                v => v.HasValue ? v.Value : v.Value.ToUniversalTime(),
                v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (entityType.IsKeyless)
                {
                    continue;
                }

                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime))
                    {
                        property.SetValueConverter(dateTimeConverter);
                    }
                    else if (property.ClrType == typeof(DateTime?))
                    {
                        property.SetValueConverter(nullableDateTimeConverter);
                    }
                }
            }
        }
    }
}