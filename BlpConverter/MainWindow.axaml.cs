using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using BlpConverter.BLP;
using BlpConverter.Config;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using System.Text;
using Image = SixLabors.ImageSharp.Image;
using ImageFormat = BlpConverter.Config.ImageFormat;

namespace BlpConverter;

public partial class MainWindow : Window
{
    private AppConfig config;
    private string currentFilePath;

    public MainWindow()
    {
        InitializeComponent();
        InitializeApp();
    }

    private void InitializeApp()
    {
        // Load configuration
        config = AppConfig.Load();

        // Setup drag and drop
        DropZone.AddHandler(DragDrop.DropEvent, DropZone_Drop);
        DropZone.AddHandler(DragDrop.DragOverEvent, DropZone_DragOver);

        // Setup event handlers
        BtnBrowse.Click += BtnBrowse_Click;
        BtnConvertToPng.Click += BtnConvertToPng_Click;
        BtnConvertToJpeg.Click += BtnConvertToJpeg_Click;
        BtnConvertToBlp.Click += BtnConvertToBlp_Click;
        BtnSelectSource.Click += BtnSelectSource_Click;
        BtnSelectTarget.Click += BtnSelectTarget_Click;
        BtnBatchConvert.Click += BtnBatchConvert_Click;

        // Setup settings controls
        CmbDefaultFormat.SelectedIndex = (int)config.ConversionSettings.DefaultOutputFormat;
        CmbDefaultFormat.SelectionChanged += (_, _) =>
        {
            config.ConversionSettings.DefaultOutputFormat = (ImageFormat)CmbDefaultFormat.SelectedIndex;
            config.Save();
        };

        CmbCompression.SelectedIndex = (int)config.ConversionSettings.BlpCompressionFormat;
        CmbCompression.SelectionChanged += (_, _) =>
        {
            config.ConversionSettings.BlpCompressionFormat = (BlpCompression)CmbCompression.SelectedIndex;
            config.Save();
        };

        NumJpegQuality.Value = config.ConversionSettings.JpegQuality;
        NumJpegQuality.ValueChanged += (_, _) =>
        {
            config.ConversionSettings.JpegQuality = (int)NumJpegQuality.Value;
            config.Save();
        };

        ChkGenerateMipmaps.IsChecked = config.ConversionSettings.GenerateMipmaps;
        ChkGenerateMipmaps.IsCheckedChanged += (_, _) =>
        {
            config.ConversionSettings.GenerateMipmaps = (bool)ChkGenerateMipmaps.IsChecked;
            config.Save();
        };

        // Set last used folders
        TxtSourceFolder.Text = config.LastSourceFolder;
        TxtTargetFolder.Text = config.LastTargetFolder;
    }

    private void DropZone_DragOver(object sender, DragEventArgs e)
    {
        if (e.DataTransfer.Contains(DataFormat.File))
        {
            e.DragEffects = DragDropEffects.Copy;
        }
    }

    private async void DropZone_Drop(object sender, DragEventArgs e)
    {
        var files = e.DataTransfer.TryGetFiles();
        var fileList = files?.ToList();
        if (fileList?.Count > 0)
        {
            var file = fileList[0];
            await LoadFile(file.Path.LocalPath);
        }
    }

