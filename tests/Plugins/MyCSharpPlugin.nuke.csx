#r "nuget: Nuke.Common, 7.0.2"
#r "nuget: Serilog, 3.0.1"

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