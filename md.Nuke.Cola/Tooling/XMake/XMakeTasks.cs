using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Serilog;

namespace Nuke.Cola.Tooling.XMake;

/// <summary>
/// XMake is a versatile build tool for many languages https://xmake.io/#/?id=supported-languages
/// scriptable in Lua
/// </summary>
public static partial class XMakeTasks
{
    public const string LatestVersion = "3.0.7";
    internal static string GetBundleAppName(string version = LatestVersion)
        => (plat: EnvironmentInfo.Platform, arch: RuntimeInformation.OSArchitecture) switch
        {
            (PlatformFamily.Windows, Architecture.X64) => $"xmake-bundle-v{version}.win64.exe",
            (PlatformFamily.Windows, Architecture.X86) => $"xmake-bundle-v{version}.win32.exe",
            (PlatformFamily.Windows, Architecture.Arm64) => $"xmake-bundle-v{version}.arm64.exe",
            (PlatformFamily.Linux, Architecture.X64) => $"xmake-bundle-v{version}.linux.x86_64",
            (PlatformFamily.OSX, Architecture.Arm64) => $"xmake-bundle-v{version}.macos.arm64",
            (PlatformFamily.OSX, Architecture.X64) => $"xmake-bundle-v{version}.macos.x86_64",
            var other => throw new Exception($"Trying to use XMake on an unsupported platform: {other.plat} {other.arch}")
        };

    [GeneratedRegex(
        """
        not found main script\:\s(?<PATH>.*)\/core.*\.lua
        """,
        RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture
    )]
    private static partial Regex ProblematicMainXMakeFolder();

    internal static AbsolutePath? GetProblematicMainXMakeFolder(this Output line)
    {
        var text = line.Text.Replace('\\', '/').Replace("//", "/");
        var result = text.Parse(ProblematicMainXMakeFolder(), forceNullOnWhitespce: true)("PATH");
        return result.TryAsPath();
    }

    /// <summary>
    /// Get XMake or an error if downloading it has failed.
    /// </summary>
    public static ValueOrError<ToolEx> TryGetXMake(string version = LatestVersion) => ErrorHandling.TryGet(() =>
    {
        var bundleAppName = GetBundleAppName(version);
        var xmakePath = NukeBuild.TemporaryDirectory / bundleAppName;
        if (!xmakePath.FileExists())
        {
            Log.Information("Downloading XMake {0}", bundleAppName);
            HttpTasks.HttpDownloadFile(
                $"https://github.com/xmake-io/xmake/releases/download/v{LatestVersion}/{bundleAppName}",
                xmakePath
            );
        }
        return ToolExResolver.GetTool(xmakePath)
            .With(
                retry: (tool, process, attempt) =>
                {
                    if (process.ExitCode == 0 || attempt >= 2) return null;
                    foreach (var line in process.Output)
                    {
                        var mainXMakeFolder = line.GetProblematicMainXMakeFolder();
                        if (mainXMakeFolder != null)
                        {
                            Log.Information("A known issue has occured with XMake where it cannot use {0}", mainXMakeFolder);
                            Log.Information("Remove it and have another attempt {0}", attempt + 1);
                            mainXMakeFolder.Parent.DeleteDirectory();
                            return tool;
                        }
                    }
                    return null;
                }
            );
    });

    public static ValueOrError<ToolEx> EnsureXMake => TryGetXMake();

    /// <summary>
    /// Get XMake. It throws an exception if setup has failed.
    /// </summary>
    public static ToolEx XMake => EnsureXMake.Get();
}
