using System;

namespace SteelBot.Helpers;

// From https://r12a.github.io/app-conversion/ Use "\n etc" checkbox on JS/Java/C style
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
        public const string WavingHand = "\uD83D\uDC4B";
        public const string ThumbsUp = "\uD83D\uDC4D";
        public const string ThumbsDown = "\uD83D\uDC4E";
        public const string WritingHand = "\u270D\uFE0F";
        public const string Herb = "\uD83C\uDF3F";
    }

    public static class Symbols
    {
        public const string Zzz = "\uD83D\uDCA4";
        public const string CheckMark = "\u2714\uFE0F";
        public const string CheckMarkButton = "\u2705";
        public const string CrossMark = "\u274C";
        public const string GreenSquare = "\uD83D\uDFE9";
        public const string GreySquare = "\u2B1B";
        public const string YellowSquare = "\uD83D\uDFE8";
        public const string GlowingStar = "\uD83C\uDF1F";
        public const string UpButton = "\uD83D\uDD3C";
        public const string DownButton = "\uD83D\uDD3D";
        public const string BackwardArrow = "\u25C0\uFE0F";
        public const string ForwardArrow = "\u25B6\uFE0F";
        public const string StopButton = "\u23F9\uFE0F";
        public const string LeftRightArrow = "\u2194\uFE0F";
    }

    public static class Faces
    {
        public const string Hugging = "\uD83E\uDD17";
        public const string Pleading = "\uD83E\uDD7A";
        public const string Flushed = "\uD83D\uDE33";
    }

    public static class RegionalIndicators
    {
        public const string A = ":regional_indicator_a:";
        public const string B = ":regional_indicator_b:";
        public const string C = ":regional_indicator_c:";
        public const string D = ":regional_indicator_d:";
        public const string E = ":regional_indicator_e:";
        public const string F = ":regional_indicator_f:";
        public const string G = ":regional_indicator_g:";
        public const string H = ":regional_indicator_h:";
        public const string I = ":regional_indicator_i:";
        public const string J = ":regional_indicator_j:";
        public const string K = ":regional_indicator_k:";
        public const string L = ":regional_indicator_l:";
        public const string M = ":regional_indicator_m:";
        public const string N = ":regional_indicator_n:";
        public const string O = ":regional_indicator_o:";
        public const string P = ":regional_indicator_p:";
        public const string Q = ":regional_indicator_q:";
        public const string R = ":regional_indicator_r:";
        public const string S = ":regional_indicator_s:";
        public const string T = ":regional_indicator_t:";
        public const string U = ":regional_indicator_u:";
        public const string V = ":regional_indicator_v:";
        public const string W = ":regional_indicator_w:";
        public const string X = ":regional_indicator_x:";
        public const string Y = ":regional_indicator_y:";
        public const string Z = ":regional_indicator_z:";
    }

    /// <summary>
    ///     Discord emoji ids from my dev server.
    /// </summary>
    public static class CustomDiscordEmojis
    {
        public const string LoadingSpinner = "<a:loading:804037223423016971>";
        public const string GreenArrowUp = "<a:green_up_arrow:808065949776740363>";
        public const string RedArrowDown = "<a:red_down_arrow:808065950770266133>";

        public static class YellowRegionalIndicators
        {
            public const string A = "<:a_yellow:939205545204801606>";
            public const string B = "<:b_yellow:939205545137676369>";
            public const string C = "<:c_yellow:939205545116729414>";
            public const string D = "<:d_yellow:939205545158668358>";
            public const string E = "<:e_yellow:939205545087361094>";
            public const string F = "<:f_yellow:939205545125085195>";
            public const string G = "<:g_yellow:939205545158651904>";
            public const string H = "<:h_yellow:939205545133486111>";
            public const string I = "<:i_yellow:939205545284468806>";
            public const string J = "<:j_yellow:939205545284489266>";
            public const string K = "<:k_yellow:939205545494212638>";
            public const string L = "<:l_yellow:939205545003458642>";
            public const string M = "<:m_yellow:939205544953135185>";
            public const string N = "<:n_yellow:939205545200590928>";
            public const string O = "<:o_yellow:939205545821339718>";
            public const string P = "<:p_yellow:939205545188003850>";
            public const string Q = "<:q_yellow:939205545192206396>";
            public const string R = "<:r_yellow:939205544873426995>";
            public const string S = "<:s_yellow:939205545087352903>";
            public const string T = "<:t_yellow:939205545204797460>";
            public const string U = "<:u_yellow:939205545213169774>";
            public const string V = "<:v_yellow:939205545255137350>";
            public const string W = "<:w_yellow:939205545334825081>";
            public const string X = "<:x_yellow:939205544948949055>";
            public const string Y = "<:y_yellow:939205544932167771>";
            public const string Z = "<:z_yellow:939205545586487316>";
        }

        public static class GreenRegionalIndicators
        {
            public const string A = "<:a_green:939205634316992664>";
            public const string B = "<:b_green:939205634774151219>";
            public const string C = "<:c_green:939205634413457459>";
            public const string D = "<:d_green:939205634577010749>";
            public const string E = "<:e_green:939205634677669938>";
            public const string F = "<:f_green:939205634711240775>";
            public const string G = "<:g_green:939205634702860400>";
            public const string H = "<:h_green:939205634644135977>";
            public const string I = "<:i_green:939205634509926401>";
            public const string J = "<:j_green:939205634631532634>";
            public const string K = "<:k_green:939205634639929455>";
            public const string L = "<:l_green:939205634732228618>";
            public const string M = "<:m_green:939205634480558123>";
            public const string N = "<:n_green:939205634660900924>";
            public const string O = "<:o_green:939205634665086986>";
            public const string P = "<:p_green:939205635084525649>";
            public const string Q = "<:q_green:939205634753187880>";
            public const string R = "<:r_green:939205634690265169>";
            public const string S = "<:s_green:939205634430214216>";
            public const string T = "<:t_green:939205634702848060>";
            public const string U = "<:u_green:939205634585411685>";
            public const string V = "<:v_green:939205634778365982>";
            public const string W = "<:w_green:939205634623160381>";
            public const string X = "<:x_green:939205634824478801>";
            public const string Y = "<:y_green:939205634933555210>";
            public const string Z = "<:z_green:939205634790916166>";
        }
    }
}

public static class EmojiHelper
{
    public static string GetNumberEmoji(int number) =>
        number switch
        {
            0 => EmojiConstants.Numbers.NumberZero,
            1 => EmojiConstants.Numbers.NumberOne,
            2 => EmojiConstants.Numbers.NumberTwo,
            3 => EmojiConstants.Numbers.NumberThree,
            4 => EmojiConstants.Numbers.NumberFour,
            5 => EmojiConstants.Numbers.NumberFive,
            6 => EmojiConstants.Numbers.NumberSix,
            7 => EmojiConstants.Numbers.NumberSeven,
            8 => EmojiConstants.Numbers.NumberEight,
            9 => EmojiConstants.Numbers.NumberNine,
            10 => EmojiConstants.Numbers.NumberTen,
            _ => throw new ArgumentException("This number does not have an emoji equivalent", nameof(number))
        };
}