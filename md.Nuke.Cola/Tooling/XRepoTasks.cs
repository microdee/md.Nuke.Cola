using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Nuke.Cola.Tooling;
using Nuke.Cola.Vcpkg;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities;
using Serilog;

namespace Nuke.Cola.Tooling;

public static class XRepoTasks
{
    public static ValueOrError<Tool> EnsureXRepo => ToolCola.GetPathTool("xrepo", XMakeTasks.Setup);
    public static Tool XRepo => EnsureXRepo.Get();

    private static void EnsureSupportedPackageManagers(ref Tool xrepo, string package)
    {
        if (package.Contains("vcpkg::"))
        {
            VcpkgTasks.EnsureVcpkg.Get($"VCPKG is needed for package(s) {package} but it couldn't be installed");
            if (VcpkgTasks.VcpkgPathInProject.DirectoryExists())
                xrepo = xrepo.With(
                    environmentVariables: new Dictionary<string, string>() {
                        {"VCPKG_ROOT", VcpkgTasks.VcpkgPathInProject}
                    }
                );
        }
        else if (package.Contains("conan::"))
            ToolCola.GetPathTool("conan", () => PythonTasks.Pip("install conan"))
                .Get($"Conan is needed for package(s) {package} but it couldn't be installed");
    }

    public static Tool Install(string package, string options = "")
    {
        var xrepo = XRepo.With($"install -v -y {options:nq} {package:nq}");
        EnsureSupportedPackageManagers(ref xrepo, package);
        return xrepo;
    }

    public static Tool Info(string package, string options = "")
    {
        var xrepo = XRepo.With($"info -y {options:nq} {package:nq}");
        EnsureSupportedPackageManagers(ref xrepo, package);
        return xrepo;
    }

    public static XRepoItem ParseXRepoInfo(this IReadOnlyCollection<Output> output)
        => XRepoItem.Parse(output);
}