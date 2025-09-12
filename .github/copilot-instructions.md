# Editor Guidelines - Visual Studio Extension

The Editor Guidelines repository contains a Visual Studio extension that adds vertical column guides to the text editor. It is written in C# targeting .NET Framework 4.7.2 and uses the Visual Studio SDK.

Always reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.

## Working Effectively

### CRITICAL: Platform Requirements
- **This project ONLY builds on Windows** - Do not attempt to build on Linux/macOS as it requires Visual Studio SDK and Windows-specific dependencies
- Requires Windows 10/11 with Visual Studio 2019 or 2022 installed
- Requires Visual Studio Workload: "Visual Studio extension development"
- Package source https://myget.org/F/vs-editor/api/v3/index.json is required but often blocked in sandboxed environments

### Build and Test Commands
**NEVER CANCEL any build or test commands - builds can take 10+ minutes**

#### Prerequisites Setup
```bash
# Ensure .NET Core 8.0 is installed (required by CI)
dotnet --version  # Should be 8.0.x

# On Windows only - verify Visual Studio and MSBuild are available
where msbuild  # Should find MSBuild.exe
```

#### Build Process (Windows Only)
```bash
# Navigate to repository root
cd /path/to/EditorGuidelines

# Restore and build the solution - NEVER CANCEL, takes 5-10 minutes
msbuild src /t:Restore,Build /p:Configuration=Debug /p:DeployExtension=false
# OR for Release builds:
msbuild src /t:Restore,Build /p:Configuration=Release /p:DeployExtension=false
```
**TIMEOUT: Set timeout to 15+ minutes. Build process includes package restoration and compilation of multiple projects.**

#### Running Tests (Windows Only)
```bash
# Run unit tests - NEVER CANCEL, takes 2-5 minutes  
dotnet test --no-build test\ColumnGuideTests\ColumnGuideTests.csproj --configuration Debug

# Run integration tests (if available)
dotnet test --no-build test\EditorGuidelinesTests\EditorGuidelinesTests.csproj --configuration Debug
```
**TIMEOUT: Set timeout to 10+ minutes for complete test suite.**

### Linux/Non-Windows Environment Limitations
```bash
# DO NOT attempt these commands on Linux - they will fail:
# msbuild src /t:Restore,Build  # ❌ Will fail - Windows only
# dotnet restore src/           # ❌ Will fail - MyGet source blocked
# dotnet build src/             # ❌ Will fail - Visual Studio SDK required

# Instead, document limitations:
echo "Build requires Windows environment with Visual Studio SDK"
echo "Package source https://myget.org/F/vs-editor/api/v3/index.json is often blocked"
```

## Key Project Structure

### Solution Layout
```
src/
├── Editor Guidelines.sln        # Main solution file
├── ColumnGuide/                 # Shared project with core functionality
│   ├── ColumnGuide.shproj      # Shared project file
│   ├── ColumnGuide.projitems   # Shared items definition
│   └── *.cs                    # Core extension code (~1900 lines)
├── VSIX/                       # VS 2015/2017/2019 extension package
│   └── VSIX.csproj            # Targets net472
└── VSIX_Dev17/                 # VS 2022 extension package  
    └── VSIX_Dev17.csproj      # Targets net472

test/
├── ColumnGuideTests/           # Unit tests using xUnit
│   └── ColumnGuideTests.csproj
└── EditorGuidelinesTests/      # Integration tests
    └── EditorGuidelinesTests.csproj
```

### Important Files for Development
- `src/ColumnGuide/EditorGuidelinesPackage.cs` - Main package class (288 lines)
- `src/ColumnGuide/ColumnGuideAdornment.cs` - Visual rendering logic (372 lines)  
- `src/ColumnGuide/Parser.cs` - .editorconfig parsing (251 lines)
- `src/ColumnGuide/TextEditorGuidesSettings.cs` - Settings management (306 lines)
- `Directory.Build.props` - Global build properties
- `.github/workflows/CI.yml` - Build automation

## Validation and Testing

### Manual Testing (Windows Only)
After making code changes, always validate by:

1. **Build the extension**: Use MSBuild commands above
2. **Install for testing**: 
   ```bash
   # Extension will be in src/VSIX/bin/Debug/ or src/VSIX_Dev17/bin/Debug/
   # Double-click the .vsix file to install in Visual Studio
   ```
