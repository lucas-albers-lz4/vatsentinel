# Contributing to Vat Sentinel

Thank you for your interest in contributing to Vat Sentinel! This document provides guidelines and instructions for contributing to the project.

## Code of Conduct

- Be respectful and considerate of others
- Provide constructive feedback
- Focus on what is best for the community
- Show empathy towards other community members

## How to Contribute

### Reporting Bugs

Before creating a bug report, please check existing issues to avoid duplicates.

**Bug Report Template:**
- **Description**: Clear and concise description of the bug
- **Steps to Reproduce**: Detailed steps to reproduce the behavior
- **Expected Behavior**: What you expected to happen
- **Actual Behavior**: What actually happened
- **Environment**: RimWorld version, mod version, other mods installed
- **Logs**: Relevant log snippets (if applicable)
- **Screenshots**: If applicable, add screenshots to help explain the problem

### Suggesting Features

Feature suggestions are welcome! Please provide:
- **Use Case**: Why this feature would be useful
- **Proposed Solution**: How you envision the feature working
- **Alternatives**: Any alternative solutions you've considered

### Pull Requests

1. **Fork the Repository**: Create your own fork of the repository
2. **Create a Branch**: Create a feature branch from `master`
   ```batch
   git checkout -b feature/your-feature-name
   ```
3. **Make Changes**: Implement your changes following the coding standards
4. **Test Your Changes**: Ensure all tests pass and the mod works correctly
5. **Update Documentation**: Update relevant documentation for your changes
6. **Commit Changes**: Write clear, descriptive commit messages
   ```batch
   git commit -m "Add feature: brief description"
   ```
7. **Push to Your Fork**: Push your branch to your fork
   ```batch
   git push origin feature/your-feature-name
   ```
8. **Create Pull Request**: Open a pull request with a clear description

## Development Setup

### Prerequisites

- Visual Studio 2022 (or Build Tools)
- .NET Framework 4.7.2 Developer Pack
- RimWorld 1.6 (for testing)
- Git

### Setup Steps

1. Clone your fork:
   ```batch
   git clone https://github.com/YOUR_USERNAME/VatSentinel.git
   cd VatSentinel
   ```

2. Restore NuGet packages:
   ```batch
   nuget restore VatSentinel.sln
   ```

3. Build the solution:
   ```batch
   build.bat
   ```

4. Install to RimWorld for testing (see `docs/TESTING.md`)

## Coding Standards

### Code Style

- Follow the existing code style in the project
- Use 4-space indentation (configured in `.editorconfig`)
- Follow C# naming conventions (PascalCase for public members, camelCase for private)
- Use meaningful variable and method names

### Code Quality

- **Linting**: All code must pass StyleCop and NetAnalyzers checks
  ```batch
  lint.bat
  ```
- **Documentation**: Document public APIs and complex logic
- **Error Handling**: Include appropriate error handling and logging
- **Testing**: Test your changes thoroughly before submitting

### Commit Messages

Follow conventional commit message format:

```
type(scope): subject

body (optional)

footer (optional)
```

**Types:**
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `style`: Code style changes (formatting, etc.)
- `refactor`: Code refactoring
- `test`: Adding or updating tests
- `chore`: Maintenance tasks

**Examples:**
```
feat(scheduler): Add retry logic for failed ejections

fix(patches): Correct pawn registration timing issue

docs(readme): Update installation instructions
```

## Testing Requirements

Before submitting a pull request:

1. **Build Verification**: Ensure the project builds without errors
2. **Linting**: Run `lint.bat` and fix any warnings
3. **Functional Testing**: Test your changes in-game following `docs/TESTING.md`
4. **Compatibility Testing**: Verify compatibility with reference mods
5. **Regression Testing**: Ensure existing functionality still works

## Documentation

### Code Documentation

- Document public APIs with XML comments
- Add inline comments for complex logic
- Update architecture documentation for significant changes

### User Documentation

- Update `README.md` for user-facing changes
- Update `CHANGELOG.md` for all changes
- Add or update relevant sections in `docs/` directory

## Review Process

1. **Automated Checks**: Pull requests must pass automated checks (build, linting)
2. **Code Review**: At least one maintainer will review your code
3. **Testing**: Maintainers may test your changes in-game
4. **Feedback**: Address any feedback or requested changes
5. **Merge**: Once approved, your changes will be merged

## Questions?

If you have questions about contributing:
- Open a GitHub Discussion
- Create an issue with the "question" label
- Review existing documentation in the `docs/` directory

Thank you for contributing to Vat Sentinel!

