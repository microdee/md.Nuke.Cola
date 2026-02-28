using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities;

namespace Nuke.Cola.Tooling.XMake;

/// <summary>
/// XRepo is a meta package manager for C/C++ built on top of XMake
/// </summary>
public static class XRepoTasks
{
    /// <summary>
    /// Get XRepo or an error if downloading it has failed.
    /// </summary>
    public static ValueOrError<ToolEx> EnsureXRepo => XMakeTasks.EnsureXMake
        .Transform(t => t.With("lua private.xrepo"));
    
    /// <summary>
    /// Get XRepo. It throws an exception if downloading it has failed.
    /// </summary>
    public static ToolEx XRepo => EnsureXRepo.Get();

    private static void ForbidExternalPackageSources(string package)
    {
        Assert.False(package.Contains("::"), "Cannot handle packages external to xrepo, via xrepo.");
    }

    /// <summary>
    /// Install a package using xrepo. Using xrepo as a meta package manager is not supported, so it can only use
    /// its own repository of packages through Nuke.Cola.
    /// </summary>
    /// <param name="package">
    /// package specification including version syntax. See https://xrepo.xmake.io/#/?id=installation-package
    /// </param>
    /// <param name="options">
    /// Extra options configuring the package. It should be comma separated key=value pairs.
    /// </param>
    /// <param name="extraArgs">
    /// Extra arguments provided through command line before package specification.
    /// </param>
    /// <returns></returns>
    public static ToolEx Install(string package, string options = "", string extraArgs = "")
    {
        ForbidExternalPackageSources(package);
        return XRepo.With(
            $"""
            install -v -y
            {("--configs=", options):quote}
            {extraArgs}
            {package:quote}
            """
        );
    }

    /// <summary>
    /// Fetch a package info installed via xrepo. Using xrepo as a meta package manager is not supported, so it can
    /// only use its own repository of packages through Nuke.Cola.
    /// </summary>
    /// <param name="package">
    /// package specification including version syntax. See https://xrepo.xmake.io/#/?id=installation-package
    /// </param>
    /// <param name="options">
    /// Extra options configuring the package. It should be comma separated key=value pairs. It is
    /// important to provide the same options here as provided in install to get accurate information
    /// from the package (like dependencies, linking methods, etc)
    /// </param>
    /// <param name="extraArgs">
    /// Extra arguments provided through command line before package specification.
    /// </param>
    /// <returns></returns>
    public static ToolEx Fetch(string package, string options = "", string extraArgs = "")
    {
        ForbidExternalPackageSources(package);
        return XRepo.With(
            $"""
            fetch -v -y
            --deps --json
            {("--configs=", options):quote}
            {extraArgs}
            {package:quote}
            """
        );
    }
}
