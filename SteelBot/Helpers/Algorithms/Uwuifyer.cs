using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SteelBot.Helpers.Algorithms
{
    public static class Uwuifyer
    {
        private static readonly string[] Faces = new string[]
        { "(・`ω´・)",
            ";;w;;",
            "OwO",
            "UwU",
            ">w<",
            "^w^",
            "ÚwÚ",
            "^-^",
            ":3",
            "x3"
        };

        private static Random Random = new Random();

        public static string Uwuify(string input, bool addFaces = true)
        {
            string output = input;

            output = output.Replace("R", "W");
            output = output.Replace("r", "w");

            output = output.Replace("L", "w");
            output = output.Replace("l", "w");

            output = output.Replace("THE ", "DA ");
            output = output.Replace("the ", "da ");
            output = output.Replace("The ", "Da ");

            output = output.Replace("ove", "uv");
            output = output.Replace("OVE", "UV");

            output = output.Replace("AYS", "EZ");
            output = output.Replace("ays", "ez");

            output = output.Replace("Have ", "Haz ");
            output = output.Replace("have ", "haz ");
            output = output.Replace("HAVE ", "HAZ ");

            if (addFaces)
            {
                output = $"{output} {GetRandomFace()}";
            }
            return output;
        }

        private static string GetRandomFace()
        {
            return Faces[Random.Next(0, Faces.Length)];
        }
    }
}