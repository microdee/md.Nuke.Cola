#r "nuget: Nuke.Common, 8.0.0"

using Nuke.Common;
using Serilog;

public interface ITestTargets : INukeBuild
{
    Target PluginTest => _ => _
        .Executes(() =>
        {
            Log.Information("tadaa!");
        });
}