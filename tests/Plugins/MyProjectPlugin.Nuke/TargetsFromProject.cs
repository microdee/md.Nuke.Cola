using Nuke.Common;
using Serilog;

namespace MyProjectPlugin.Nuke;

public interface ITargetsFromProject : INukeBuild
{
    Target ProjectTest => _ => _
        .Description("This is a target testing Project plugins")
        .Executes(() =>
        {
            Log.Information(RootDirectory);
        });
}
