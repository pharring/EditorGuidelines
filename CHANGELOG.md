# Editor Guidelines
All notable changes will be documented in this file.

## Version [2.2.13] (January 13th 2026)
### Fixed
- Fixed issue [#144](https://github.com/pharring/EditorGuidelines/issues/144) where guideline stroke thickness parsing was culture-dependent, causing issues in locales that use comma as decimal separator (e.g., German).

## Version [2.2.12] (September 12th 2025)
### Added
- Support for Visual Studio 2026 by extending the version range from [17.0, 18.0) to [17.0, 19.0)

### Fixed
- Improved error handling for Text Editor settings collection to ensure compatibility with Visual Studio 2026

## Version [2.2.11] (January 26th 2024)
### Fixed
- Fixed issue [#122](https://github.com/pharring/EditorGuidelines/issues/122) where Editor Guidelines failed to install on Visual Studio 2022 (17.10 preview) due to a missing CodingConventions package.

## Version [2.2.10] (April 24th 2023)
### Fixed
- Fixed issue [#116](https://github.com/pharring/EditorGuidelines/issues/116) where Editor Guidelines broke the Visual Studio Spell Checker in Visual Studio 2022 (17.5.4)

## Version [2.2.9] (Dec 31st 2022)
### Added
- Support for ARM in the VS 2022 version. Issue [#106](https://github.com/pharring/EditorGuidelines/issues/106)

## Version [2.2.8] (Nov 15th 2021)
### Changed
- Removed the Preview tag from the VS 2022 version.

## Version [2.2.7] (June 21st 2021)
### Fixed
- Fixed issue [#82](https://github.com/pharring/EditorGuidelines/issues/82) where version 2.2.6 wouldn't load in VS 2015 and VS 2017.

## Version [2.2.6] (June 19th 2021)
### Removed
- Support for VS 2012 and VS 2013

## Version [2.2.5] (March 26th 2020)
###
- Last version to support VS 2012 and VS 2013

### Added
- Support for different style for each guideline specified in .editorconfig

## Version [2.2.4] (June 14th 2019)
### Changed
- Updated context menu icons. Issue [#49](https://github.com/pharring/EditorGuidelines/issues/49)

## Version [2.2.3] (April 28th 2019)
### Added
- Line width set in guidelines_style can now be non-integral (e.g. 0.5px)

### Fixed
- Fixed issue [#37](https://github.com/pharring/EditorGuidelines/issues/37) where you couldn't set guidelines via .editorconfig in HTML-based files.

## Version [2.2.2] (April 21st 2019)
### Added
- Style and color may be set via guidelines_style setting in .editorconfig

## Version [2.2.1] (April 7th 2019)
### Added
- CD release to Marketplace from Azure Dev Ops

## Version [2.2.0] (April 7th 2019)
### Added
- Support for max_line_length in .editorconfig

## Version [2.1.0] (December 12th 2018)
### Added
- .editorconfig support (VS 2017 and VS 2019 only)

## Version [2.0.4] (December 7th 2018)
### Added
- Support for Visual Studio 2019 (Dev16).

### Changed
- Thinned out telemetry events.

## Version [2.0.3] (December 26th 2017)
### Changed
- Updated icon with one from the PPT artwork team.

## Version [2.0.2] (December 24th 2017)
### Changed
- Update icon again.

## Version [2.0.1] (December 24th 2017)
### Changed
- Update icon (with permission from PPT team).

## Version [2.0.0] (December 23rd 2017)
### Changed
- MIT license.
- Open sourced on https://github.com/pharring/EditorGuidelines

## Version 1.15.61202.0  (December 2nd 2016)
### Changed
- Thinned out telemetry events.

## Version 1.15.61129.0  (November 29th 2016)
### Added
- Moved color selection into 'Fonts and Colors' section in Tools/Options.

## Version 1.15.61103.1  (November 3rd 2016)
### Added
- Usage telemetry

## Version 1.15.61102.0  (November 2nd 2016)
### Added
- Support VS "15" RC

## Version 1.11.70722.0  (July 22nd 2015)
### Added
- "Remove All Guidelines" command.
- "Edit.AddGuideline" and "Edit.RemoveGuideline" can now take a parameter when invoked from the command window indicating which column to add or remove.

### Fixed
- Fixed a bug where the guideline menu was missing from context menu in HTML files in VS 2013.

### Changed
- Updated description to indicate that VS 2015 is supported.

[2.2.13]: https://github.com/pharring/EditorGuidelines/compare/2.2.12..2.2.13
[2.2.12]: https://github.com/pharring/EditorGuidelines/compare/2.2.11..2.2.12
[2.2.11]: https://github.com/pharring/EditorGuidelines/compare/2.2.9..2.2.11
[2.2.10]: https://github.com/pharring/EditorGuidelines/compare/2.2.9..2.2.10
[2.2.9]: https://github.com/pharring/EditorGuidelines/compare/2.2.8..2.2.9
[2.2.8]: https://github.com/pharring/EditorGuidelines/compare/2.2.7..2.2.8
[2.2.7]: https://github.com/pharring/EditorGuidelines/compare/2.2.6..2.2.7
[2.2.6]: https://github.com/pharring/EditorGuidelines/compare/2.2.5..2.2.6
[2.2.5]: https://github.com/pharring/EditorGuidelines/compare/2.2.4..2.2.5
[2.2.4]: https://github.com/pharring/EditorGuidelines/compare/2.2.3..2.2.4
[2.2.3]: https://github.com/pharring/EditorGuidelines/compare/2.2.2..2.2.3
[2.2.2]: https://github.com/pharring/EditorGuidelines/compare/2.2.1..2.2.2
[2.2.1]: https://github.com/pharring/EditorGuidelines/compare/2.2.0..2.2.1
[2.2.0]: https://github.com/pharring/EditorGuidelines/compare/2.1.0..2.2.0
[2.1.0]: https://github.com/pharring/EditorGuidelines/compare/2.0.4..2.1.0
[2.0.4]: https://github.com/pharring/EditorGuidelines/compare/v2.0.3..2.0.4
[2.0.3]: https://github.com/pharring/EditorGuidelines/compare/v2.0.2..v2.0.3
[2.0.2]: https://github.com/pharring/EditorGuidelines/compare/v2.0.1..v2.0.2
[2.0.1]: https://github.com/pharring/EditorGuidelines/compare/v2.0.0..v2.0.1
[2.0.0]: https://github.com/pharring/EditorGuidelines/releases/tag/v2.0.0
