using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.PowerShell;
using Serilog;
using static Nuke.Cola.Cola;

namespace Nuke.Cola.Tooling;

/// <summary>
/// XMake is a versatile build tool for many languages https://xmake.io/#/?id=supported-languages
/// scriptable in Lua
/// </summary>
public static class XMakeTasks
{
    public const string LatestVersion = "3.0.4";
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

    /// <summary>
    /// Get XMake or an error if downloading it has failed.
    /// </summary>
    public static ValueOrError<Tool> TryGetXMake(string version = LatestVersion) => ErrorHandling.TryGet(() =>
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
        return ToolResolver.GetTool(xmakePath);
    });

    public static ValueOrError<Tool> EnsureXMake => TryGetXMake();

    /// <summary>
    /// Get XMake. It throws an exception if setup has failed.
    /// </summary>
    public static Tool XMake => EnsureXMake.Get();
}