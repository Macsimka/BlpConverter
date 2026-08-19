using BlpConverter.BLP;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace BlpConverter.Tests;

public sealed class BlpInputPreprocessorTests
{
    [Theory]
    [InlineData(1, 1)]
    [InlineData(128, 128)]
    [InlineData(200, 256)]
    [InlineData(300, 512)]
    [InlineData(1025, 2048)]
    [InlineData(32768, 32768)]
    public void NextPowerOfTwo_ReturnsExpectedDimension(int source, int expected)
    {
        Assert.Equal(expected, BlpInputPreprocessor.NextPowerOfTwo(source));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(32769)]
    public void NextPowerOfTwo_RejectsUnsupportedDimensions(int source)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            BlpInputPreprocessor.NextPowerOfTwo(source));
    }

    [Fact]
    public void Prepare_WhenDisabled_DoesNotTouchNonPowerOfTwoInput()
    {
        string sourcePath = CreateImage(3, 5, new Rgba32(10, 20, 30, 37));

        try
        {
            using var prepared = BlpInputPreprocessor.Prepare(sourcePath, false);

            Assert.False(prepared.WasResized);
            Assert.Equal(sourcePath, prepared.Path);
            Assert.True(File.Exists(sourcePath));
        }
        finally
        {
            File.Delete(sourcePath);
        }
    }

    [Fact]
    public void Prepare_WhenAlreadyPowerOfTwo_DoesNotCreateTemporaryFile()
    {
        string sourcePath = CreateImage(4, 8, new Rgba32(10, 20, 30, 37));

        try
        {
            using var prepared = BlpInputPreprocessor.Prepare(sourcePath, true);

            Assert.False(prepared.WasResized);
            Assert.Equal(sourcePath, prepared.Path);
        }
        finally
        {
            File.Delete(sourcePath);
        }
    }

    [Fact]
    public void Prepare_ResizesEachDimensionAndPreservesAlpha()
    {
        var expectedPixel = new Rgba32(10, 20, 30, 37);
        string sourcePath = CreateImage(3, 5, expectedPixel);
        string? temporaryPath = null;

        try
        {
            using (var prepared = BlpInputPreprocessor.Prepare(sourcePath, true))
            {
                temporaryPath = prepared.Path;

                Assert.True(prepared.WasResized);
                Assert.Equal(3, prepared.OriginalWidth);
                Assert.Equal(5, prepared.OriginalHeight);
                Assert.Equal(4, prepared.Width);
                Assert.Equal(8, prepared.Height);
                Assert.NotEqual(sourcePath, prepared.Path);

                using Image<Rgba32> resized = Image.Load<Rgba32>(prepared.Path);
                Assert.Equal(4, resized.Width);
                Assert.Equal(8, resized.Height);

                resized.ProcessPixelRows(accessor =>
                {
                    for (int y = 0; y < accessor.Height; y++)
                    {
                        Span<Rgba32> row = accessor.GetRowSpan(y);
                        foreach (Rgba32 pixel in row)
                            Assert.Equal(expectedPixel, pixel);
                    }
                });
            }

            Assert.False(File.Exists(temporaryPath));
        }
        finally
        {
            File.Delete(sourcePath);
            if (temporaryPath != null)
                File.Delete(temporaryPath);
        }
    }

    private static string CreateImage(int width, int height, Rgba32 pixel)
    {
        string path = Path.Combine(Path.GetTempPath(), $"BlpConverter-test-{Guid.NewGuid():N}.png");
        using var image = new Image<Rgba32>(width, height, pixel);
        image.SaveAsPng(path);
        return path;
    }
}
