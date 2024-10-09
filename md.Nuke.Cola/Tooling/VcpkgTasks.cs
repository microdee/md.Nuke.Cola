using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.Git;
using Serilog;

namespace Nuke.Cola.Vcpkg;

/// <summary>
/// Wrapper class for VCPKG a C++ package manager by Microsoft
/// </summary>
public class VcpkgTasks
{
    /// <summary>
    /// Set this property if your project already has an instance of VCPKG placed somewhere inside the project
    /// </summary>
    public static AbsolutePath? VcpkgPathOverride { private get; set; }

    /// <summary>
    /// Path to a place where a local VCPKG instance can be found / should be set up.
    /// </summary>
    public static AbsolutePath VcpkgPathInProject => VcpkgPathOverride ?? NukeBuild.TemporaryDirectory / "vcpkg";

    internal static void Setup()
    {
        VcpkgPathInProject.CreateOrCleanDirectory();
        GitTasks.Git($"clone --recurse-submodules --progress https://github.com/microsoft/vcpkg.git {VcpkgPathInProject}");

        var bootstrapPath = EnvironmentInfo.Platform == PlatformFamily.Windows
            ? VcpkgPathInProject / "bootstrap-vcpkg.bat"
            : VcpkgPathInProject / "bootstrap-vcpkg.sh";

        ToolResolver.GetTool(bootstrapPath)("");
    }

    /// <summary>
    /// Get an instance of VCPKG or an error if setup has failed.
    /// </summary>
    public static ValueOrError<Tool> EnsureVcpkg { get
    {
        var vcpkgPath = EnvironmentInfo.Platform == PlatformFamily.Windows
            ? VcpkgPathInProject / "vcpkg.exe"
            : VcpkgPathInProject / "vcpkg";

        if (vcpkgPath.FileExists())
            return ToolResolver.GetTool(vcpkgPath);

        return ErrorHandling.TryGet(() => ToolResolver.GetPathTool("vcpkg"))
            .Else(() =>
            {
                Log.Warning("VCPKG was not installed or not yet setup for this project. Setting up a project specific instance");
                Setup();
                return ToolResolver.GetTool(vcpkgPath);
            })
            .Get();
    }}

    /// <summary>
    /// Get an instance of VCPKG. It throws an exception if setup has failed.
    /// </summary>
    public static Tool Vcpkg => EnsureVcpkg.Get();
}