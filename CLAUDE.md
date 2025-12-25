# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

PDFsharp 6.2.x is an open-source .NET library for creating and processing PDF documents. This repository targets .NET 10 only (CORE build). The project uses MIT license.

## Build Commands

```bash
# Download required assets (fonts, images, PDFs) - run once before first build
.\dev\download-assets.ps1

# Build the solution
dotnet build

# Build release
dotnet build -c Release

# Clean bin/obj folders (useful when dotnet build behaves strangely)
.\dev\del-bin-and-obj.ps1
```

## Testing

```bash
# Run all tests
dotnet test

# Run tests with the comprehensive script (Windows + WSL)
.\dev\run-tests.ps1

# Run tests for .NET 10 instead of .NET 8
.\dev\run-tests.ps1 -Net10 $true

# Skip build and just run tests
.\dev\run-tests.ps1 -SkipBuild $true

# Run all tests including slow ones
.\dev\run-tests.ps1 -RunAllTests $true

# Run specific test project
dotnet test src/foundation/src/PDFsharp/tests/PdfSharp.Tests/PdfSharp.Tests.csproj

# Run single test by name
dotnet test --filter "FullyQualifiedName~TestMethodName"
```

Test projects use xUnit with FluentAssertions. The `PDFsharpTests` environment variable controls whether slow tests run.

## Architecture

### Project Structure

```
src/
├── foundation/
│   ├── src/
│   │   ├── PDFsharp/           # Main PDFsharp projects
│   │   │   ├── src/
│   │   │   │   ├── PdfSharp/           # Core PDF library (CORE build)
│   │   │   │   ├── PdfSharp.Charting/  # Chart generation
│   │   │   │   ├── PdfSharp.BarCodes/  # Barcode generation
│   │   │   │   └── PdfSharp.Cryptography/  # PDF encryption/signatures
│   │   │   ├── features/       # Feature demo projects
│   │   │   └── tests/          # Test projects
│   │   └── shared/             # Shared infrastructure
│   │       └── src/
│   │           ├── PdfSharp.Shared/    # Shared internals
│   │           ├── PdfSharp.System/    # System extensions
│   │           ├── PdfSharp.Fonts/     # Font handling
│   │           ├── PdfSharp.Quality/   # Quality/testing utilities
│   │           ├── PdfSharp.Snippets/  # Code snippets
│   │           └── PdfSharp.Testing/   # Test infrastructure
│   └── nuget/                  # NuGet packaging
└── Directory.Packages.props    # Central package management
```

### Key Conventions

- **CORE constant**: All projects define `CORE` for the core (non-GDI/WPF) build
- **Central Package Management**: Package versions are defined in `src/Directory.Packages.props`
- **GitVersion**: Semantic versioning via GitVersion.MsBuild - requires at least one git commit
- **Strong naming**: Assemblies are signed with `StrongnameKey.snk`
- **Target framework**: .NET 10 only (`net10.0`)

### Code Tags

The codebase uses these tags for code tracking:

| Tag | Meaning |
|-----|---------|
| `TODO` | Must be done before release |
| `BUG` | Wrong code that must be fixed |
| `HACK` | Quick fix needing review |
| `REVIEW` | Needs discussion with another developer |
| `IMPROVE` | Works but has potential for improvement |
| `KEEP` | Old code kept for reference |

### PowerShell Scripts (dev folder)

All scripts require PowerShell 7:
- `download-assets.ps1` - Download test assets (required before first build)
- `download-fonts.ps1` - Download fonts only
- `run-tests.ps1` - Comprehensive test runner
- `del-bin-and-obj.ps1` - Clean build artifacts
- `build-local-nuget-packages-release.ps1` - Build NuGet packages

## Dependencies

- Uses BigGustave for PNG image reading (public domain)
- BouncyCastle.Cryptography for digital signatures
- xUnit + FluentAssertions for testing
