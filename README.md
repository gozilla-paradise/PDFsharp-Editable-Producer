<p align="center">
  <img src="https://www.pdfsharp.com/assets/img/PDFsharp-Logo-lowquality.png" alt="PDFsharp Logo" width="300"/>
</p>

<h1 align="center">PDFsharp 6</h1>

<p align="center">
  <strong>A .NET library for creating and processing PDF documents</strong>
</p>

<p align="center">
  <a href="#-features">Features</a> •
  <a href="#-quick-start">Quick Start</a> •
  <a href="#-installation">Installation</a> •
  <a href="#-documentation">Documentation</a> •
  <a href="#-contributing">Contributing</a>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/version-6.2.3-blue?style=for-the-badge" alt="Version"/>
  <img src="https://img.shields.io/badge/.NET-10.0-purple?style=for-the-badge" alt=".NET 10"/>
  <img src="https://img.shields.io/badge/license-MIT-green?style=for-the-badge" alt="MIT License"/>
  <img src="https://img.shields.io/badge/C%23-13-brightgreen?style=for-the-badge" alt="C# 13"/>
</p>

---

## ✨ Features

| Feature | Description |
|---------|-------------|
| 📄 **PDF Creation** | Create PDF documents from scratch with full control over content and layout |
| 🔧 **PDF Modification** | Read, modify, and merge existing PDF documents |
| 📊 **Charting** | Generate charts and graphs with `PdfSharp.Charting` |
| 📱 **Barcodes** | Create various barcode types with `PdfSharp.BarCodes` |
| 🔐 **Encryption** | Secure PDFs with encryption and digital signatures via `PdfSharp.Cryptography` |
| 🎨 **Graphics** | Rich drawing API with support for text, images, and vector graphics |
| 🖼️ **Image Support** | Embed JPEG, PNG, and other image formats |
| ♿ **Accessibility** | PDF/UA support for accessible documents |
| 📋 **PDF/A** | Create archival-quality PDF/A compliant documents |

---

## 🚀 Quick Start

### Prerequisites

- **.NET 10 SDK** or later
- **PowerShell 7** (for build scripts)
- **Git** (required for versioning)

### Clone & Build

```bash
# Clone the repository
git clone https://github.com/empira/PDFsharp.git
cd PDFsharp

# Download required assets (fonts, images, test PDFs)
.\dev\download-assets.ps1

# Build the solution
dotnet build
```

### Create Your First PDF

```csharp
using PdfSharp.Pdf;
using PdfSharp.Drawing;

// Create a new PDF document
var document = new PdfDocument();
document.Info.Title = "Hello, PDFsharp!";

// Add a page
var page = document.AddPage();

// Get an XGraphics object for drawing
var gfx = XGraphics.FromPdfPage(page);

// Draw text
var font = new XFont("Arial", 20);
gfx.DrawString("Hello, World!", font, XBrushes.Black,
    new XRect(0, 0, page.Width, page.Height),
    XStringFormats.Center);

// Save the document
document.Save("HelloWorld.pdf");
```

---

## 📦 Installation

### NuGet Package

```bash
dotnet add package PDFsharp
```

### Build from Source

```bash
# Standard build
dotnet build

# Release build
dotnet build -c Release

# Clean build artifacts (if issues occur)
.\dev\del-bin-and-obj.ps1
```

---

## 🧪 Testing

```bash
# Run all tests
dotnet test

# Comprehensive test runner (Windows + WSL)
.\dev\run-tests.ps1

# Run with .NET 10
.\dev\run-tests.ps1 -Net10 $true

# Include slow tests
.\dev\run-tests.ps1 -RunAllTests $true

# Run specific test
dotnet test --filter "FullyQualifiedName~YourTestName"
```

---

## 📁 Project Structure

