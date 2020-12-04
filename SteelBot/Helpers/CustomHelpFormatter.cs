using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.CommandsNext.Entities;
using DSharpPlus.Entities;
using Humanizer;
using Microsoft.Extensions.Logging;
using SteelBot.Attributes;
using SteelBot.DiscordModules;
using SteelBot.Services.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteelBot.Helpers
{
    public class CustomHelpFormatter : BaseHelpFormatter
    {
        private readonly DiscordEmbedBuilder Embed;
        private readonly StringBuilder Content;
        private readonly ILogger<CustomHelpFormatter> Logger;
        private readonly DataHelpers DataHelpers;
        private readonly string BotPrefix;
        private bool HasSubCommands;

        public CustomHelpFormatter(CommandContext ctx, ILogger<CustomHelpFormatter> logger, DataHelpers dataHelpers, AppConfigurationService appConfigurationService) : base(ctx)
        {
            Logger = logger;
            Embed = new DiscordEmbedBuilder();
            Content = new StringBuilder();
            DataHelpers = dataHelpers;
            if (ctx.Guild != null)
            {
                BotPrefix = DataHelpers.Config.GetPrefix(ctx.Guild.Id);
            }
            else
            {
                BotPrefix = appConfigurationService.Application.DefaultCommandPrefix;
            }
            HasSubCommands = false;

            Logger.LogInformation($"Executing Help command [{ctx.Message.Content}]");
        }

        public override BaseHelpFormatter WithCommand(Command command)
        {
            Content.Append("**Command**: ");
            Content.AppendLine(Formatter.InlineCode(command.QualifiedName.Transform(To.TitleCase)));
            Content.AppendLine(command.Description);

            if (command.Aliases.Count > 0)
            {
                StringBuilder aliasesBuilder = new StringBuilder();
                for (int i = 0; i < command.Aliases.Count; i++)
                {
                    aliasesBuilder.Append(Formatter.InlineCode(command.Aliases[i]));
                    if (i != command.Aliases.Count - 1)
                    {
                        aliasesBuilder.Append(" | ");
                    }
                }
                Embed.AddField("Aliases", aliasesBuilder.ToString());
            }

            StringBuilder usageBuilder = new StringBuilder();
            if (command is CommandGroup)
            {
                usageBuilder.AppendLine($"`{BotPrefix}{command.Name} <Sub-Command>`");
                HasSubCommands = true;
            }

            foreach (var overload in command.Overloads)
            {
                if (overload.Arguments.Count > 0)
                {
                    usageBuilder.Append($"`{BotPrefix}{command.QualifiedName.Transform(To.TitleCase)} ");
                    foreach (var argument in overload.Arguments)
                    {
                        string argumentStarter = "<";
                        string argumentEnder = ">";
                        if (argument.IsOptional)
                        {
                            argumentStarter = "[";
                            argumentEnder = "]";
                        }
                        if (argument.Type == typeof(string))
                        {
                            // Wrap with quotes.
                            argumentEnder += '"';
                            argumentStarter = $"\"{argumentStarter}";
                        }
                        usageBuilder.Append($"{argumentStarter}{argument.Name.Humanize().Transform(To.TitleCase)}{argumentEnder} ");
                    }
                    usageBuilder.Append("`\n");
                }
            }

            if (usageBuilder.Length > 0)
            {
                Embed.AddField("Usage", usageBuilder.ToString());
            }

            return this;
        }

        public override BaseHelpFormatter WithSubcommands(IEnumerable<Command> cmds)
        {
            StringBuilder childCommands = new StringBuilder();
            foreach (var cmd in cmds)
            {
                if (cmd.Name.Equals("help", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (cmd is CommandGroup cmdGroup)
                {
                    Embed.AddField(Formatter.InlineCode(cmdGroup.Name.Transform(To.TitleCase)), cmdGroup.Description);
                    HasSubCommands = true;
                }
                else if (cmd is Command)
                {
                    childCommands.AppendLine(Formatter.InlineCode(cmd.Name.Transform(To.TitleCase)));
                }
            }

            if (childCommands.Length > 0)
            {
                Embed.AddField("Sub-Commands", childCommands.ToString());
            }

            return this;
        }

        public override CommandHelpMessage Build()
        {
            Embed.WithColor(EmbedGenerator.InfoColour)
                .WithAuthor(name: Context.Client.CurrentUser.Username, iconUrl: Context.Client.CurrentUser.AvatarUrl)
                .WithTitle("Help")
                .WithDescription(Content.ToString());

            if (HasSubCommands)
            {
                Embed.WithFooter("Specify a command for more information.");
            }

            return new CommandHelpMessage(embed: Embed.Build());
        }
    }
}