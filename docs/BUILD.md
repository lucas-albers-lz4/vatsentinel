# Build Documentation

This document covers the build requirements, dependency management, and build process for Vat Sentinel.

## Prerequisites

### Development Tools (Chocolatey)

The following tools are installed via Chocolatey to set up the development environment:

- **.NET Framework 4.7.2 Developer Pack** (`netfx-4.7.2-devpack`): Required for compiling against .NET Framework 4.7.2
- **Visual Studio 2022 Build Tools** (`visualstudio2022buildtools`): Provides MSBuild and compilation tools

**Installation**:
```powershell
choco install netfx-4.7.2-devpack visualstudio2022buildtools -y
```

These are one-time setup requirements for the development environment. Chocolatey is used for system-level development tools, not project dependencies.

### Project Dependencies (NuGet)

Project dependencies are managed via NuGet and are automatically restored during build:

- **Lib.Harmony** (v2.3.6): Runtime patching framework for RimWorld mods
  - Managed via `<PackageReference>` in `VatSentinel.csproj`
  - Automatically restored by MSBuild/NuGet during build
  - `ExcludeAssets="runtime"` because RimWorld provides Harmony at runtime

- **Krafs.Rimworld.Ref** (v1.6.*): RimWorld reference assemblies for compilation
  - Provides type definitions without requiring full RimWorld installation
  - Used only during build, not included in mod distribution

- **StyleCop.Analyzers** & **Microsoft.CodeAnalysis.NetAnalyzers**: Code quality analyzers
  - Run during compilation to enforce code standards
  - Not included in final mod distribution

## Build Process

### Quick Build

From the repository root, run:

```batch
build.bat
```

This script:
1. Initializes the Visual Studio build environment
2. Restores NuGet packages (including Harmony)
3. Builds the solution with code analyzers enabled
4. Outputs compiled DLL to `Assemblies/VatSentinel.dll`

### Manual Build

If you prefer to build manually:

```batch
# Initialize Visual Studio environment (required for MSBuild)
"C:\Program Files\Microsoft Visual Studio\2022\BuildTools\Common7\Tools\VsDevCmd.bat"

# Restore NuGet packages
nuget restore VatSentinel.sln

# Build the solution
msbuild VatSentinel.sln /t:Build /p:Configuration=Release /verbosity:minimal
```

### Build Output

After a successful build:
- `Assemblies/VatSentinel.dll` - Compiled mod assembly
- `Assemblies/VatSentinel.pdb` - Debug symbols (for troubleshooting)

## Dependency Management Summary

| Dependency | Management | Purpose |
|------------|-----------|---------|
| .NET Framework 4.7.2 DevPack | Chocolatey | Development environment |
| Visual Studio Build Tools | Chocolatey | Build tools (MSBuild) |
| Lib.Harmony | NuGet | Runtime patching framework |
| RimWorld References | NuGet | Compilation-time type definitions |
| Code Analyzers | NuGet | Code quality enforcement |

## Version Synchronization

The build process automatically synchronizes version numbers:
- Version is defined in `Source/VatSentinel/Properties/AssemblyInfo.cs`
- MSBuild task (`UpdateAboutXmlVersion`) extracts version and updates `About/About.xml` during build
- Both files stay in sync automatically

### Release Process - Version Increment

**Before each release**, increment the version number in `Source/VatSentinel/Properties/AssemblyInfo.cs`:
- Update all three version attributes: `AssemblyVersion`, `AssemblyFileVersion`, and `AssemblyInformationalVersion`
- Example: Change `0.0.0.11` to `0.0.0.12` (or appropriate version)
- The build process will automatically update `About/About.xml` with the new version
- Version will appear in RimWorld's mod info page and in-game logs

## Troubleshooting

### Build Fails: "MSBuild not found"
- Ensure Visual Studio Build Tools are installed via Chocolatey
- Run `VsDevCmd.bat` to initialize the build environment
- Or use the provided `build.bat` script which handles this automatically

### Build Fails: "NuGet packages not restored"
- Run `nuget restore VatSentinel.sln` manually
- Or use `build.bat` which handles package restoration

### Harmony Not Found
- Harmony is provided by RimWorld at runtime, not included in the mod
- The `ExcludeAssets="runtime"` setting is intentional
- Ensure RimWorld is installed and Harmony mod is enabled

### Version Mismatch in About.xml
- Version is automatically synced during build
- If mismatch occurs, rebuild the project
- Check that `UpdateAboutXmlVersion` target ran (check build output)

