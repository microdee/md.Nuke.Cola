using Nuke.Common;
using Nuke.Common.Tooling;

namespace Nuke.Cola.Tooling;

public class ToolExResolver
{
    public static ToolEx GetTool(string toolPath)
    {
        Assert.FileExists(toolPath);
        return new ToolExExecutor(toolPath).Execute;
    }

    public static ToolEx GetNuGetTool(string packageId, string packageExecutable, string? version = null, string? framework = null)
    {
        var toolPath = NuGetToolPathResolver.GetPackageExecutable(packageId, packageExecutable, version, framework);
        return GetTool(toolPath);
    }

    public static ToolEx GetNpmTool(string npmExecutable)
    {
        var toolPath = NpmToolPathResolver.GetNpmExecutable(npmExecutable);
        return GetTool(toolPath);
    }

    public static ToolEx? TryGetEnvironmentTool(string name)
    {
        var toolPath = ToolPathResolver.TryGetEnvironmentExecutable($"{name.ToUpperInvariant()}_EXE");
        if (toolPath == null)
            return null;

        return GetTool(toolPath);
    }

    public static ToolEx GetPathTool(string name)
    {
        var toolPath = ToolPathResolver.GetPathExecutable(name);
        return GetTool(toolPath);
    }

    public static ToolEx GetEnvironmentOrPathTool(string name)
    {
        return TryGetEnvironmentTool(name) ?? GetPathTool(name);
    }
}