using DSharpPlus.Entities;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SteelBot.Database.Models;
using SteelBot.Helpers.Extensions;
using SteelBot.Helpers.Levelling;
using SteelBot.Services.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace SteelBot.Helpers
{
    public class LevelCardGenerator
    {
        private const int Width = 800;
        private const int Height = 200;

        private const int XPadding = 10;
        private const int YPadding = 10;
        private const int XpBarHeight = 35;

        private const int X2 = Width - XPadding,
            Y1 = Height - YPadding - XpBarHeight,
            Y2 = Height - YPadding;

        private static readonly int AvatarHeight = Height - (YPadding * 2);
        private static readonly int XpBarWidth = Width - (XPadding * 2) - AvatarHeight;
        private static readonly Rgba32 BackgroundColour = Color.ParseHex("#2F3136");
        private static readonly Color XpBarBaseColour = Color.FromRgb(102, 102, 102);

        private static readonly int X1 = XPadding + AvatarHeight;

        private static readonly ColorStop[] XpGradient = new ColorStop[] { new ColorStop(0, Color.FromRgb(57, 161, 255)), new ColorStop(1, Color.FromRgb(0, 71, 191)) };
        private static readonly LinearGradientBrush GradientBrush = new LinearGradientBrush(new PointF(0, 0), new PointF(0, XpBarHeight), GradientRepetitionMode.Repeat, XpGradient);

        private static readonly FontCollection Fonts = new FontCollection();
        private const string FontName = "Roboto";
        private readonly FontFamily FontFamily;

        public LevelCardGenerator(AppConfigurationService appConfigurationService)
        {
            FontFamily = Fonts.Add(Path.Combine(appConfigurationService.BasePath, "Resources", "Fonts", "Roboto-Regular.ttf"));
        }

        public async Task<MemoryStream> GenerateCard(User user, DiscordMember member)
        {
            ulong xpForNextLevel = LevellingMaths.XpForLevel(user.CurrentLevel + 1);
            ulong xpForThisLevel = LevellingMaths.XpForLevel(user.CurrentLevel);

            ulong xpIntoThisLevel = (user.TotalXp - xpForThisLevel);
            ulong xpToAchieveNextLevel = (xpForNextLevel - xpForThisLevel);

            double progressToNextLevel = (((double)xpIntoThisLevel / (double)xpToAchieveNextLevel) * XpBarWidth) + XPadding + AvatarHeight;

            using (Image avatar = await GetAvatar(member.GetAvatarUrl(DSharpPlus.ImageFormat.Auto, 256)))
            using (Image<Rgba32> image = new Image<Rgba32>(Width, Height))
            {
                image.Mutate(imageContext =>
                {
                    imageContext.BackgroundColor(BackgroundColour);

                    // XP Bar.
                    int progressWidth = (int)Math.Round(progressToNextLevel - X1);

                    // Draw base empty bar.
                    imageContext.DrawRoundedRectangle(XpBarWidth, XpBarHeight, X1, Y1, XpBarHeight / 2, new SolidBrush(XpBarBaseColour));

                    if (progressWidth > 0)
                    {
                        // Draw current xp bar on top.
                        imageContext.DrawRoundedRectangle(progressWidth, XpBarHeight, X1, Y1, XpBarHeight / 2, GradientBrush);
                    }

                    // Avatar Image.
                    using (Image avatarRound = avatar.Clone(x => x.ConvertToAvatar(new Size(AvatarHeight, AvatarHeight), AvatarHeight / 2)))
                    {
                        imageContext.DrawImage(avatarRound, new Point(XPadding, YPadding), 1);
                    }

                    float xpBarMidpointX = X1 + (XpBarWidth / 2),
                    xpBarMidpointY = Y1 + (XpBarHeight / 2);

                    // Current XP text.
                    var currentXpOpts = GetOptions(FontFamily.CreateFont(XpBarHeight - 12),
                        HorizontalAlignment.Right,
                        VerticalAlignment.Center,
                        x: xpBarMidpointX,
                        y: xpBarMidpointY);
                    imageContext.DrawSimpleText(currentXpOpts, user.TotalXp.KiloFormat(), Color.WhiteSmoke);

                    // Remaining Xp text.
                    var remainingXpOpts = GetOptions(FontFamily.CreateFont(XpBarHeight - 12),
                        HorizontalAlignment.Left,
                        VerticalAlignment.Center,
                        x: xpBarMidpointX,
                        y: xpBarMidpointY);
                    imageContext.DrawSimpleText(remainingXpOpts, $" / {xpForNextLevel.KiloFormat()}", Color.LightGray);

                    // Current Level text.
                    var levelTextOpts = GetOptions(FontFamily.CreateFont(48),
                        HorizontalAlignment.Right,
                        VerticalAlignment.Top,
                        x: Width - XPadding,
                        y: YPadding);

                    string levelText = $"Level {user.CurrentLevel}";
                    FontRectangle levelMeasurements = TextMeasurer.Measure(levelText, levelTextOpts);
                    imageContext.DrawText(levelTextOpts, levelText, Color.WhiteSmoke);

                    // Username Text.
                    var usernameTextOpts = GetOptions(FontFamily.CreateFont(44),
                        HorizontalAlignment.Left,
                        VerticalAlignment.Bottom,
                        x: AvatarHeight + (XPadding * 2),
                        y: Y1 - (XPadding * 2));

                    FontRectangle usernameMeasurements = TextMeasurer.Measure(member.Username, usernameTextOpts);

                    var tagTextOpts = GetOptions(FontFamily.CreateFont(28),
                        HorizontalAlignment.Left,
                        VerticalAlignment.Bottom,
                        x: AvatarHeight + (XPadding * 2) + usernameMeasurements.Width,
                        y: Y1 - (XPadding * 2));


                    imageContext.DrawText(usernameTextOpts, member.Username, Color.WhiteSmoke);
                    imageContext.DrawText(tagTextOpts, $" #{member.Discriminator}", Color.Gray);

                    DiscordRole topRole = member.Roles.OrderByDescending(r => r.Position).FirstOrDefault();
                    if (topRole != default)
                    {
                        var serverRoleOptions = GetOptions(FontFamily.CreateFont(28),
                            HorizontalAlignment.Left,
                            VerticalAlignment.Bottom,
                            x: AvatarHeight + (XPadding * 2),
                            y: Y1 - (YPadding * 2) - usernameMeasurements.Height,
                            Width - AvatarHeight - (XPadding * 2) - levelMeasurements.Width);
                       
                        imageContext.DrawSimpleText(serverRoleOptions,
                            topRole.Name,
                            Color.FromRgb(topRole.Color.R, topRole.Color.G, topRole.Color.B));
                    }
                });

                MemoryStream stream = new MemoryStream();
                image.Save(stream, new PngEncoder()
                {
                    CompressionLevel = PngCompressionLevel.BestCompression
                });
                stream.Position = 0;
                return stream;
            }
        }

        private static TextOptions GetOptions(Font font, HorizontalAlignment horizontalAlignment, VerticalAlignment verticalAlignment, float x, float y, float? wrapWidth = null)
        {
            var opts = new TextOptions(font)
            {
                Origin = new System.Numerics.Vector2(x, y),
                HorizontalAlignment = horizontalAlignment,
                VerticalAlignment = verticalAlignment,
            };

            if (wrapWidth.HasValue)
            {
                opts.WrappingLength = wrapWidth.Value;
            }
            return opts;
        }

        private static async Task<Image> GetAvatar(string avatarUrl)
        {
            Image avatar = null;
            if (!string.IsNullOrWhiteSpace(avatarUrl))
            {
                using (HttpClient client = new HttpClient())
                {
                    using (Stream bytes = await client.GetStreamAsync(avatarUrl))
                    {
                        avatar = Image.Load(bytes);
                    }
                }
            }
            return avatar;
        }
    }
}