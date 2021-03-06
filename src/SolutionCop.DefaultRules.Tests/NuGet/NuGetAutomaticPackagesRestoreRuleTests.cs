﻿namespace SolutionCop.DefaultRules.Tests.NuGet
{
    using System.IO;
    using System.Xml.Linq;
    using ApprovalTests.Namers;
    using ApprovalTests.Reporters;
    using DefaultRules.NuGet;
    using Xunit;

    [UseReporter(typeof(DiffReporter))]
    [UseApprovalSubdirectory("ApprovedResults")]
    public class NuGetAutomaticPackagesRestoreRuleTests : ProjectRuleTest
    {
        public NuGetAutomaticPackagesRestoreRuleTests()
            : base(new NuGetAutomaticPackagesRestoreRule())
        {
        }

        [Fact]
        public void Should_pass_if_NuGet_targets_file_is_not_referenced()
        {
            var xmlConfig = XElement.Parse("<NuGetAutomaticPackagesRestore/>");
            ShouldPassNormally(xmlConfig, new FileInfo(@"..\..\Data\NuGetAutomaticPackagesRestore\NoNuGet.csproj").FullName);
        }

        [Fact]
        public void Should_pass_if_old_restore_mode_is_used_in_exception()
        {
            var xmlConfig = XElement.Parse(@"
<NuGetAutomaticPackagesRestore>
  <Exception>
    <Project>NoNuGet.csproj</Project>
  </Exception>
  <Exception>
    <Project>SomeOtherProject.csproj</Project>
  </Exception>
</NuGetAutomaticPackagesRestore>");
            ShouldPassNormally(xmlConfig, new FileInfo(@"..\..\Data\NuGetAutomaticPackagesRestore\NoNuGet.csproj").FullName);
        }

        [Fact]
        public void Should_fail_if_old_restore_mode_is_used()
        {
            var xmlConfig = XElement.Parse("<NuGetAutomaticPackagesRestore enabled=\"true\"/>");
            ShouldFailNormally(xmlConfig, new FileInfo(@"..\..\Data\NuGetAutomaticPackagesRestore\OldNuGetRestoreMode.csproj").FullName);
        }

        [Fact]
        public void Should_fail_if_exception_does_not_have_project_specified()
        {
            var xmlConfig = XElement.Parse(@"
<NuGetAutomaticPackagesRestore>
  <Exception>Some text</Exception>
</NuGetAutomaticPackagesRestore>");
            ShouldFailOnConfiguration(xmlConfig, new FileInfo(@"..\..\Data\NuGetAutomaticPackagesRestore\NoNuGet.csproj").FullName);
        }

        [Fact]
        public void Should_pass_if_rule_is_disabled()
        {
            var xmlConfig = XElement.Parse("<NuGetAutomaticPackagesRestore enabled=\"false\"/>");
            ShouldPassAsDisabled(xmlConfig, new FileInfo(@"..\..\Data\NuGetAutomaticPackagesRestore\NoNuGet.csproj").FullName);
        }
    }
}