using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Runtime.InteropServices;

namespace BlpConverter.BLP;

public static class RustBlpConverter
{
    private const string DLL_NAME = "rust_blp_converter.dll";

    public const int SUCCESS = 0;
    public const int ERROR_NULL_POINTER = -1;
    public const int ERROR_INVALID_PATH = -2;
    public const int ERROR_IO_ERROR = -3;
    public const int ERROR_IMAGE_ERROR = -4;
    public const int ERROR_UNKNOWN = -99;

    public enum ImageFormat
    {
        PNG = 0,
        JPEG = 1
    }

    public enum BlpCompression
    {
        Uncompressed = 0,
        DXT1 = 1,
        DXT3 = 2,
        DXT5 = 3
    }

    public enum BlpVersion : uint
    {
        BLP0,
        BLP1,
        BLP2
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BlpInfo
    {
        public uint Width;
        public uint Height;
        public uint MipmapCount;
        public BlpVersion Version;
        public uint Content;
        public uint Compression;
        public uint AlphaBits;
        public uint AlphaType;
        public uint HasMipmaps;
    }

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern int blp_to_image(
        [MarshalAs(UnmanagedType.LPStr)] string blpPath,
        [MarshalAs(UnmanagedType.LPStr)] string outputPath,
        int format
    );

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern int image_to_blp(
        [MarshalAs(UnmanagedType.LPStr)] string imagePath,
        [MarshalAs(UnmanagedType.LPStr)] string blpPath,
        int compression,
        int mipmapCount
    );

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern int blp_get_info(
        [MarshalAs(UnmanagedType.LPStr)] string blpPath,
        out uint width,
        out uint height,
        out uint mipmapCount
    );

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int blp_get_info_extended(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string blpPath,
        ref BlpInfo info
    );

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr get_error_message(int errorCode);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern void free_string(IntPtr ptr);

    public static void BlpToImage(string blpPath, string outputPath, ImageFormat format = ImageFormat.PNG)
    {
        int result = blp_to_image(blpPath, outputPath, (int)format);
        if (result != SUCCESS)
        {
            throw new Exception($"Failed to convert BLP to image: {GetErrorMessage(result)} (code: {result})");
        }
    }

    public static void ImageToBlp(string imagePath, string blpPath,
        BlpCompression compression = BlpCompression.DXT1, int mipmapCount = 0)
    {
        int result = image_to_blp(imagePath, blpPath, (int)compression, mipmapCount);
        if (result != SUCCESS)
        {
            throw new Exception($"Failed to convert image to BLP: {GetErrorMessage(result)} (code: {result})");
        }
    }

    public static (uint width, uint height, uint mipmaps) GetBlpInfo(string blpPath)
    {
        int result = blp_get_info(blpPath, out uint width, out uint height, out uint mipmaps);
        if (result != SUCCESS)
        {
            throw new Exception($"Failed to get BLP info: {GetErrorMessage(result)} (code: {result})");
        }
        return (width, height, mipmaps);
    }

    public static string GetErrorMessage(int errorCode)
    {
        IntPtr ptr = get_error_message(errorCode);
        if (ptr == IntPtr.Zero)
        {
            return "Unknown error";
        }

        try
        {
            return Marshal.PtrToStringAnsi(ptr) ?? "Unknown error";
        }
        finally
        {
            free_string(ptr);
        }
    }

    /// <summary>
    /// Извлекает сырые пиксели из BLP файла в формате RGBA8
    /// </summary>
    /// <param name="blpPath">Путь к BLP файлу</param>
    /// <param name="mipmapLevel">Уровень мипмапа (0 = полное разрешение)</param>
    /// <param name="width">Выходная ширина</param>
    /// <param name="height">Выходная высота</param>
    /// <param name="dataLen">Выходная длина данных в байтах</param>
    /// <returns>Указатель на пиксели (требует освобождения через free_pixel_data)</returns>
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr blp_get_pixels(
        [MarshalAs(UnmanagedType.LPStr)] string blpPath,
        int mipmapLevel,
        out uint width,
        out uint height,
        out UIntPtr dataLen
    );

    /// <summary>
    /// Освобождает пиксели, выделенные Rust
    /// </summary>
    /// <param name="ptr">Указатель на данные пикселей</param>
    /// <param name="len">Длина данных</param>
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern void free_pixel_data(IntPtr ptr, UIntPtr len);

    public static Image<Rgba32> LoadBlpImage(string blpPath, int mipmapLevel = 0)
    {
        // Получаем пиксели из Rust
        IntPtr pixelPtr = blp_get_pixels(blpPath, mipmapLevel, out uint width, out uint height, out UIntPtr dataLen);

        if (pixelPtr == IntPtr.Zero)
        {
            throw new Exception($"Failed to load BLP file: {blpPath}");
        }

        try
        {
            int len = (int)dataLen;

            // Копируем данные в управляемый массив
            byte[] pixelData = new byte[len];
            Marshal.Copy(pixelPtr, pixelData, 0, len);

            // Создаем ImageSharp Image из RGBA8 данных
            var image = Image.LoadPixelData<Rgba32>(pixelData, (int)width, (int)height);

            return image;
        }
        finally
        {
            // Освобождаем память, выделенную Rust
            free_pixel_data(pixelPtr, dataLen);
        }
    }

    public static Image LoadBlp(string blpPath, int mipmapLevel = 0)
    {
        return LoadBlpImage(blpPath, mipmapLevel);
    }
}
