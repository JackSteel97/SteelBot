using Microsoft.EntityFrameworkCore;
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

        public DbSet<ExceptionLog> LoggedErrors { get; set; }

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
                entity.HasMany(g => g.UsersInGuild).WithOne(u => u.Guild).HasForeignKey(u => u.GuildRowId);
                entity.HasMany(g => g.SelfRoles).WithOne(sr => sr.Guild).HasForeignKey(sr => sr.GuildRowId);
                entity.HasMany(g => g.RankRoles).WithOne(rr => rr.Guild).HasForeignKey(rr => rr.GuildRowId);
                entity.HasMany(g => g.Triggers).WithOne(t => t.Guild).HasForeignKey(t => t.GuildRowId);
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.RowId);
                entity.Ignore(u => u.TotalXp);
                entity.Ignore(u => u.TimeSpentInVoice);
                entity.Ignore(u => u.TimeSpentDeafened);
                entity.Ignore(u => u.TimeSpentMuted);
                entity.Ignore(u => u.TimeSpentStreaming);

                entity.HasMany(u => u.CreatedTriggers).WithOne(t => t.Creator).HasForeignKey(t => t.CreatorRowId).OnDelete(DeleteBehavior.NoAction);
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
        }
    }
}