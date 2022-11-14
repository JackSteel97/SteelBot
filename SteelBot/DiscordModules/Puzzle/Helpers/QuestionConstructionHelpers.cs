using DSharpPlus.Entities;
using SteelBot.Services.Configuration;
using System.Collections.Generic;
using System.IO;

namespace SteelBot.DiscordModules.Puzzle.Helpers;

public static class QuestionConstructionHelpers
{
    public static FileStream AddFile(DiscordMessageBuilder message, string fileName)
    {
        string basePath = Directory.GetCurrentDirectory();
        var fs = new FileStream(Path.Combine(basePath, "Resources", "Puzzle", fileName), FileMode.Open, FileAccess.Read);
        message.WithFile(fileName, fs);
        return fs;
    }

    public static DiscordMessageBuilder AddFiles(DiscordMessageBuilder message, params string[] fileNames)
    {
        string basePath = Directory.GetCurrentDirectory();
        var streams = new Dictionary<string, Stream>();
        foreach (var fileName in fileNames)
        {
            var stream = File.OpenRead(Path.Combine(basePath, "Resources", "Puzzle", fileName));
            streams.Add(fileName, stream);
        }

        message.WithFiles(streams);
        return message;
    }
}