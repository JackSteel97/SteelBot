using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteelBot.Helpers.Maths;

/// <summary>
/// https://stackoverflow.com/a/26312275/4739697
/// </summary>
public static class PermutationsAndCombinations
{
    public static long NCr(int n, int r) =>
        // naive: return Factorial(n) / (Factorial(r) * Factorial(n - r));
        NPr(n, r) / Factorial(r);

    public static long NPr(int n, int r) =>
        // naive: return Factorial(n) / Factorial(n - r);
        FactorialDivision(n, n - r);

    private static long FactorialDivision(int topFactorial, int divisorFactorial)
    {
        long result = 1;
        for (int i = topFactorial; i > divisorFactorial; i--)
            result *= i;
        return result;
    }

    private static long Factorial(int i)
    {
        return i <= 1 ? 1 : i * Factorial(i - 1);
    }
}