3. **Test functionality**: Open Visual Studio, create a file, right-click editor → Guidelines menu
4. **Test .editorconfig support**: Create .editorconfig with `guidelines = 80,120` and verify lines appear

### Required Validation Steps
- Always build both Debug and Release configurations
- Test both VSIX projects (VS 2015-2019 and VS 2022 versions)
- Run complete unit test suite before submitting changes
- Verify extension loads correctly in Visual Studio experimental instance

### Pre-commit Validation
```bash
# These commands must pass before committing (Windows only):
msbuild src /t:Restore,Build /p:Configuration=Debug /p:DeployExtension=false
msbuild src /t:Restore,Build /p:Configuration=Release /p:DeployExtension=false
dotnet test --no-build test\ColumnGuideTests\ColumnGuideTests.csproj --configuration Debug
```

## Common Development Tasks

### Adding New Features
1. Modify shared code in `src/ColumnGuide/`
2. Update both VSIX project manifests if needed
3. Add unit tests in `test/ColumnGuideTests/`
4. Build and test both extension versions
5. Update `CHANGELOG.md` with changes

### Debugging
- Use Visual Studio with `/rootsuffix Exp` for experimental instance
- Set `StartAction=Program` and `StartProgram=devenv.exe` in project properties
- Both VSIX projects configured for F5 debugging

### Version Management
- Version defined in `Directory.Build.props` (currently 2.2.11)
- Update version for releases in both VSIX manifests
- Follow semantic versioning guidelines

## Build Artifacts and Outputs

### Expected Build Outputs
```
src/VSIX/bin/Debug/EditorGuidelines.vsix          # VS 2015-2019 extension
src/VSIX_Dev17/bin/Debug/EditorGuidelines.vsix    # VS 2022 extension
```

### CI/CD Artifacts
- GitHub Actions uploads VSIX packages as artifacts
- Both Debug and Release configurations built
- Separate artifacts for each Visual Studio version

## Troubleshooting Common Issues

### Package Restoration Failures
```bash
# If MyGet source fails (common in sandboxed environments):
# 1. Check NuGet.Config for package sources
# 2. Verify network access to https://myget.org/F/vs-editor/api/v3/index.json
# 3. Build may require manual package cache on Windows developer machine
```

### Platform-Specific Errors
```bash
# On non-Windows systems, expect these failures:
# - "MSBuild not found" - MSBuild requires Windows/.NET Framework
# - "Unable to load service index" - Package sources blocked
# - "Visual Studio SDK not found" - Extension development requires VS SDK

# Document as expected limitations rather than trying to fix
```

### Extension Installation Issues
- Ensure Visual Studio is closed before installing .vsix
- Use `/rootsuffix Exp` for development testing
- Check Visual Studio version compatibility (2015+ for VSIX, 2022 for VSIX_Dev17)

## Repository Navigation Quick Reference

### Frequently Used Commands (Windows Only)
```bash
# Check repository status
git status

# View project structure  
ls -la src/
ls -la test/

# Check build configuration
cat Directory.Build.props
cat src/VSIX/VSIX.csproj

# View CI/CD setup
cat .github/workflows/CI.yml

# Check package dependencies
cat NuGet.Config
```

### Key Directories by Task
- **Core development**: `src/ColumnGuide/`
- **Extension packaging**: `src/VSIX/` and `src/VSIX_Dev17/`
- **Testing**: `test/ColumnGuideTests/` and `test/EditorGuidelinesTests/`
- **Documentation**: `README.md`, `CHANGELOG.md`, `marketplace/`
- **Build configuration**: `Directory.Build.props`, `.github/workflows/`

## Environment-Specific Notes

### Windows Development Environment
- Full build and test capabilities available
- Visual Studio debugging and testing supported
- Extension installation and manual testing possible

### Linux/macOS/Sandboxed Environments  
- **Build**: ❌ Not supported - Windows/Visual Studio SDK required
- **Code viewing/editing**: ✅ Supported
- **Documentation updates**: ✅ Supported  
- **Test writing**: ❌ Limited - cannot validate without build
- **Package analysis**: ❌ Limited - NuGet sources may be blocked

When working in non-Windows environments, focus on:
- Code review and analysis
- Documentation improvements
- Test case design (validation on Windows required)
- CI/CD workflow improvements