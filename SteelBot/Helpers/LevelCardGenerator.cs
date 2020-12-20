using DSharpPlus.Entities;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SteelBot.Database.Models;
using SteelBot.Helpers.Extensions;
using SteelBot.Services.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
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

        public LevelCardGenerator(AppConfigurationService appConfigurationService)
        {
            Fonts.Install(Path.Combine(appConfigurationService.BasePath, "Resources", "Fonts", "Roboto-Regular.ttf"));
        }

        public async Task<MemoryStream> GenerateCard(User user, DiscordMember member)
        {
            ulong xpForNextLevel = LevellingMaths.XpForLevel(user.CurrentLevel + 1);
            double progressToNextLevel = (((double)user.TotalXp / (double)xpForNextLevel) * XpBarWidth) + XPadding + AvatarHeight;

            using (var avatar = await GetAvatar(member.AvatarUrl))
            using (var image = new Image<Rgba32>(Width, Height))
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
                    using (var avatarRound = avatar.Clone(x => x.ConvertToAvatar(new Size(AvatarHeight, AvatarHeight), AvatarHeight / 2)))
                    {
                        imageContext.DrawImage(avatarRound, new Point(XPadding, YPadding), 1);
                    }

                    float xpBarMidpointX = X1 + (XpBarWidth / 2),
                    xpBarMidpointY = Y1 + (XpBarHeight / 2);
                    // Current XP text.
                    imageContext.DrawSimpleText(GetOptions(HorizontalAlignment.Right, VerticalAlignment.Center),
                        user.TotalXp.KiloFormat(),
                        Fonts.CreateFont(FontName, XpBarHeight - 12),
                        Color.WhiteSmoke,
                        xpBarMidpointX,
                        xpBarMidpointY);

                    // Remaining Xp text.
                    imageContext.DrawSimpleText(GetOptions(HorizontalAlignment.Left, VerticalAlignment.Center),
                        $" / {xpForNextLevel.KiloFormat()}",
                        Fonts.CreateFont(FontName, XpBarHeight - 12),
                        Color.LightGray,
                        xpBarMidpointX,
                        xpBarMidpointY);

                    // Current Level text.
                    var levelTextOpts = GetOptions(HorizontalAlignment.Right, VerticalAlignment.Top);
                    string levelText = $"Level {user.CurrentLevel}";
                    var levelFont = Fonts.CreateFont(FontName, 48);
                    var levelMeasurements = TextMeasurer.Measure(levelText, new RendererOptions(levelFont));
                    imageContext.DrawText(levelTextOpts, levelText, levelFont, Color.WhiteSmoke, new PointF(Width - XPadding, YPadding));

                    // Username Text.
                    var usernameTextOpts = GetOptions(HorizontalAlignment.Left, VerticalAlignment.Bottom);
                    var usernameFont = Fonts.CreateFont(FontName, 44);
                    var tagFont = Fonts.CreateFont(FontName, 28);
                    var usernameMeasurements = TextMeasurer.Measure(member.Username, new RendererOptions(usernameFont));
                    imageContext.DrawText(usernameTextOpts, member.Username, usernameFont, Color.WhiteSmoke, new PointF(AvatarHeight + (XPadding * 2), Y1 - (XPadding * 2)));
                    imageContext.DrawText(usernameTextOpts, $" #{member.Discriminator}", tagFont, Color.Gray, new PointF(AvatarHeight + (XPadding * 2) + usernameMeasurements.Width, Y1 - (XPadding * 2)));

                    var topRole = member.Roles.FirstOrDefault();
                    if (topRole != default)
                    {
                        var serverRoleOptions = GetOptions(HorizontalAlignment.Left, VerticalAlignment.Bottom, Width - AvatarHeight - (XPadding * 2) - levelMeasurements.Width);
                        imageContext.DrawSimpleText(serverRoleOptions,
                            topRole.Name,
                            Fonts.CreateFont(FontName, 28),
                            Color.FromRgb(topRole.Color.R, topRole.Color.G, topRole.Color.B),
                            AvatarHeight + (XPadding * 2),
                            Y1 - (YPadding * 2) - usernameMeasurements.Height);
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

        private static TextGraphicsOptions GetOptions(HorizontalAlignment horizontalAlignment, VerticalAlignment verticalAlignment, float? wrapWidth = null)
        {
            TextGraphicsOptions opts = new TextGraphicsOptions()
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
                using (HttpClient client = new HttpClient())
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
}