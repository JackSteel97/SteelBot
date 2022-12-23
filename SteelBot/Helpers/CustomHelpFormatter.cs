using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.CommandsNext.Entities;
using DSharpPlus.Entities;
using Humanizer;
using Microsoft.Extensions.Logging;
using SteelBot.DiscordModules;
using SteelBot.Services.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace SteelBot.Helpers;

public class CustomHelpFormatter : BaseHelpFormatter
{
    private readonly string _botPrefix;
    private readonly StringBuilder _content;
    private readonly DataHelpers _dataHelpers;
    private readonly DiscordEmbedBuilder _embed;
    private readonly ILogger<CustomHelpFormatter> _logger;
    private bool _hasSubCommands;

    public CustomHelpFormatter(CommandContext ctx, ILogger<CustomHelpFormatter> logger, DataHelpers dataHelpers, AppConfigurationService appConfigurationService) : base(ctx)
    {
        _logger = logger;
        _embed = new DiscordEmbedBuilder();
        _content = new StringBuilder();
        _dataHelpers = dataHelpers;
        _botPrefix = ctx.Guild != null ? _dataHelpers.Config.GetPrefix(ctx.Guild.Id) : appConfigurationService.Application.DefaultCommandPrefix;
        _hasSubCommands = false;

        _logger.LogInformation($"Executing Help command [{ctx.Message.Content}]");
    }

    public override BaseHelpFormatter WithCommand(Command command)
    {
        _content.Append("**Command**: ");
        _content.AppendLine(Formatter.InlineCode(command.QualifiedName.Transform(To.TitleCase)));
        _content.AppendLine(command.Description);

        if (command.Aliases.Count > 0)
        {
            var aliasesBuilder = new StringBuilder();
            for (int i = 0; i < command.Aliases.Count; i++)
            {
                aliasesBuilder.Append(Formatter.InlineCode(command.Aliases[i]));
                if (i != command.Aliases.Count - 1) aliasesBuilder.Append(" | ");
            }

            _embed.AddField("Aliases", aliasesBuilder.ToString());
        }

        var usageBuilder = new StringBuilder();
        if (command is CommandGroup)
        {
            usageBuilder.AppendLine($"`{_botPrefix}{command.Name.Humanize().Transform(To.TitleCase)} <Sub-Command>`");
            _hasSubCommands = true;
        }

        foreach (var overload in command.Overloads)
            if (overload.Arguments.Count > 0)
            {
                usageBuilder.Append($"`{_botPrefix}{command.QualifiedName.Transform(To.TitleCase)} ");
                foreach (var argument in overload.Arguments)
                {
                    string argumentStarter = "<";
                    string argumentEnder = ">";
                    if (argument.IsOptional)
                    {
                        argumentStarter = "[";
                        argumentEnder = "]";
                    }

                    if (argument.Type == typeof(string) && !argument.IsCatchAll)
                    {
                        // Wrap with quotes.
                        argumentEnder += '"';
                        argumentStarter = $"\"{argumentStarter}";
                    }

                    usageBuilder.Append($"{argumentStarter}{argument.Name.Humanize().Transform(To.TitleCase)}{argumentEnder} ");
                }

                usageBuilder.Append("`\n");
            }

        if (usageBuilder.Length > 0) _embed.AddField("Usage", usageBuilder.ToString());

        return this;
    }

    public override BaseHelpFormatter WithSubcommands(IEnumerable<Command> cmds)
    {
        var childCommands = new StringBuilder();
        foreach (var cmd in cmds)
        {
            if (cmd.Name.Equals("help", StringComparison.OrdinalIgnoreCase)) continue;

            if (cmd is CommandGroup cmdGroup)
            {
                _embed.AddField(Formatter.InlineCode(cmdGroup.Name.Transform(To.TitleCase)), cmdGroup.Description);
                _hasSubCommands = true;
            }
            else if (cmd is not null && cmd.Parent != null)
            {
                childCommands.AppendLine(Formatter.InlineCode(cmd.Name.Transform(To.TitleCase)));
            }
        }

        if (childCommands.Length > 0) _embed.AddField("Sub-Commands", childCommands.ToString());

        return this;
    }

    public override CommandHelpMessage Build()
    {
        _embed.WithColor(EmbedGenerator.InfoColour)
            .WithAuthor(Context.Client.CurrentUser.Username, iconUrl: Context.Client.CurrentUser.AvatarUrl)
            .WithTitle("Help")
            .WithDescription(_content.ToString());

        if (_hasSubCommands) _embed.WithFooter("Specify a command for more information.");

        return new CommandHelpMessage(embed: _embed.Build());
    }
}