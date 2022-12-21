using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace SteelBot.Helpers.Extensions;

public static class ImageContextExtensions
{
    // Implements a full image mutating pipeline operating on IImageProcessingContext
    public static IImageProcessingContext ConvertToAvatar(this IImageProcessingContext processingContext, Size size, float cornerRadius) =>
        processingContext.Resize(new ResizeOptions { Size = size, Mode = ResizeMode.Crop }).ApplyRoundedCorners(cornerRadius);

    // This method can be seen as an inline implementation of an `IImageProcessor`:
    // (The combination of `IImageOperations.Apply()` + this could be replaced with an `IImageProcessor`)
    public static IImageProcessingContext ApplyRoundedCorners(this IImageProcessingContext ctx, float cornerRadius)
    {
        var size = ctx.GetCurrentSize();
        var corners = BuildCorners(size.Width, size.Height, cornerRadius);

        ctx.SetGraphicsOptions(new GraphicsOptions
        {
            Antialias = true, AlphaCompositionMode = PixelAlphaCompositionMode.DestOut // enforces that any part of this shape that has color is punched out of the background
        });

        // mutating in here as we already have a cloned original
        // use any color (not Transparent), so the corners will be clipped
        foreach (var c in corners) ctx = ctx.Fill(Color.Red, c);
        return ctx;
    }

    public static IPathCollection BuildCorners(int imageWidth, int imageHeight, float cornerRadius)
    {
        // first create a square
        var rect = new RectangularPolygon(-0.5f, -0.5f, cornerRadius, cornerRadius);

        // then cut out of the square a circle so we are left with a corner
        var cornerTopLeft = rect.Clip(new EllipsePolygon(cornerRadius - 0.5f, cornerRadius - 0.5f, cornerRadius));

        // corner is now a corner shape positions top left
        //lets make 3 more positioned correctly, we can do that by translating the original around the center of the image

        float rightPos = imageWidth - cornerTopLeft.Bounds.Width + 1;
        float bottomPos = imageHeight - cornerTopLeft.Bounds.Height + 1;

        // move it across the width of the image - the width of the shape
        var cornerTopRight = cornerTopLeft.RotateDegree(90).Translate(rightPos, 0);
        var cornerBottomLeft = cornerTopLeft.RotateDegree(-90).Translate(0, bottomPos);
        var cornerBottomRight = cornerTopLeft.RotateDegree(180).Translate(rightPos, bottomPos);

        return new PathCollection(cornerTopLeft, cornerBottomLeft, cornerTopRight, cornerBottomRight);
    }

    public static void DrawRoundedRectangle(this IImageProcessingContext ctx, int width, int height, int topLeftX, int topLeftY, float roundingRadius, IBrush fillBrush)
    {
        using (var roundedBar = new Image<Rgba32>(width, height))
        {
            roundedBar.Mutate(x =>
            {
                x.Fill(fillBrush);
                x.ConvertToAvatar(new Size(width, height), roundingRadius);
            });

            ctx.DrawImage(roundedBar, new Point(topLeftX, topLeftY), 1);
        }
    }

    public static void DrawSimpleText(this IImageProcessingContext ctx, DrawingOptions opts, string text, Font font, Color color, float x, float y) =>
        ctx.DrawText(opts, text, font, color, new PointF(x, y));
}