// Copyright (c) Paul Harrington.  All Rights Reserved.  Licensed under the MIT License.  See LICENSE in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EditorGuidelinesTests.Harness;
using Microsoft.VisualStudio;
using WindowsInput.Native;
using Xunit;

namespace EditorGuidelinesTests
{
    [VsTestSettings(ReuseInstance = false, Version = "[15.0-)")]
    public class OpenFirstFileTests : AbstractIntegrationTest
    {
        private string _testSolutionDirectory;
        private string _testSolutionFile;
        private string _testProjectFile;
        private string _testConfigurationFile;
        private string _testSourceFile;

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            _testSolutionDirectory = Path.Combine(Path.GetTempPath(), nameof(EditorGuidelinesTests), nameof(OpenFirstFileTests), Path.GetRandomFileName());
            Directory.CreateDirectory(_testSolutionDirectory);

            var solutionGuid = Guid.NewGuid();
            var projectGuid = Guid.NewGuid();
            _testSolutionFile = Path.Combine(_testSolutionDirectory, "Test.sln");
            File.WriteAllText(_testSolutionFile, $@"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 16
VisualStudioVersion = 16.0.28729.10
MinimumVisualStudioVersion = 10.0.40219.1
Project(""{{9A19103F-16F7-4668-BE54-9A1E7A4F7556}}"") = ""Test"", ""Test.csproj"", ""{projectGuid:B}""
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{projectGuid:B}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{projectGuid:B}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{projectGuid:B}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{projectGuid:B}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
	GlobalSection(ExtensibilityGlobals) = postSolution
		SolutionGuid = {solutionGuid:B}
	EndGlobalSection
EndGlobal
");
            _testProjectFile = Path.Combine(_testSolutionDirectory, "Test.csproj");
            File.WriteAllText(_testProjectFile, @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
  </PropertyGroup>

</Project>
");
            _testConfigurationFile = Path.Combine(_testSolutionDirectory, ".editorconfig");
            File.WriteAllText(_testConfigurationFile, @"
root = true

[*]
guidelines = 120
");
            _testSourceFile = Path.Combine(_testSolutionDirectory, "Class1.cs");
            File.WriteAllText(_testSourceFile, @"class Class1 {
}
");
        }

        public override async Task DisposeAsync()
        {
            try
            {
                await TestServices.Solution.CloseSolutionAsync();
            }
            finally
            {
                await base.DisposeAsync();
            }
        }

        public override void Dispose()
        {
            try
            {
                if (Directory.Exists(_testSolutionDirectory))
                {
                    try
                    {
                        Directory.Delete(_testSolutionDirectory, recursive: true);
                    }
                    catch
                    {
                        // Ignore failures. Users can clean up the folders later since they are all grouped into
                        // %TEMP%\EditorGuidelinesTests.
                    }
                }
            }
            finally
            {
                base.Dispose();
            }
        }

        /// <summary>
        /// Verifies that opening the first file via Visual Studio APIs will not cause an exception.
        /// </summary>
        [VsFact]
        public async Task TestOpenFileViaAPI()
        {
            await TestServices.Solution.OpenSolutionAsync(_testSolutionFile);

            await TestServices.Solution.OpenFileAsync(
                projectName: Path.GetFileNameWithoutExtension(_testProjectFile),
                relativeFilePath: Path.GetFileName(_testSourceFile));

            // ➡ TODO: wait for guidelines to appear and assert they are at the correct position
        }

        /// <summary>
        /// Verifies that opening the first file via a Navigate To operation will not cause an exception.
        /// </summary>
        [VsFact]
        public async Task TestOpenFileFromNavigateTo()
        {
            await TestServices.Solution.OpenSolutionAsync(_testSolutionFile);

            await TestServices.SendInput.SendAsync(
                (VirtualKeyCode.VK_T, ShiftState.Ctrl),
                "f Class1.cs",
                VirtualKeyCode.RETURN);

            using (var cancellationTokenSource = new CancellationTokenSource(TestServices.HangMitigatingTimeout))
            {
                while (true)
                {
                    await Task.Yield();
                    cancellationTokenSource.Token.ThrowIfCancellationRequested();

                    if (await TestServices.Solution.IsDocumentOpenAsync(
                        projectName: Path.GetFileNameWithoutExtension(_testProjectFile),
                        relativeFilePath: Path.GetFileName(_testSourceFile),
                        VSConstants.LOGVIEWID.Code_guid))
                    {
                        break;
                    }
                }
            }

            // ➡ TODO: wait for guidelines to appear and assert they are at the correct position
        }
    }
}
