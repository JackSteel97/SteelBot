using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sentry;
using SteelBot.Database;
using SteelBot.Database.Models.Puzzle;
using SteelBot.Helpers.Sentry;
using System.Linq;
using System.Threading.Tasks;

namespace SteelBot.DataProviders.SubProviders;

public class PuzzleProvider
{
    private readonly ILogger<PuzzleProvider> _logger;
    private readonly IDbContextFactory<SteelBotContext> _dbContextFactory;
    private readonly IHub _sentry;

    public PuzzleProvider(ILogger<PuzzleProvider> logger, IDbContextFactory<SteelBotContext> dbContextFactory, IHub sentry)
    {
        _logger = logger;
        _dbContextFactory = dbContextFactory;
        _sentry = sentry;
    }

    public async Task<int> GetUserPuzzleLevel(ulong userId)
    {
        var transaction = _sentry.StartSpanOnCurrentTransaction(nameof(GetUserPuzzleLevel));

        await using (var db = await _dbContextFactory.CreateDbContextAsync())
        {
           var progress = await db.PuzzleProgress.AsNoTracking().FirstOrDefaultAsync(x=>x.UserId == userId);
           if (progress != default)
           {
               return progress.CurrentLevel;
           }
        }
        return 1;
    }

    public async Task SetUserPuzzleLevel(ulong userId, int newLevel)
    {
        var transaction = _sentry.StartSpanOnCurrentTransaction(nameof(SetUserPuzzleLevel));
        await using (var db = await _dbContextFactory.CreateDbContextAsync())
        {
            var progress = await db.PuzzleProgress.FirstOrDefaultAsync(x => x.UserId == userId);
            if (progress != default)
            {
                progress.CurrentLevel = newLevel;
            }
            else
            {
                db.PuzzleProgress.Add(new Progress { UserId = userId, CurrentLevel = newLevel });
            }

            await db.SaveChangesAsync();
        }
    }

    public async Task RecordGuess(ulong userId, int puzzleLevel, string guess)
    {
        var transaction = _sentry.StartSpanOnCurrentTransaction(nameof(RecordGuess));

        await using (var db = await _dbContextFactory.CreateDbContextAsync())
        {
            var guessRecord = new Guess() { UserId = userId, PuzzleLevel = puzzleLevel, GuessContent = guess };
            db.Guesses.Add(guessRecord);
            await db.SaveChangesAsync();
        }

        transaction.Finish();
    }
}