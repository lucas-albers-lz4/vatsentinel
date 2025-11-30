# Changelog

All notable changes to Vat Sentinel will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Comprehensive architecture documentation
- Professional-grade testing procedures
- Code quality tools (StyleCop, NetAnalyzers, EditorConfig)
- Automated linting in build process

### Changed
- Enhanced documentation for professional presentation
- Improved error handling and logging
- Refined ejection retry logic

### Fixed
- Path references in build scripts
- Documentation inconsistencies

## [0.1.0] - YYYY-MM-DD

### Added
- Initial release
- Age-based ejection at configurable thresholds (3, 7, 13 years) corresponding to RimWorld growth stages
- Persistent state management across save/load
- Harmony-based integration with RimWorld vat system
- Settings UI for threshold configuration
- Error handling with retry logic
- Comprehensive debug logging
- Note: Age 18 (adulthood) ejection is handled automatically by RimWorld and is not configurable

[Unreleased]: https://github.com/LucasAlbers/VatSentinel/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/LucasAlbers/VatSentinel/releases/tag/v0.1.0

