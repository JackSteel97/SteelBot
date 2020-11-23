using System;

namespace SteelBot.Helpers
{
    public static class EmojiConstants
    {
        public static class Numbers
        {
            public const string NumberZero = "\u0030\u20e3";
            public const string NumberOne = "\u0031\u20e3";
            public const string NumberTwo = "\u0032\u20e3";
            public const string NumberThree = "\u0033\u20e3";
            public const string NumberFour = "\u0034\u20e3";
            public const string NumberFive = "\u0035\u20e3";
            public const string NumberSix = "\u0036\u20e3";
            public const string NumberSeven = "\u0037\u20e3";
            public const string NumberEight = "\u0038\u20e3";
            public const string NumberNine = "\u0039\u20e3";
            public const string NumberTen = "\uD83D\uDD1F";
        }

        public static class Objects
        {
            public const string TrashBin = "\uD83D\uDDD1";
            public const string StopSign = "\uD83D\uDED1";
        }
    }
    public static class EmojiHelper
    {
        public static string GetNumberEmoji(int number)
        {
            switch (number)
            {
                case 0:
                    return EmojiConstants.Numbers.NumberZero;
                case 1:
                    return EmojiConstants.Numbers.NumberOne;
                case 2:
                    return EmojiConstants.Numbers.NumberTwo;
                case 3:
                    return EmojiConstants.Numbers.NumberThree;
                case 4:
                    return EmojiConstants.Numbers.NumberFour;
                case 5:
                    return EmojiConstants.Numbers.NumberFive;
                case 6:
                    return EmojiConstants.Numbers.NumberSix;
                case 7:
                    return EmojiConstants.Numbers.NumberSeven;
                case 8:
                    return EmojiConstants.Numbers.NumberEight;
                case 9:
                    return EmojiConstants.Numbers.NumberNine;
                case 10:
                    return EmojiConstants.Numbers.NumberTen;
                default:
                    throw new ArgumentException("This number does not have an emoji equivalent", nameof(number));
            }
        }
    }
}