```
PDFsharp/
├── 📂 src/
│   └── 📂 foundation/
│       ├── 📂 src/
│       │   ├── 📂 PDFsharp/
│       │   │   ├── 📂 src/
│       │   │   │   ├── 📦 PdfSharp           # Core PDF library
│       │   │   │   ├── 📦 PdfSharp.Charting  # Chart generation
│       │   │   │   ├── 📦 PdfSharp.BarCodes  # Barcode support
│       │   │   │   └── 📦 PdfSharp.Cryptography  # Encryption & signatures
│       │   │   ├── 📂 features/              # Feature demos
│       │   │   └── 📂 tests/                 # Unit tests
│       │   └── 📂 shared/                    # Shared infrastructure
│       │       └── 📂 src/
│       │           ├── 📦 PdfSharp.Shared    # Internal utilities
│       │           ├── 📦 PdfSharp.System    # System extensions
│       │           ├── 📦 PdfSharp.Fonts     # Font handling
│       │           └── 📦 PdfSharp.Testing   # Test infrastructure
│       └── 📂 nuget/                         # NuGet packaging
├── 📂 dev/                                   # Development scripts
├── 📂 docs/                                  # Internal documentation
└── 📄 PdfSharp.sln                           # Solution file
```

---

## 📖 Documentation

| Resource | Link |
|----------|------|
| 📚 **Official Docs** | [docs.pdfsharp.net](https://docs.pdfsharp.net/) |
| 📜 **License** | [MIT License](https://docs.pdfsharp.net/LICENSE.html) |
| 💡 **Samples** | [PDFsharp Samples](https://docs.pdfsharp.net/samples/) |
| ❓ **FAQ** | [Frequently Asked Questions](https://docs.pdfsharp.net/faq/) |

---

## ⚙️ Configuration

### Central Package Management

All NuGet package versions are centrally managed in `src/Directory.Packages.props`. When adding new packages, define versions there.

### Versioning

PDFsharp uses **GitVersion** for semantic versioning:

```bash
# Set a specific version tag
git tag v6.2.0

# Without a tag, version 0.1.0 is used
```

> ⚠️ **Note**: A git repository with at least one commit is required for the build to succeed.

---

## 🛠️ Development Scripts

All scripts are in the `dev/` folder and require **PowerShell 7**:

| Script | Description |
|--------|-------------|
| `download-assets.ps1` | Download test assets (required before first build) |
| `download-fonts.ps1` | Download fonts only |
| `run-tests.ps1` | Comprehensive test runner for Windows & WSL |
| `del-bin-and-obj.ps1` | Clean all build artifacts |
| `build-local-nuget-packages-release.ps1` | Build NuGet packages |

---

## 📚 Third-Party Libraries

| Library | Purpose | License |
|---------|---------|---------|
| [BigGustave](https://github.com/EliotJones/BigGustave) | PNG image reading | Public Domain |
| [BouncyCastle](https://www.bouncycastle.org/) | Cryptography & signatures | MIT |

---

## 👥 Authors

### Current Maintainers

<table>
  <tr>
    <td align="center"><strong>Stefan Lange</strong></td>
    <td align="center"><strong>Thomas Hövel</strong></td>
    <td align="center"><strong>Martin Ossendorf</strong></td>
    <td align="center"><strong>Andreas Seifert</strong></td>
  </tr>
</table>

### Original PDFsharp Team

Stefan Lange • Niklas Schneider • David Stephensen

### Original MigraDoc Team

Klaus Potzesny • Niklas Schneider • Stefan Lange

---

## 🤝 Contributing

We welcome contributions! Please feel free to submit issues and pull requests.

---

## 📄 License

PDFsharp is licensed under the **MIT License**.

```
PDFsharp: Copyright (c) 2005-2025 empira Software GmbH, Troisdorf (Cologne Area), Germany
MigraDoc: Copyright (c) 2001-2025 empira Software GmbH, Troisdorf (Cologne Area), Germany
```

---

<p align="center">
  Made with ❤️ by the PDFsharp Team
</p>

<p align="center">
  <a href="https://docs.pdfsharp.net">Documentation</a> •
  <a href="https://github.com/empira/PDFsharp/issues">Issues</a> •
  <a href="https://www.nuget.org/packages/PDFsharp">NuGet</a>
</p>
