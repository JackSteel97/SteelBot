using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Pets.Generation;
internal static class PetGenerationShared
{
    internal static T GetRandomEnumValue<T>(params T[] excluding)
    {
        T result;
        var excludedValues = excluding.ToHashSet();
        do
        {
            var values = Enum.GetValues(typeof(T)).Cast<T>().ToArray();
            result = values[RandomNumberGenerator.GetInt32(values.Length)];
        } while (excludedValues.Contains(result));
        return result;
    }
}
