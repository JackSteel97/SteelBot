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
            public const string HashKeycap = "#\ufe0f\u20e3";
        }

        public static class Objects
        {
            public const string TrashBin = "\uD83D\uDDD1";
            public const string StopSign = "\uD83D\uDED1";
            public const string LightBulb = "\ud83d\udca1";
            public const string Ruler = "\ud83d\udccf";
            public const string Microphone = "\ud83c\udfa4";
            public const string Camera = "\ud83d\udcf7";
            public const string MutedSpeaker = "\ud83d\udd07";
            public const string BellWithSlash = "\ud83d\udd15";
            public const string Television = "\ud83d\udcfa";
        }

        /// <summary>
        /// Discord emoji ids from my dev server.
        /// </summary>
        public static class CustomDiscordEmojis
        {
            public const string LoadingSpinner = "<a:loading:804037223423016971>";
            public const string GreenArrowUp = "<a:green_up_arrow:808065949776740363>";
            public const string RedArrowDown = "<a:red_down_arrow:808065950770266133>";
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