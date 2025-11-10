# BLP Converter

BLP Converter is a cross-platform AvaloniaUI application for converting BLP (Blizzard Texture Format) images to PNG/JPEG and vice versa.

## Features

### Single File Conversion
- **Drag & Drop**: Drag files into the window for quick loading
- **Preview**: Display image in the preview window
- **File Information**: Detailed information about BLP files:
  - Dimensions (width x height)
  - Number of mipmaps
  - Encoding format (JPEG, Palette, DXT, ARGB8888)
  - Pixel format (DXT1, DXT3, DXT5, etc.)
  - Alpha channel depth
  - File size

- **Conversion**:
  - BLP → PNG
  - BLP → JPEG
  - PNG/JPEG → BLP

### Batch Conversion
- Select source and target folders
- Automatic conversion of all files in subfolders
- Progress bar showing conversion status
- Preserves folder structure

### Settings

#### Image Export Settings
- **Default Format**: PNG or JPEG
- **JPEG Quality**: 1-100 (default 95)

#### BLP Settings
- **Preserve Alpha**: Keep alpha channel
- **Generate Mipmaps**: Automatic mipmap generation
- **Compression Format**: Choose compression format:
  - DXT1 (smaller size, no alpha or 1-bit alpha)
  - DXT3 (4-bit alpha)
  - DXT5 (8-bit interpolated alpha)
  - Uncompressed (no compression)

### Configuration

All settings are automatically saved to a configuration file:
- Path: `config.json` (in the application directory)
- Automatic saving on changes
- Restores last used folders

## Usage

### Converting a Single File

1. Launch the application
2. Drag a file into the window or click "Browse..."
3. Review the preview and file information
4. Select conversion format (PNG, JPEG, or BLP)
5. Specify the save path

### Batch Conversion

1. Go to the "Batch Conversion" tab
2. Select the source folder
3. Select the target folder
4. Click "Start Conversion"
5. Wait for the process to complete

### Configuring Settings

1. Go to the "Settings" tab
2. Configure image export settings
3. Configure BLP settings
4. Changes are saved automatically

## Technical Details

- **Framework**: .NET 9.0
- **UI**: AvaloniaUI
- **Image Library**: SixLabors.ImageSharp 3.1.12
- **BLP Library**: [wow-blp](https://crates.io/crates/wow-blp) (Rust library via FFI)
- **Supported Formats**: BLP, PNG, JPEG

## Project Structure

```
BlpConverter/
├── BLP/
│   └── RustBlpConverter.cs # Rust FFI bindings for wow-blp library
├── Config/
│   └── AppConfig.cs        # Configuration management
├── MainWindow.axaml        # Main window UI (AXAML)
├── MainWindow.axaml.cs     # Main window logic
├── App.axaml               # Application resources
└── Program.cs              # Entry point

```

## Building the Project

```bash
dotnet build
```

## Running

```bash
dotnet run --project BlpConverter
```

## BLP Format Features

BLP (Blizzard Picture) is an image format used in Blizzard Entertainment games (World of Warcraft, Warcraft III, etc.).

### Format Versions
- **BLP0/BLP1**: Legacy versions
- **BLP2**: Current version (used in this application)

### Encoding Types
- **JPEG**: JPEG compression
- **Palette**: Paletted image (256 colors)
- **DXT**: DirectX compression (DXT1, DXT3, DXT5)
- **ARGB8888**: Uncompressed 32-bit image

### Mipmaps
BLP files can contain mipmaps - reduced versions of the image for performance optimization in games.
