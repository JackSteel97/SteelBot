using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SteelBot.Database;
using SteelBot.Database.Models.AuditLog;
using System.Threading.Tasks;

namespace SteelBot.DataProviders.SubProviders;

public class AuditLogProvider
{
    private readonly ILogger<AuditLogProvider> _logger;
    private readonly IDbContextFactory<SteelBotContext> _dbContextFactory;

    public AuditLogProvider(ILogger<AuditLogProvider> logger, IDbContextFactory<SteelBotContext> dbContextFactory)
    {
        _logger = logger;
        _dbContextFactory = dbContextFactory;
    }

    public async Task Write(Audit auditLogItem)
    {
        _logger.LogInformation("Writing a new {Action} item to the audit log for {UserId}", auditLogItem.What, auditLogItem.Who);

        int writtenCount;
        await using (var db = await _dbContextFactory.CreateDbContextAsync())
        {
            db.AuditLog.Add(auditLogItem);
            writtenCount = await db.SaveChangesAsync();
        }

        if (writtenCount == 0)
        {
            _logger.LogError("Writing a new {Action} item to the audit log for {UserId} instered no entries", auditLogItem.What, auditLogItem.Who);
        }
    } 
}