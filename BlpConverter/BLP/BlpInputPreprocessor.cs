using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace BlpConverter.BLP;

public static class BlpInputPreprocessor
{
    // wow-blp stores dimensions as u32, but currently rejects values above 65535.
    // 32768 is therefore the largest power of two accepted by the encoder.
    public const int MaximumPowerOfTwoDimension = 32768;

    public static bool IsPowerOfTwo(int value) =>
        value > 0 && (value & (value - 1)) == 0;

    public static int NextPowerOfTwo(int value)
    {
        if (value <= 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Image dimensions must be positive.");

        if (value > MaximumPowerOfTwoDimension)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                $"Cannot resize {value} pixels to a wow-blp-compatible power of two. " +
                $"The largest supported value is {MaximumPowerOfTwoDimension}.");
        }

        int result = 1;
        while (result < value)
            result <<= 1;

        return result;
    }

    public static PreparedBlpInput Prepare(string sourcePath, bool resizeToPowerOfTwo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);

        if (!resizeToPowerOfTwo)
            return PreparedBlpInput.Unchanged(sourcePath);

        using Image<Rgba32> image = Image.Load<Rgba32>(sourcePath);
        int targetWidth = NextPowerOfTwo(image.Width);
        int targetHeight = NextPowerOfTwo(image.Height);

        if (targetWidth == image.Width && targetHeight == image.Height)
            return PreparedBlpInput.Unchanged(sourcePath);

        int sourceWidth = image.Width;
        int sourceHeight = image.Height;
        image.Mutate(context => context.Resize(new ResizeOptions
        {
            Size = new Size(targetWidth, targetHeight),
            Mode = ResizeMode.Stretch,
            Sampler = KnownResamplers.Lanczos3
        }));

        string temporaryPath = Path.Combine(
            Path.GetTempPath(),
            $"BlpConverter-{Guid.NewGuid():N}.png");

        image.SaveAsPng(temporaryPath);
        return PreparedBlpInput.Resized(
            sourcePath,
            temporaryPath,
            sourceWidth,
            sourceHeight,
            targetWidth,
            targetHeight);
    }
}

public sealed class PreparedBlpInput : IDisposable
{
    private readonly bool deleteOnDispose;

    private PreparedBlpInput(
        string originalPath,
        string path,
        bool deleteOnDispose,
        int originalWidth,
        int originalHeight,
        int width,
        int height)
    {
        OriginalPath = originalPath;
        Path = path;
        this.deleteOnDispose = deleteOnDispose;
        OriginalWidth = originalWidth;
        OriginalHeight = originalHeight;
        Width = width;
        Height = height;
    }

    public string OriginalPath { get; }
    public string Path { get; }
    public bool WasResized => deleteOnDispose;
    public int OriginalWidth { get; }
    public int OriginalHeight { get; }
    public int Width { get; }
    public int Height { get; }

    internal static PreparedBlpInput Unchanged(string path) =>
        new(path, path, false, 0, 0, 0, 0);

    internal static PreparedBlpInput Resized(
        string originalPath,
        string path,
        int originalWidth,
        int originalHeight,
        int width,
        int height) =>
        new(originalPath, path, true, originalWidth, originalHeight, width, height);

    public void Dispose()
    {
        if (deleteOnDispose && File.Exists(Path))
            File.Delete(Path);
    }
}
