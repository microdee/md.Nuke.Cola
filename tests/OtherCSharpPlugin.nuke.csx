#r "nuget: Nuke.Common, 8.0.0"
#r "nuget: Serilog, 3.0.1"

using Nuke.Common;
using Serilog;

public interface IOtherTargets : INukeBuild
{
    Target SomeOtherTarget => _ => _
        .Executes(() =>
        {
            Log.Information("foobar");
        });
}