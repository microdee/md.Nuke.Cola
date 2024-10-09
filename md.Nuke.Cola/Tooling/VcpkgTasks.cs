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

public class VcpkgTasks
{
    public static AbsolutePath? VcpkgPathOverride { private get; set; }
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

    public static Tool Vcpkg => EnsureVcpkg.Get();
}