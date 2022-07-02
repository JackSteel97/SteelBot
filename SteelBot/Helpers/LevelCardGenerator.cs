using DSharpPlus.Entities;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SteelBot.Database.Models.Users;
using SteelBot.Helpers.Extensions;
using SteelBot.Helpers.Levelling;
using SteelBot.Services.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace SteelBot.Helpers;

public class LevelCardGenerator
{
    private const int _width = 800;
    private const int _height = 200;

    private const int _xPadding = 10;
    private const int _yPadding = 10;
    private const int _xpBarHeight = 35;

    private const int _y1 = _height - _yPadding - _xpBarHeight;

    private static readonly int _avatarHeight = _height - (_yPadding * 2);
    private static readonly int _xpBarWidth = _width - (_xPadding * 2) - _avatarHeight;
    private static readonly Rgba32 _backgroundColour = Color.ParseHex("#2F3136");
    private static readonly Color _xpBarBaseColour = Color.FromRgb(102, 102, 102);

    private static readonly int _x1 = _xPadding + _avatarHeight;

    private static readonly ColorStop[] _xpGradient = new ColorStop[] { new ColorStop(0, Color.FromRgb(57, 161, 255)), new ColorStop(1, Color.FromRgb(0, 71, 191)) };
    private static readonly LinearGradientBrush _gradientBrush = new LinearGradientBrush(new PointF(0, 0), new PointF(0, _xpBarHeight), GradientRepetitionMode.Repeat, _xpGradient);

    private static readonly FontCollection _fonts = new FontCollection();
    private const string _fontName = "Roboto";

    public LevelCardGenerator(AppConfigurationService appConfigurationService)
    {
        _fonts.Install(Path.Combine(appConfigurationService.BasePath, "Resources", "Fonts", "Roboto-Regular.ttf"));
    }

