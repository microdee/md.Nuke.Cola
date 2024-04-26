#r "nuget: Nuke.Common, 8.0.0"

using Nuke.Common;
using Serilog;

public interface ITestTargets : INukeBuild
{
    Target PluginTest => _ => _
        .Description("This is a target testing CSX plugins")
        .Executes(() =>
        {
            Log.Information("tadaa!");
        });
}