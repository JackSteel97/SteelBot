using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zalgo;

namespace SteelBot.Helpers.Extensions;
public static class StringExtensions
{
    public static string ToZalgo(this string input, bool zalgoify = true) => zalgoify ? new ZalgoString(input).ToString() : input;
}