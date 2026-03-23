using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Nuke.Cola;
using Nuke.Cola.BuildPlugins;
using Nuke.Cola.Tooling;
using Nuke.Cola.Tooling.XMake;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using Serilog;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.PathConstruction;

public class Build : NukeBuild
{
    public static int Main () => Plugins.Execute<Build>(Execute);

    protected override void OnBuildCreated() => NoLogo = true;

    [Parameter()]
    public string Block = "b1";

    public Target BuildPluginPoc => _ => _
        .Executes(() =>
        {
            Log.Information(Assembly.GetExecutingAssembly().Location);
            Log.Information("Extra args: {0}", Arguments.GetBlock(Block));
        });

    public Target TestXRepo => _ => _
        .Executes(() =>
        {
            var imguiConfig =
                """
                dx12=true
                freetype=true
                """
                .AsSingleLine(",");
            
            XRepoTasks.Install("imgui", imguiConfig)();
            var imguiInfo = XRepoTasks.Fetch("imgui", imguiConfig)()!.ParseXRepoFetch();
            Log.Information("Linkdirs: {0}", imguiInfo!.GetLibrary("imgui")!.LinkDirs);
        });

    public Target TestCMake => _ => _
        .Executes(() =>
        {
            CMakeTasks.CMake("--version");
        });

    public Target BuildTestProgram => _ => _
        .Executes(() =>
        {
            var testProgram = RootDirectory / "TestProgram";
            DotNetTasks.DotNetBuild(c => c
                .SetProjectFile(testProgram / "TestProgram.csproj")
            );
        });
    
    public Target TestToolEx => _ => _
        .DependsOn(BuildTestProgram)
        .Executes(() =>
        {
            var testProgramPath = RootDirectory / "TestProgram/bin/Debug/net9.0/TestProgram.exe";
            var testProgram = ToolExResolver.GetTool(testProgramPath);
            var testProgramVanilla = ToolResolver.GetTool(testProgramPath);

            Log.Information("Test streams");
            testProgram
                .WithInput("Hello")
                .WithInput("World")
                .WithEnvVar("FOO", "bar")
                .WithEnvVar("HELLO", "mom")
                .CloseInput()
                ("input-length");

            testProgram.WithInput(["jazz", "stuff"]).CloseInput()("repeat-line")!
                .Pipe(testProgram)("repeat-line")!
                .Pipe(testProgram)("repeat-line");

            Log.Information("Test arguments (ToolEx)");
            Log.Information("Regular");
            testProgram("foo bar");
            Log.Information("Multiline");
            testProgram(
                """
                Foo
                Bar
                Lorem
                Ipsum
                Dolor
                Sit amet
                """
            );
            var regularValue = "asdasd";
            var valueWithSpaces = "asdasd asdasd";
            var multilineValue =
                """
                Hello!
                How is it going?
                """
            ;
            Log.Information("Interpolated");
            testProgram(
                $"""
                Foo={regularValue:q}
                Bar={valueWithSpaces:q}
                Lorem={multilineValue:q}
                """
            );
            Log.Information("Test arguments (Tool)");
            Log.Information("Regular");
            testProgramVanilla("foo bar");
            Log.Information("Regular (With)");
            testProgramVanilla.With("foo bar")("");
            Log.Information("Multiline (AsSingleLine)");
            testProgramVanilla(
                """
                Foo
                Bar
                Lorem
                Ipsum
                Dolor
                Sit amet
                """.AsSingleLine()
            );
            Log.Information("Multiline (With)");
            testProgramVanilla.With(
                """
                Foo
                Bar
                Lorem
                Ipsum
                Dolor
                Sit amet
                """
            )("");
            Log.Information("Vanilla interpolation features");
            testProgramVanilla(
                $"Foo={regularValue} Bar={valueWithSpaces} Lorem={multilineValue}"
            );
            Log.Information("Nuke.Cola interpolation features");
            testProgramVanilla.With(
                $"""
                Foo={regularValue}
                Bar={valueWithSpaces:q}
                Lorem={multilineValue:q}
                """
            )("");
        });
}
