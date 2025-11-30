# Linting and Code Quality Guide

This document describes the linting and code analysis tools configured for the Vat Sentinel mod.

## Tools Configured

### 1. EditorConfig

**File**: `.editorconfig`

Ensures consistent code formatting across different editors and IDEs. Key settings:
- 4-space indentation for C# files
- UTF-8 encoding
- CRLF line endings (Windows standard)
- Trim trailing whitespace
- Insert final newline

### 2. StyleCop.Analyzers

**Package**: `StyleCop.Analyzers` (v1.2.0-beta.556)  
**Configuration**: `stylecop.json`

Enforces C# coding style conventions. Key features:
- Naming conventions
- Code layout and formatting
- Using directive ordering
- Documentation requirements (relaxed for internal/private members)

**Disabled Rules**:
- Documentation requirements for internal/private members (SA16xx rules)
- File header requirements (SA163x rules)
- Some naming rules that conflict with RimWorld conventions (e.g., underscores in identifiers)

### 3. Microsoft.CodeAnalysis.NetAnalyzers

**Package**: `Microsoft.CodeAnalysis.NetAnalyzers` (v9.0.0)  
**Configuration**: `VatSentinel.ruleset`

Provides comprehensive code quality and security analysis. Includes:
- Design rules (CA1xxx)
- Globalization rules (CA13xx)
- Interoperability rules (CA14xx)
- Naming rules (CA17xx)
- Performance rules (CA18xx)
- Reliability rules (CA20xx)
- Security rules (CA3xxx, CA5xxx)

**Disabled Rules**:
- CA1014 (CLSCompliant) - Not needed for RimWorld mods
- CA1303 (Culture-specific strings) - RimWorld handles localization differently
- CA1707 (Underscores in identifiers) - RimWorld uses underscores in naming
- CA2255 (ModuleInitializer) - May not be available in .NET Framework 4.7.2

## Using the Linters

### Build Scripts

The project includes batch scripts for building and linting:

**`build.bat`** - Standard build script (analyzers run automatically):
```batch
build.bat
```
- Runs MSBuild with analyzers enabled
- Shows errors and warnings in minimal verbosity
- Analyzers (StyleCop, NetAnalyzers) run automatically during compilation

**`lint.bat`** - Lint-only script for checking code quality:
```batch
lint.bat
```
- Runs build with normal verbosity to show all analyzer warnings
- Useful for code quality checks without full deployment build
- Explicitly enables analyzers for maximum visibility

### Visual Studio

1. Open the solution in Visual Studio
2. Build the project (`Ctrl+Shift+B`)
3. Warnings will appear in the **Error List** window (View → Error List)
4. Hover over warnings in the code editor to see details
5. Right-click warnings to see quick fixes (if available)

### Visual Studio Code

1. Install the **C#** extension (by Microsoft)
2. Install the **EditorConfig for VS Code** extension
3. Open the workspace
4. Warnings will appear in the **Problems** panel
5. The C# extension will show analyzer warnings automatically

### Command Line (Manual)

When building with MSBuild, analyzer warnings will appear in the build output:

```powershell
# Standard build (analyzers run automatically)
msbuild VatSentinel.sln /t:Build /p:Configuration=Release

# Build with detailed analyzer output
msbuild VatSentinel.sln /t:Build /p:Configuration=Release /verbosity:normal

# Explicitly enable analyzers (they're on by default)
msbuild VatSentinel.sln /t:Build /p:Configuration=Release /p:RunAnalyzersDuringBuild=true
```

## Customizing Rules

### Adjusting StyleCop Rules

Edit `stylecop.json` to modify StyleCop analyzer behavior. See the [StyleCop Analyzers documentation](https://github.com/DotNetAnalyzers/StyleCopAnalyzers) for available settings.

### Adjusting Code Analysis Rules

Edit `VatSentinel.ruleset` to enable/disable specific rules. Rule actions:
- **Error** - Treats violations as build errors
- **Warning** - Treats violations as warnings (default)
- **Info** - Treats violations as informational messages
- **Hidden** - Hides violations
- **None** - Disables the rule

### Adjusting EditorConfig

Edit `.editorconfig` to change formatting preferences. See [EditorConfig documentation](https://editorconfig.org/) for available options.

## Best Practices

1. **Fix warnings before committing** - Keep the codebase clean
2. **Don't suppress warnings without reason** - If a rule doesn't apply, disable it in the ruleset, not with `#pragma` directives
3. **Review new warnings** - When updating analyzer packages, review new warnings and decide if they should be addressed
4. **Consistent formatting** - Let EditorConfig handle formatting automatically

## RimWorld-Specific Considerations

Some standard C# conventions don't apply to RimWorld mods:

- **Underscores in identifiers** - RimWorld uses underscores (e.g., `Building_GrowthVat`), so CA1707 is disabled
- **Culture-specific strings** - RimWorld handles localization through its own system, so CA1303 is disabled
- **Documentation** - Internal implementation details don't need XML documentation, so many SA16xx rules are disabled

## Troubleshooting

### Analyzers not running

1. Ensure NuGet packages are restored: `nuget restore` or `dotnet restore`
2. Rebuild the solution
3. Check that packages are installed: Look in `obj/project.assets.json`

### Too many warnings

1. Review the ruleset and disable rules that don't apply to your workflow
2. Fix warnings incrementally - don't try to fix everything at once
3. Consider using `#pragma warning disable` for specific cases (use sparingly)

### Performance issues

If analyzers slow down your IDE:
1. Disable real-time analysis in IDE settings
2. Run analyzers only on build
3. Exclude certain files/folders from analysis (if supported by your IDE)