    public static async Task<MemoryStream> GenerateCard(User user, DiscordMember member)
    {
        ulong xpForNextLevel = LevellingMaths.XpForLevel(user.CurrentLevel + 1);
        ulong xpForThisLevel = LevellingMaths.XpForLevel(user.CurrentLevel);

        ulong xpIntoThisLevel = 0;
        if (user.TotalXp > xpForThisLevel)
        {
            xpIntoThisLevel = user.TotalXp - xpForThisLevel;
        }
        ulong xpToAchieveNextLevel = xpForNextLevel - xpForThisLevel;

        double progressToNextLevel = ((double)xpIntoThisLevel / (double)xpToAchieveNextLevel * _xpBarWidth) + _xPadding + _avatarHeight;

        using (var avatar = await GetAvatar(member.GetAvatarUrl(DSharpPlus.ImageFormat.Auto, 256)))
        using (var image = new Image<Rgba32>(_width, _height))
        {
            image.Mutate(imageContext =>
            {
                imageContext.BackgroundColor(_backgroundColour);

                // XP Bar.
                int progressWidth = (int)Math.Round(progressToNextLevel - _x1);

                // Draw base empty bar.
                imageContext.DrawRoundedRectangle(_xpBarWidth, _xpBarHeight, _x1, _y1, _xpBarHeight / 2, new SolidBrush(_xpBarBaseColour));

                if (progressWidth > 0)
                {
                    // Draw current xp bar on top.
                    imageContext.DrawRoundedRectangle(progressWidth, _xpBarHeight, _x1, _y1, _xpBarHeight / 2, _gradientBrush);
                }

                // Avatar Image.
                using (var avatarRound = avatar.Clone(x => x.ConvertToAvatar(new Size(_avatarHeight, _avatarHeight), _avatarHeight / 2)))
                {
                    imageContext.DrawImage(avatarRound, new Point(_xPadding, _yPadding), 1);
                }

                float xpBarMidpointX = _x1 + (_xpBarWidth / 2),
                xpBarMidpointY = _y1 + (_xpBarHeight / 2);
                // Current XP text.
                imageContext.DrawSimpleText(GetOptions(HorizontalAlignment.Right, VerticalAlignment.Center),
                    user.TotalXp.KiloFormat(),
                    _fonts.CreateFont(_fontName, _xpBarHeight - 12),
                    Color.WhiteSmoke,
                    xpBarMidpointX,
                    xpBarMidpointY);

                // Remaining Xp text.
                imageContext.DrawSimpleText(GetOptions(HorizontalAlignment.Left, VerticalAlignment.Center),
                    $" / {xpForNextLevel.KiloFormat()}",
                    _fonts.CreateFont(_fontName, _xpBarHeight - 12),
                    Color.LightGray,
                    xpBarMidpointX,
                    xpBarMidpointY);

                // Current Level text.
                var levelTextOpts = GetOptions(HorizontalAlignment.Right, VerticalAlignment.Top);
                string levelText = $"Level {user.CurrentLevel}";
                var levelFont = _fonts.CreateFont(_fontName, 48);
                var levelMeasurements = TextMeasurer.Measure(levelText, new RendererOptions(levelFont));
                imageContext.DrawText(levelTextOpts, levelText, levelFont, Color.WhiteSmoke, new PointF(_width - _xPadding, _yPadding));

                // Username Text.
                var usernameTextOpts = GetOptions(HorizontalAlignment.Left, VerticalAlignment.Bottom);
                var usernameFont = _fonts.CreateFont(_fontName, 44);
                var tagFont = _fonts.CreateFont(_fontName, 28);
                var usernameMeasurements = TextMeasurer.Measure(member.Username, new RendererOptions(usernameFont));
                imageContext.DrawText(usernameTextOpts, member.Username, usernameFont, Color.WhiteSmoke, new PointF(_avatarHeight + (_xPadding * 2), _y1 - (_xPadding * 2)));
                imageContext.DrawText(usernameTextOpts, $" #{member.Discriminator}", tagFont, Color.Gray, new PointF(_avatarHeight + (_xPadding * 2) + usernameMeasurements.Width, _y1 - (_xPadding * 2)));

                var topRole = member.Roles.OrderByDescending(r => r.Position).FirstOrDefault();
                if (topRole != default)
                {
                    var serverRoleOptions = GetOptions(HorizontalAlignment.Left, VerticalAlignment.Bottom, _width - _avatarHeight - (_xPadding * 2) - levelMeasurements.Width);
                    imageContext.DrawSimpleText(serverRoleOptions,
                        topRole.Name,
                        _fonts.CreateFont(_fontName, 28),
                        Color.FromRgb(topRole.Color.R, topRole.Color.G, topRole.Color.B),
                        _avatarHeight + (_xPadding * 2),
                        _y1 - (_yPadding * 2) - usernameMeasurements.Height);
                }
            });

            var stream = new MemoryStream();
            image.Save(stream, new PngEncoder()
            {
                CompressionLevel = PngCompressionLevel.BestCompression
            });
            stream.Position = 0;
            return stream;
        }
    }

    private static DrawingOptions GetOptions(HorizontalAlignment horizontalAlignment, VerticalAlignment verticalAlignment, float? wrapWidth = null)
    {
        var opts = new DrawingOptions()
        {
            TextOptions = new TextOptions()
            {
                HorizontalAlignment = horizontalAlignment,
                VerticalAlignment = verticalAlignment
            }
        };
        if (wrapWidth.HasValue)
        {
            opts.TextOptions.WrapTextWidth = wrapWidth.Value;
        }
        return opts;
    }

    private static async Task<Image> GetAvatar(string avatarUrl)
    {
        Image avatar = null;
        if (!string.IsNullOrWhiteSpace(avatarUrl))
        {
            using (var client = new HttpClient())
            {
                using (var bytes = await client.GetStreamAsync(avatarUrl))
                {
                    avatar = Image.Load(bytes);
                }
            }
        }
        return avatar;
    }
}