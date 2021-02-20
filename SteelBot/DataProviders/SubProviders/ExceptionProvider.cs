using Microsoft.EntityFrameworkCore;
using SteelBot.Database;
using SteelBot.Database.Models;
using System.Threading.Tasks;

namespace SteelBot.DataProviders.SubProviders
{
    public class ExceptionProvider
    {
        private readonly IDbContextFactory<SteelBotContext> DbContextFactory;

        public ExceptionProvider(IDbContextFactory<SteelBotContext> contextFactory)
        {
            DbContextFactory = contextFactory;
        }

        public async Task InsertException(ExceptionLog ex)
        {
            using (SteelBotContext db = DbContextFactory.CreateDbContext())
            {
                db.LoggedErrors.Add(ex);
                await db.SaveChangesAsync();
            }
        }
    }
}