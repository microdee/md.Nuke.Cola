using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.PowerShell;
using static Nuke.Cola.Cola;

namespace Nuke.Cola.Tooling;

/// <summary>
/// Microsofts official command line package manager for windows.
/// </summary>
public static class WingetTasks
{
    internal static void Setup()
    {
        if (EnvironmentInfo.Platform == PlatformFamily.Windows)
        {
            var ps = """
                -Command
                {
                    $progressPreference = 'silentlyContinue';
                    Write-Information "Downloading WinGet and its dependencies...";
                    Invoke-WebRequest -Uri https://aka.ms/getwinget -OutFile Microsoft.DesktopAppInstaller_8wekyb3d8bbwe.msixbundle;
                    Invoke-WebRequest -Uri https://aka.ms/Microsoft.VCLibs.x64.14.00.Desktop.appx -OutFile Microsoft.VCLibs.x64.14.00.Desktop.appx;
                    Invoke-WebRequest -Uri https://github.com/microsoft/microsoft-ui-xaml/releases/download/v2.8.6/Microsoft.UI.Xaml.2.8.x64.appx -OutFile Microsoft.UI.Xaml.2.8.x64.appx;
                    Add-AppxPackage Microsoft.VCLibs.x64.14.00.Desktop.appx;
                    Add-AppxPackage Microsoft.UI.Xaml.2.8.x64.appx;
                    Add-AppxPackage Microsoft.DesktopAppInstaller_8wekyb3d8bbwe.msixbundle;
                }
                """.AsSingleLine();
            
            PowerShellTasks.PowerShell(
                $"{ps:nq}",
                workingDirectory: EnvironmentInfo.SpecialFolder(SpecialFolders.UserProfile)
            );

            var settingsFile = EnvironmentInfo.SpecialFolder(SpecialFolders.LocalApplicationData)
                /"Packages"/"Microsoft.DesktopAppInstaller_8wekyb3d8bbwe"/"LocalState"/"settings.json";

            settingsFile.WriteAllText(
                """
                {
                    "interactivity": {
                        "disable": true
                    }
                }
                """
            );
        }
    }

    /// <summary>
    /// Get Winget or an error if setup has failed (or if we're not running on Windows).
    /// </summary>
    public static ValueOrError<Tool> EnsureWinget => ToolCola.Use("winget", Setup);
    
    /// <summary>
    /// Get Winget. It throws an exception if setup has failed (or if we're not running on Windows).
    /// </summary>
    public static Tool Winget => EnsureWinget.Get();
}