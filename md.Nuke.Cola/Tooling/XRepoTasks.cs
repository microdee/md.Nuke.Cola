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

/// <summary>
/// XRepo is a meta package manager for C/C++ built on top of XMake
/// </summary>
public static class XRepoTasks
{
    /// <summary>
    /// Get XRepo or an error if setup has failed.
    /// </summary>
    public static ValueOrError<Tool> EnsureXRepo => ToolCola.GetPathTool("xrepo", XMakeTasks.Setup);
    
    /// <summary>
    /// Get XRepo. It throws an exception if setup has failed.
    /// </summary>
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

    /// <summary>
    /// Install a package using xrepo. Using this function also ensures setting up conan or VCPKG if
    /// they're referenced in the package specification
    /// </summary>
    /// <param name="package">
    /// package specification including third-party manager identifier (e.g.: conan::),
    /// version syntax (depending on the selected manager), and other extensions (e.g. vcpkg::boost[core])
    /// <br />
    /// See https://xmake.io/#/package/remote_package?id=install-third-party-packages
    /// See https://xrepo.xmake.io/#/?id=installation-package
    /// </param>
    /// <param name="options">
    /// Extra options provided through command line arguments before package specification.
    /// </param>
    /// <returns></returns>
    public static Tool Install(string package, string options = "")
    {
        var xrepo = XRepo.With($"install -v -y {options:nq} {package:nq}");
        EnsureSupportedPackageManagers(ref xrepo, package);
        return xrepo;
    }

    /// <summary>
    /// Fetch a package info installed via xrepo. Using this function also ensures setting up conan
    /// or VCPKG if they're referenced in the package specification
    /// </summary>
    /// <param name="package">
    /// package specification including third-party manager identifier (e.g.: conan::),
    /// version syntax (depending on the selected manager), and other extensions (e.g. vcpkg::boost[core])
    /// <br />
    /// See https://xmake.io/#/package/remote_package?id=install-third-party-packages
    /// See https://xrepo.xmake.io/#/?id=installation-package
    /// </param>
    /// <param name="options">
    /// Extra options provided through command line arguments before package specification. It is
    /// important to provide the same options here as provided in install to get accurate information
    /// from the package (like dependencies, linking methods, etc)
    /// </param>
    /// <returns></returns>
    public static Tool Info(string package, string options = "")
    {
        var xrepo = XRepo.With($"info -y {options:nq} {package:nq}");
        EnsureSupportedPackageManagers(ref xrepo, package);
        return xrepo;
    }

    /// <summary>
    /// Parse the Tool output of XRepoTasks.Info into structured data.
    /// </summary>
    public static XRepoItem ParseXRepoInfo(this IReadOnlyCollection<Output> output)
        => XRepoItem.Parse(output);
}