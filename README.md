# Vat Sentinel

Vat Sentinel monitors pawn development and automatically ejects them at specified age milestones

## Overview

Automatically ejects them when they reach configurable age thresholds (childhood at 3 years, growth moment at 7 years, or adolescence at 13 years). This eliminates the need for manual monitoring and prevents pawns from remaining in vats longer than necessary. Note: RimWorld automatically ejects pawns at adulthood (age 18), so that threshold is not configurable.

### Key Features

- **Automated Age-Based Ejection**: Configurable ejection at biological age milestones (3, 7, or 13 years) corresponding to RimWorld growth stages
- **Persistent State Management**: Ejection schedules persist across game saves and are recalculated on load

## Requirements

- **RimWorld**: Version 1.6 (also compatible with 1.4 and 1.5)
- **DLC**: Biotech (required)
- **Harmony**: Automatically managed via NuGet

## Installation

### For Players

1. Download the latest release from the [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=YOUR_WORKSHOP_ID) or [GitHub releases](https://github.com/lucas-albers-lz4/vatsentinel/releases)
2. Extract to your RimWorld `Mods` folder: `%RIMWORLD_ROOT%\Mods\VatSentinel\`
3. Launch RimWorld and enable "Vat Sentinel" in the Mod Manager
4. Restart RimWorld when prompted

### For Developers

See [`docs/BUILD.md`](docs/BUILD.md) for build setup and [Development](#development) section below.

## Usage

### Configuration

Access mod settings through: **Options → Mod Settings → Vat Sentinel**

Available settings:
- **Eject when reaching childhood (age 3)**: Automatically eject pawns at 3 years of age
- **Eject at growth moment (age 7)**: Automatically eject pawns at 7 years of age (growth moment)
- **Eject when reaching adolescence (age 13)**: Automatically eject pawns at 13 years of age
- **Eject after 1 day in vat (development/testing only)**: Time-based ejection for testing (defaults to off)

Multiple thresholds can be enabled simultaneously; the mod will eject at the earliest enabled threshold. Settings changes take effect immediately and will recalculate ejection schedules for all tracked pawns.

### How It Works

1. When a pawn is placed in a growth vat, Vat Sentinel registers the pawn and records the entry time and entry age
2. The mod evaluates ejection schedules hourly (every 2,500 ticks) to check if thresholds are met
3. When a configured age threshold is reached, the mod attempts to eject the pawn
4. Success or failure notifications are displayed to the player
5. If ejection fails (e.g., vat is blocked), the mod schedules a retry
6. Settings changes take effect immediately and recalculate schedules for all tracked pawns

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

For build setup and instructions, see [`docs/BUILD.md`](docs/BUILD.md).

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

This project code standards:

- **Static Analysis**: StyleCop.Analyzers and Microsoft.CodeAnalysis.NetAnalyzers
- **Consistent Formatting**: EditorConfig for cross-IDE compatibility

See [`docs/LINTING.md`](docs/LINTING.md) for detailed information on code quality tools and practices.

### Testing

Testing procedures are documented in [`docs/TESTING.md`](docs/TESTING.md). The testing strategy includes:

- Unit testing for age calculation logic
- Regression testing across game versions

## Contributing

Contributions are welcome! Please see [`docs/CONTRIBUTING.md`](docs/CONTRIBUTING.md) for detailed guidelines on:

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

Vat Sentinel uses conservative Harmony patching to minimize conflicts. If you encounter compatibility issues, please report them via [GitHub Issues](https://github.com/lucas-albers-lz4/vatsentinel/issues).

### Known Limitations

- Ejection requires the vat to be accessible (not blocked by walls or forbidden)
- Manual pawn removal will clear tracking records automatically

## License
BSD 3-Clause  [`LICENSE`](LICENSE)


## Credits

- **Author**: Lucas Albers
- **RimWorld**: Developed by Ludeon Studios
- **Harmony**: Library by Andreas Pardeike
- **HugsLib**: Developed by UnlimitedHugs

## Support

- **Issues**: Report bugs or request features via [GitHub Issues](https://github.com/lucas-albers-lz4/vatsentinel/issues)
- **Discussions**: Join discussions on [GitHub Discussions](https://github.com/lucas-albers-lz4/vatsentinel/discussions)

## Changelog

See [`docs/CHANGELOG.md`](docs/CHANGELOG.md) for version history and release notes.
