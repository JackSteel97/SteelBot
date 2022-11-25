using Microsoft.EntityFrameworkCore;
using SteelBot.Database;
using SteelBot.Database.Models;
using System.Threading.Tasks;

namespace SteelBot.DataProviders.SubProviders;

public class ExceptionProvider
{
    private readonly IDbContextFactory<SteelBotContext> _dbContextFactory;

    public ExceptionProvider(IDbContextFactory<SteelBotContext> contextFactory)
    {
        _dbContextFactory = contextFactory;
    }

    public async Task InsertException(ExceptionLog ex)
    {
        await using (var db = _dbContextFactory.CreateDbContext())
        {
            db.LoggedErrors.Add(ex);
            await db.SaveChangesAsync();
        }
    }
}