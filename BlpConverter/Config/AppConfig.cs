using System.Text.Json;
using static BlpConverter.BLP.RustBlpConverter;

namespace BlpConverter.Config;

public class AppConfig
{
    public string LastSourceFolder { get; set; } = string.Empty;
    public string LastTargetFolder { get; set; } = string.Empty;
    public ConversionSettings ConversionSettings { get; set; } = new();

    private static readonly string ConfigPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        "config.json");

    public static AppConfig Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
            }
        }
        catch
        {
            // If config is corrupted, return new config
        }

        return new AppConfig();
    }

    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(ConfigPath, json);
        }
        catch
        {
            // Silently fail if we can't save config
        }
    }
}

public class ConversionSettings
{
    public ImageFormat DefaultOutputFormat { get; set; } = ImageFormat.Png;
    public int JpegQuality { get; set; } = 95;
    public bool GenerateMipmaps { get; set; } = true;
    public BlpCompression BlpCompressionFormat { get; set; } = BlpCompression.DXT5;
    public bool PreserveAlpha { get; set; } = true;
}

public enum ImageFormat
{
    Png,
    Jpeg
}
