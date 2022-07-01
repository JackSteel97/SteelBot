using Microsoft.EntityFrameworkCore;
using Sentry;
using SteelBot.Database;
using SteelBot.Database.Models;
using SteelBot.Helpers.Sentry;
using System.Threading.Tasks;

namespace SteelBot.DataProviders.SubProviders
{
    public class ExceptionProvider
    {
        private readonly IDbContextFactory<SteelBotContext> DbContextFactory;
        private readonly IHub _sentry;

        public ExceptionProvider(IDbContextFactory<SteelBotContext> contextFactory, IHub sentry)
        {
            DbContextFactory = contextFactory;
            _sentry = sentry;
        }

        public async Task InsertException(ExceptionLog ex)
        {
            var transaction = _sentry.StartSpanOnCurrentTransaction(nameof(InsertException));
            await using (SteelBotContext db = DbContextFactory.CreateDbContext())
            {
                db.LoggedErrors.Add(ex);
                await db.SaveChangesAsync();
            }

            transaction.Finish();
        }
    }
}