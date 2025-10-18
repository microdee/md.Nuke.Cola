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
using Nuke.Common.Utilities;
using Serilog;
using static Nuke.Cola.Cola;

namespace Nuke.Cola.Tooling;

/// <summary>
/// CMake is a versatile build tool for C and C++ (in 99% of cases)
/// </summary>
public static class CMakeTasks
{
    public const string LatestVersion = "4.1.2";
    internal static string GetArchiveName(string version = LatestVersion)
        => (plat: EnvironmentInfo.Platform, arch: RuntimeInformation.OSArchitecture) switch
        {
            (PlatformFamily.Windows, Architecture.X64) => $"cmake-{version}-windows-x86_64.zip",
            (PlatformFamily.Windows, Architecture.X86) => $"cmake-{version}-windows-i386.zip",
            (PlatformFamily.Windows, Architecture.Arm64) => $"cmake-{version}-windows-arm64.zip",
            (PlatformFamily.Linux, Architecture.X64) => $"cmake-{version}-linux-x86_64.tar.gz",
            (PlatformFamily.Linux, Architecture.Arm64) => $"cmake-{version}-linux-aarch64.tar.gz",
            (PlatformFamily.OSX, _) => $"cmake-{version}-macos-universal.tar.gz",
            var other => throw new Exception($"Trying to use CMake on an unsupported platform: {other.plat} {other.arch}")
        };

    public static AbsolutePath GetLocalCMakeBin(string version = LatestVersion)
    {
        var archiveName = GetArchiveName(version);
        var subfolderName = archiveName
            .Replace(".zip", "")
            .Replace(".tar.gz", "");
            
        var localPath = NukeBuild.TemporaryDirectory / "cmake";
        return EnvironmentInfo.Platform == PlatformFamily.OSX
            ? localPath / subfolderName
            : localPath / subfolderName / "bin";
    }

    /// <summary>
    /// Get CMake or an error if downloading it has failed.
    /// </summary>
    public static ValueOrError<Tool> TryGetCMake(string version = LatestVersion) => ErrorHandling.TryGet(() =>
    {
        var archiveName = GetArchiveName(version);
        var subfolderName = archiveName
            .Replace(".zip", "")
            .Replace(".tar.gz", "");
            
        var localPath = NukeBuild.TemporaryDirectory / "cmake";
        if (!(localPath / subfolderName).DirectoryExists())
        {
            var downloadPath = localPath / archiveName;
            Log.Information("Downloading CMake {0}", archiveName);
            HttpTasks.HttpDownloadFile(
                $"https://github.com/Kitware/CMake/releases/download/v{LatestVersion}/{archiveName}",
                downloadPath
            );
            if (archiveName.EndsWithOrdinalIgnoreCase(".zip"))
            {
                downloadPath.UnZipTo(localPath);
            }
            else if (archiveName.EndsWithOrdinalIgnoreCase(".tar.gz"))
            {
                downloadPath.UnTarGZipTo(localPath);
            }
        }
        var programPath = EnvironmentInfo.Platform switch
        {
            PlatformFamily.Windows => localPath / subfolderName / "bin" / "cmake.exe",
            PlatformFamily.Linux => localPath / subfolderName / "bin" / "cmake",
            PlatformFamily.OSX => localPath / subfolderName / "CMake.app",
            var other => throw new Exception($"Trying to use CMake on an unsupported platform: {other}")
        };
        return ToolResolver.GetTool(programPath);
    });

    public static ValueOrError<Tool> EnsureCMake => TryGetCMake();

    /// <summary>
    /// Get CMake. It throws an exception if setup has failed.
    /// </summary>
    public static Tool CMake => EnsureCMake.Get();
}