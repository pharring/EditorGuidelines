# Release Preparation Prompt for Editor Guidelines

This document provides a reusable prompt for preparing new releases of the Editor Guidelines Visual Studio extension. Use this template each time you need to prepare for a new release.

## Instructions for Use

Replace the placeholders in the prompt below with actual values:
- `{NEW_VERSION}` - The new version number (e.g., 2.2.13)
- `{CURRENT_VERSION}` - The current version number (e.g., 2.2.12)
- `{NEW_YEAR}` - The current year for copyright updates
- `{RELEASE_DATE}` - The release date in format "Month DDth YYYY"

## Release Preparation Prompt Template

```
Prepare the Editor Guidelines extension for release version {NEW_VERSION}. 

## Required Tasks

### 1. Version Updates
- Update the version number from {CURRENT_VERSION} to {NEW_VERSION} in `Directory.Build.props`
- Update the copyright year to {NEW_YEAR} in both `Directory.Build.props` and `LICENSE` files

### 2. Changelog Documentation
Add a new section to `CHANGELOG.md` for version {NEW_VERSION} with the release date {RELEASE_DATE}.

To identify changes to document:
1. Review all merged pull requests since the last release ({CURRENT_VERSION})
2. Focus on user-facing changes: new features, bug fixes, breaking changes
3. **ONLY include PRs that were actually merged** - verify merge status
4. Exclude infrastructure changes like CI updates, dependency updates, documentation improvements unless they significantly impact users
5. Organize changes into appropriate sections: Added, Changed, Fixed, Removed

### 3. Changelog Format
Use this format for the new changelog entry:

```
## Version [{NEW_VERSION}] ({RELEASE_DATE})
### Added
- [List new features and enhancements]

### Changed  
- [List breaking changes and modifications]

### Fixed
- [List bug fixes and improvements]

### Removed
- [List deprecated or removed features]
```

Also add the version comparison link at the bottom of the file:
```
[{NEW_VERSION}]: https://github.com/pharring/EditorGuidelines/compare/{CURRENT_VERSION}..{NEW_VERSION}
```

### 4. Validation Steps
- Ensure all changes listed in the changelog correspond to actually merged PRs
- Verify version numbers are consistent across all files
- Check that copyright years are updated appropriately
- Review the changelog for clarity and completeness

### 5. Common Change Categories to Look For
When reviewing merged PRs, look for these types of changes:
- **Visual Studio version support**: New VS versions, version range extensions
- **Feature additions**: New commands, settings, UI improvements
- **Bug fixes**: Crash fixes, compatibility issues, rendering problems
- **Performance improvements**: Significant optimizations
- **Breaking changes**: API changes, setting changes, compatibility breaks
- **Security fixes**: Any security-related improvements

### 6. What NOT to Include
- CI/CD pipeline updates
- Build script improvements
- Code style changes
- Internal refactoring (unless it fixes user-visible bugs)
- Documentation updates (unless they reflect feature changes)
- Dependency version updates (unless they fix user issues)

## Example Usage

To prepare for release 2.2.13:

```
Prepare the Editor Guidelines extension for release version 2.2.13.

## Required Tasks

### 1. Version Updates
- Update the version number from 2.2.12 to 2.2.13 in `Directory.Build.props`
- Update the copyright year to 2025 in both `Directory.Build.props` and `LICENSE` files

### 2. Changelog Documentation
Add a new section to `CHANGELOG.md` for version 2.2.13 with the release date March 15th 2025.

[Continue with rest of template...]
```

## Notes for Future Maintenance

- Update this template if the release process changes
- Add new file locations if version numbers are stored elsewhere
- Modify changelog sections if the format evolves
- Include additional validation steps as the project grows

## Related Files

- `Directory.Build.props` - Contains version and copyright information
- `LICENSE` - Contains copyright year
- `CHANGELOG.md` - Release notes and version history
- `.github/copilot-instructions.md` - General development guidelines
```