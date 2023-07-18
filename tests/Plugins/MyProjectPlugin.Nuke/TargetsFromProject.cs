using Nuke.Common;
using Serilog;

namespace MyProjectPlugin.Nuke;

public interface ITargetsFromProject : INukeBuild
{
    Target ProjectTest => _ => _
        .Executes(() =>
        {
            Log.Information(RootDirectory);
        });
}
