# BLP Converter

A desktop AvaloniaUI utility for converting BLP (Blizzard Texture Format) images to PNG/JPEG and back. The app uses a native Rust library (`rust_blp_converter`) for the actual BLP work; the repository currently ships a Windows `.dll`. To run on other platforms you need to provide a compatible native build and update the import name if required.

## Features

### Single File
- Drag & drop or file picker for BLP/PNG/JPEG.
- Preview of the loaded image.
- BLP metadata readout: version, dimensions, mipmap count, content/compression type, alpha bits, inferred pixel format, file size and estimated uncompressed size.
- Conversions:
  - BLP → PNG
  - BLP → JPEG (uses the native library’s defaults)
  - PNG/JPEG → BLP using the selected compression and optional mipmap generation

### Batch Conversion
- Converts BLP/PNG/JPEG recursively, preserving the folder structure.
- BLP → PNG when alpha is present, otherwise JPEG (uses the configured JPEG quality).
- PNG/JPEG → BLP with mipmaps optional; compression is DXT5 for PNG inputs and DXT1 for JPEG inputs.
- Progress bar and counter for long runs.

### Settings and Config
- Default output format selector (stored in config for future use; current conversions are driven by the buttons).
- JPEG quality (applied to batch BLP→JPEG exports without alpha).
- BLP compression and mipmap toggle for single-file PNG/JPEG → BLP conversions; mipmap toggle also affects batch conversion.
- Settings and last used folders persist in `config.json` in the application directory.

## Technical Details

- Framework: .NET 9.0
- UI: AvaloniaUI 11.3.x
- Image processing: SixLabors.ImageSharp 3.1.12
- BLP handling: [wow-blp](https://crates.io/crates/wow-blp) via the native `rust_blp_converter` library

## CI / Releases

Every push triggers GitHub Actions to build a self-contained single-file Windows (`win-x64`) binary and publish a release with a fresh tag based on the commit SHA. The release bundle (`blp-converter-win-x64.zip`) contains the executable and bundled native DLL.

## Project Structure

```
BlpConverter/
├── App.axaml               # Application resources
├── App.axaml.cs
├── Assets/icon.ico         # App icon
├── BLP/RustBlpConverter.cs # P/Invoke bindings to rust_blp_converter
├── Config/AppConfig.cs     # Config persistence
├── MainWindow.axaml        # UI layout
├── MainWindow.axaml.cs     # UI logic and conversions
├── Program.cs              # Entry point
└── rust_blp_converter.dll  # Native library (Windows build)
```

## Building and Running

```bash
dotnet build
dotnet run --project BlpConverter
```

Ensure `rust_blp_converter.dll` (or a platform-appropriate build) is available alongside the executable or in the working directory.