    private async void BtnBrowse_Click(object sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select an image file",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Image Files") { Patterns = ["*.blp", "*.png", "*.jpg", "*.jpeg"]},
                new FilePickerFileType("BLP Files") { Patterns = ["*.blp"]},
                new FilePickerFileType("PNG Files") { Patterns = ["*.png"]},
                new FilePickerFileType("JPEG Files") { Patterns = ["*.jpg", "*.jpeg"]},
                new FilePickerFileType("All Files") { Patterns = ["*.*"]}
            ]
        });

        if (files.Count > 0)
        {
            await LoadFile(files[0].Path.LocalPath);
        }
    }

    private async Task LoadFile(string filePath)
    {
        try
        {
            currentFilePath = filePath;
            string ext = Path.GetExtension(filePath).ToLowerInvariant();

            // Clear previous file
            PreviewImage.Source = null;

            if (ext == ".blp")
                await LoadBlpFile(filePath);
            else if (ext is ".png" or ".jpg" or ".jpeg")
                await LoadImageFile(filePath);
            else
                await ShowMessage("Error", "Unsupported file format. Please select a BLP, PNG, or JPEG file.", MsBox.Avalonia.Enums.Icon.Error);
        }
        catch (Exception ex)
        {
            await ShowMessage("Error", $"Error loading file: {ex.Message}", MsBox.Avalonia.Enums.Icon.Error);
        }
    }

    private async Task LoadBlpFile(string filePath)
    {
        using Image image = RustBlpConverter.LoadBlp(filePath);
        using var ms = new MemoryStream();
        await image.SaveAsPngAsync(ms);
        ms.Position = 0;
        PreviewImage.Source = new Bitmap(ms);

        BlpInfo info = new();
        int result = RustBlpConverter.blp_get_info_extended(filePath, ref info);

        if (result != 0)
            return;

        // Display file info
        var output = new StringBuilder();
        output.AppendLine($"File: {Path.GetFileName(filePath)}");
        output.AppendLine($"Path: {Path.GetDirectoryName(filePath)}");
        output.AppendLine($"Size: {new FileInfo(filePath).Length / 1024.0:F2} KB");
        output.AppendLine($"Uncompressed size: {CalculateMemoryUsage(info):F2} KB");
        output.AppendLine();

        // Basic Information
        output.AppendLine("BASIC INFORMATION:");
        output.AppendLine($"  Format:       BLP (Version {info.Version})");
        output.AppendLine($"  Dimensions:   {info.Width}x{info.Height}");
        output.AppendLine($"  Mipmaps:      {info.MipmapCount} level(s)");
        output.AppendLine($"  Has Mipmaps:  {(info.HasMipmaps != 0 ? "Yes" : "No")}");
        output.AppendLine();

        // Encoding Information
        output.AppendLine("ENCODING INFORMATION:");
        output.AppendLine($"  Content Type: {GetContentTypeName(info.Content)}");
        output.AppendLine($"  Compression:  {GetCompressionName(info.Compression)}");
        output.AppendLine($"  Pixel Format: {GetPixelFormatName(info.Compression, info.AlphaBits)}");
        output.AppendLine($"  Alpha Depth:  {info.AlphaBits} bit");
        
        if (info.Version == RustBlpConverter.BlpVersion.BLP2) // BLP2
            output.AppendLine($"  Alpha Type:   {info.AlphaType}");

        output.AppendLine();

        // Mipmap Details
        if (info.MipmapCount > 1)
        {
            output.AppendLine("MIPMAP LEVELS:");

            for (uint i = 0; i < info.MipmapCount && i < 16; i++)
            {
                uint w = Math.Max(1, info.Width >> (int)i);
                uint h = Math.Max(1, info.Height >> (int)i);
                output.AppendLine($"  Level {i}: {w}x{h} ({w * h} pixels)");
            }
        }

        FileInfoText.Text = output.ToString();

        // Enable conversion buttons
        BtnConvertToPng.IsEnabled = true;
        BtnConvertToJpeg.IsEnabled = true;
        BtnConvertToBlp.IsEnabled = false;
    }

    private async Task LoadImageFile(string filePath)
    {
        using var image = await Image.LoadAsync(filePath);

        using var ms = new MemoryStream();
        await image.SaveAsPngAsync(ms);
        ms.Position = 0;
        PreviewImage.Source = new Bitmap(ms);

        // Display file info
        var info = new StringBuilder();
        info.AppendLine($"File: {Path.GetFileName(filePath)}");
        info.AppendLine($"Format: {Path.GetExtension(filePath).TrimStart('.').ToUpperInvariant()}");
        info.AppendLine($"Size: {image.Width}x{image.Height}");
        info.AppendLine($"File Size: {new FileInfo(filePath).Length / 1024.0:F2} KB");

        FileInfoText.Text = info.ToString();

        // Enable conversion buttons
        BtnConvertToPng.IsEnabled = false;
        BtnConvertToJpeg.IsEnabled = false;
        BtnConvertToBlp.IsEnabled = true;
    }

    private async void BtnConvertToPng_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(currentFilePath) || !File.Exists(currentFilePath))
            return;

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save PNG File",
            DefaultExtension = "png",
            SuggestedFileName = Path.GetFileNameWithoutExtension(currentFilePath) + ".png",
            FileTypeChoices = [new FilePickerFileType("PNG Files") { Patterns = ["*.png"]}]
        });

        if (file != null)
        {
            try
            {
                RustBlpConverter.BlpToImage(currentFilePath, file.Path.LocalPath);
                await ShowMessage("Success", "File converted successfully!", MsBox.Avalonia.Enums.Icon.Success);
            }
            catch (Exception ex)
            {
                await ShowMessage("Error", $"Error converting file: {ex.Message}", MsBox.Avalonia.Enums.Icon.Error);
            }
        }
    }

    private async void BtnConvertToJpeg_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(currentFilePath) || !File.Exists(currentFilePath))
            return;

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save JPEG File",
            DefaultExtension = "jpg",
            SuggestedFileName = Path.GetFileNameWithoutExtension(currentFilePath) + ".jpg",
            FileTypeChoices = [new FilePickerFileType("JPEG Files") { Patterns = ["*.jpg"]}]
        });

        if (file != null)
        {
            try
            {
                //await currentImage.SaveAsJpegAsync(file.Path.LocalPath, new JpegEncoder { Quality = config.ConversionSettings.JpegQuality });
                RustBlpConverter.BlpToImage(currentFilePath, file.Path.LocalPath, RustBlpConverter.ImageFormat.JPEG);
                await ShowMessage("Success", "File converted successfully!", MsBox.Avalonia.Enums.Icon.Success);
            }
            catch (Exception ex)
            {
                await ShowMessage("Error", $"Error converting file: {ex.Message}", MsBox.Avalonia.Enums.Icon.Error);
            }
        }
    }

    private async void BtnConvertToBlp_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(currentFilePath))
            return;

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save BLP File",
            DefaultExtension = "blp",
            SuggestedFileName = Path.GetFileNameWithoutExtension(currentFilePath) + ".blp",
            FileTypeChoices = [new FilePickerFileType("BLP Files") { Patterns = ["*.blp"]}]
        });

        if (file != null)
        {
            try
            {
                RustBlpConverter.ImageToBlp(currentFilePath, file.Path.LocalPath, config.ConversionSettings.BlpCompressionFormat, config.ConversionSettings.GenerateMipmaps ? 0 : 1);
                await ShowMessage("Success", "File converted successfully!", MsBox.Avalonia.Enums.Icon.Success);
            }
            catch (Exception ex)
            {
                await ShowMessage("Error", $"Error converting file: {ex.Message}", MsBox.Avalonia.Enums.Icon.Error);
            }
        }
    }

    private async void BtnSelectSource_Click(object sender, RoutedEventArgs e)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select source folder",
            AllowMultiple = false
        });

        if (folders.Count > 0)
        {
            TxtSourceFolder.Text = folders[0].Path.LocalPath;
            config.LastSourceFolder = folders[0].Path.LocalPath;
            config.Save();
        }
    }

    private async void BtnSelectTarget_Click(object sender, RoutedEventArgs e)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select target folder",
            AllowMultiple = false
        });

        if (folders.Count > 0)
        {
            TxtTargetFolder.Text = folders[0].Path.LocalPath;
            config.LastTargetFolder = folders[0].Path.LocalPath;
            config.Save();
        }
    }

    private async void BtnBatchConvert_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtSourceFolder.Text) || !Directory.Exists(TxtSourceFolder.Text))
        {
            await ShowMessage("Error", "Please select a valid source folder.", MsBox.Avalonia.Enums.Icon.Error);
            return;
        }

        if (string.IsNullOrWhiteSpace(TxtTargetFolder.Text) || !Directory.Exists(TxtTargetFolder.Text))
        {
            await ShowMessage("Error", "Please select a valid target folder.", MsBox.Avalonia.Enums.Icon.Error);
            return;
        }

        BtnBatchConvert.IsEnabled = false;
        BatchProgressBar.Value = 0;

        try
        {
            var files = Directory.GetFiles(TxtSourceFolder.Text, "*.*", SearchOption.AllDirectories)
                .Where(f => new[] { ".blp", ".png", ".jpg", ".jpeg" }.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .ToArray();

            BatchProgressBar.Maximum = files.Length;

            // Get UI values before entering background threads
            var sourceFolder = TxtSourceFolder.Text;
            var targetFolder = TxtTargetFolder.Text;

            for (int i = 0; i < files.Length; i++)
            {
                var sourceFile = files[i];
                var relativePath = Path.GetRelativePath(sourceFolder, sourceFile);
                var ext = Path.GetExtension(sourceFile).ToLowerInvariant();

                // Convert file
                await Task.Run(() =>
                {
                    string targetFile;

                    if (ext == ".blp")
                    {
                        // Get BLP info to check for alpha channel
                        BlpInfo info = new();
                        int result = RustBlpConverter.blp_get_info_extended(sourceFile, ref info);
                        if (result != 0)
                            return;

                        // Automatically choose format based on alpha channel presence
                        bool hasAlpha = info.AlphaBits > 0;
                        var targetExt = hasAlpha ? ".png" : ".jpg";

                        targetFile = Path.Combine(targetFolder, Path.ChangeExtension(relativePath, targetExt));

                        // Create target directory if needed
                        var targetDir = Path.GetDirectoryName(targetFile);
                        if (!Directory.Exists(targetDir))
                            Directory.CreateDirectory(targetDir!);

                        using var image = RustBlpConverter.LoadBlp(sourceFile);

                        if (hasAlpha)
                            image.SaveAsPng(targetFile);
                        else
                            image.SaveAsJpeg(targetFile, new JpegEncoder { Quality = config.ConversionSettings.JpegQuality });
                    }
                    else
                    {
                        BlpCompression compression = BlpCompression.DXT5;

                        if (ext is ".jpg" or ".jpeg")
                            compression = BlpCompression.DXT1;

                        // Convert PNG/JPEG to BLP
                        targetFile = Path.Combine(targetFolder, Path.ChangeExtension(relativePath, ".blp"));

                        // Create target directory if needed
                        var targetDir = Path.GetDirectoryName(targetFile);
                        if (!Directory.Exists(targetDir))
                            Directory.CreateDirectory(targetDir!);

                        RustBlpConverter.ImageToBlp(sourceFile, targetFile, compression, config.ConversionSettings.GenerateMipmaps ? 0 : 1);
                    }
                });

                BatchProgressBar.Value = i + 1;
                LblProgress.Text = $"Progress: {i + 1}/{files.Length}";
            }

            await ShowMessage("Success", $"Batch conversion completed! Converted {files.Length} files.", MsBox.Avalonia.Enums.Icon.Success);
        }
        catch (Exception ex)
        {
            await ShowMessage("Error", $"Error during batch conversion: {ex.Message}", MsBox.Avalonia.Enums.Icon.Error);
        }
        finally
        {
            BtnBatchConvert.IsEnabled = true;
            BatchProgressBar.Value = 0;
            LblProgress.Text = "Progress:";
        }
    }

    private async Task ShowMessage(string title, string message, Icon icon)
    {
        var box = MessageBoxManager.GetMessageBoxStandard(title, message, ButtonEnum.Ok, icon);
        await box.ShowWindowDialogAsync(this);
    }

    #region MyRegion

    static string GetContentTypeName(uint content)
    {
        return content switch
        {
            0 => "JPEG (compressed)",
            1 => "Direct (raw pixel data)",
            _ => $"Unknown ({content})"
        };
    }

    private static string GetCompressionName(Compression compression)
    {
        return compression switch
        {
            Compression.Jpeg => "JPEG",
            Compression.Raw1 => "Raw1 (Uncompressed)",
            Compression.Raw3 => "Raw3 (Uncompressed with palette)",
            Compression.Dxtc => "DXTC (DXT1/DXT3/DXT5)",
            _ => $"Unknown ({compression})"
        };
    }

    private static string GetPixelFormatName(Compression compression, uint alphaBits)
    {
        if (compression == Compression.Dxtc)
        {
            return alphaBits switch
            {
                0 => "DXT1 (RGB, no alpha)",
                1 => "DXT1 (RGB + 1-bit alpha)",
                8 => "DXT5 (RGBA, 8-bit alpha)",
                _ => $"DXT (unknown alpha: {alphaBits}-bit)"
            };
        }
        else if (compression == Compression.Raw1)
        {
            return alphaBits switch
            {
                0 => "RGB24",
                8 => "RGBA32",
                _ => $"Raw ({alphaBits}-bit alpha)"
            };
        }
        else if (compression == Compression.Raw3)
            return "Paletted (256 colors, 4-bit alpha)";
        else // JPEG
            return "JPEG (lossy)";
    }

    static double CalculateMemoryUsage(BlpInfo info)
    {
        double totalBytes = 0;
        uint width = info.Width;
        uint height = info.Height;

        // Calculate for all mipmap levels
        for (uint i = 0; i < info.MipmapCount; i++)
        {
            uint w = Math.Max(1, width >> (int)i);
            uint h = Math.Max(1, height >> (int)i);
            totalBytes += w * h * 4; // RGBA = 4 bytes per pixel
        }

        return totalBytes / 1024.0; // Convert to KB
    }

    #endregion
}
