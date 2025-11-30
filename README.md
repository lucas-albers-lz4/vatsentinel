# Vat Sentinel

A RimWorld mod that provides automated management of growth vats by enforcing configurable biological age limits for pawns. Vat Sentinel monitors pawn development and automatically ejects them at specified age milestones, ensuring optimal resource management and colony efficiency.

## Overview

Vat Sentinel extends RimWorld's Biotech DLC growth vat functionality with intelligent automation. The mod tracks pawns throughout their development cycle and automatically ejects them when they reach configurable age thresholds (childhood at 3 years, adolescence at 13 years, or adulthood at 18 years). This eliminates the need for manual monitoring and prevents pawns from remaining in vats longer than necessary.

### Key Features

- **Automated Age-Based Ejection**: Configurable ejection at biological age milestones (3, 13, or 18 years)
- **Persistent State Management**: Ejection schedules persist across game saves and are recalculated on load
- **Robust Error Handling**: Retry logic for failed ejections with user notifications
- **Harmony-Based Integration**: Non-invasive patches that maintain compatibility with other mods
- **Comprehensive Logging**: Detailed debug logging for troubleshooting and development

## Requirements

- **RimWorld**: Version 1.6 (also compatible with 1.4 and 1.5)
- **DLC**: Biotech (required)
- **Harmony**: Automatically managed via NuGet

## Installation

### For Players

1. Download the latest release from the [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=YOUR_WORKSHOP_ID) or GitHub releases
2. Extract to your RimWorld `Mods` folder: `%RIMWORLD_ROOT%\Mods\VatSentinel\`
3. Launch RimWorld and enable "Vat Sentinel" in the Mod Manager
4. Restart RimWorld when prompted

### For Developers

See [Building](#building) and [Development](#development) sections below.

## Usage

### Configuration

Access mod settings through: **Options → Mod Settings → Vat Sentinel**

Available settings:
- **Eject when reaching childhood (age 3)**: Automatically eject pawns at 3 years of age
- **Eject when reaching adolescence (age 13)**: Automatically eject pawns at 13 years of age
- **Eject when reaching adulthood (age 18)**: Automatically eject pawns at 18 years of age

Multiple thresholds can be enabled simultaneously; the mod will eject at the earliest enabled threshold.

### How It Works

1. When a pawn is placed in a growth vat, Vat Sentinel registers the pawn and records the entry time
2. The mod continuously monitors the pawn's biological age during each game tick
3. When a configured age threshold is reached, the mod attempts to eject the pawn
4. Success or failure notifications are displayed to the player
5. If ejection fails (e.g., vat is blocked), the mod schedules a retry

## Architecture

Vat Sentinel follows a modular architecture with clear separation of concerns:

- **VatSentinelWorldComponent**: Persistent game component managing pawn tracking and state
- **VatTrackingRecord**: Data model representing tracked pawns and their ejection schedules
- **VatEjectionSchedule**: Business logic for calculating ejection targets based on age thresholds
- **VatSentinelScheduler**: Tick-based evaluation and ejection execution
- **CompVatGrowerReflection**: Reflection-based access to RimWorld's vat API
- **Harmony Patches**: Non-invasive integration points with RimWorld's vat system

For detailed architecture documentation, see [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md).

## Building

### Prerequisites

- Visual Studio 2022 Build Tools (or Visual Studio 2022)
- .NET Framework 4.7.2 Developer Pack
- RimWorld 1.6 reference assemblies (managed via NuGet)

### Quick Build

Run the build script from the repository root:

```batch
build.bat
```

This will:
- Set up the Visual Studio build environment
- Restore NuGet packages
- Build the solution with code analyzers enabled
- Display any compilation errors or analyzer warnings

### Linting

To run code quality checks without a full build:

```batch
lint.bat
```

For detailed linting documentation, see [`docs/LINTING.md`](docs/LINTING.md).

## Development

### Project Structure

```
VatSentinel/
├── About/              # Mod metadata and Steam Workshop configuration
├── Assemblies/         # Compiled DLL output
├── Defs/              # RimWorld XML definitions
├── docs/              # Documentation
│   ├── ARCHITECTURE.md
│   ├── Design.md
│   ├── LINTING.md
│   ├── TESTING.md
│   └── TODO.md
├── Languages/         # Localization files
├── Source/            # C# source code
│   └── VatSentinel/
│       ├── Patches/   # Harmony patches
│       ├── Scheduling/# Business logic
│       └── ...
└── Textures/          # Mod assets
```

### Code Quality

This project maintains high code quality standards through:

- **Static Analysis**: StyleCop.Analyzers and Microsoft.CodeAnalysis.NetAnalyzers
- **Consistent Formatting**: EditorConfig for cross-IDE compatibility
- **Comprehensive Logging**: Structured logging for debugging and diagnostics
- **Error Handling**: Robust exception handling with retry logic

See [`docs/LINTING.md`](docs/LINTING.md) for detailed information on code quality tools and practices.

### Testing

Comprehensive testing procedures are documented in [`docs/TESTING.md`](docs/TESTING.md). The testing strategy includes:

- Unit testing for age calculation logic
- Integration testing with RimWorld's vat system
- Compatibility testing with reference mods
- Regression testing across game versions

## Contributing

Contributions are welcome! Please see [`CONTRIBUTING.md`](CONTRIBUTING.md) for detailed guidelines on:

- Code style and quality standards
- Development setup and workflow
- Testing requirements
- Pull request process
- Commit message conventions

Quick start:
1. Fork the repository
2. Create a feature branch from `master`
3. Make your changes following the coding standards
4. Test thoroughly using [`docs/TESTING.md`](docs/TESTING.md)
5. Submit a pull request with a clear description

## Compatibility

### Tested Mods

- Enhanced Vat Learning
- RimWorld Growth Accelerator

Vat Sentinel uses conservative Harmony patching to minimize conflicts. If you encounter compatibility issues, please report them via GitHub Issues.

### Known Limitations

- Ejection requires the vat to be accessible (not blocked by walls or forbidden)
- Manual pawn removal will clear tracking records automatically
- Save game compatibility is maintained across mod versions

## License

[Specify your license here - e.g., MIT, GPL, etc.]

## Credits

- **Author**: Lucas Albers
- **RimWorld**: Developed by Ludeon Studios
- **Harmony**: Library by Andreas Pardeike

## Support

- **Issues**: Report bugs or request features via [GitHub Issues](https://github.com/LucasAlbers/VatSentinel/issues)
- **Discussions**: Join discussions on [GitHub Discussions](https://github.com/LucasAlbers/VatSentinel/discussions)

## Changelog

See [`CHANGELOG.md`](CHANGELOG.md) for version history and release notes.
