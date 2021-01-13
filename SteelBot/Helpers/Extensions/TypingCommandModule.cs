using DSharpPlus.CommandsNext;
using System.Threading.Tasks;

namespace SteelBot.Helpers.Extensions
{
    public class TypingCommandModule : BaseCommandModule
    {
        public override Task BeforeExecutionAsync(CommandContext ctx)
        {
            ctx.TriggerTypingAsync();
            return Task.CompletedTask;
        }
    }
}