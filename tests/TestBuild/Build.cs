using System;
using System.Linq;
using System.Reflection;
using Nuke.Cola.BuildPlugins;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities.Collections;
using Serilog;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;

public partial class Build : NukeBuild
{
    public static int Main () => Plugins.Execute<Build>(Execute);

    protected override void OnBuildCreated() => NoLogo = true;

    public Target BuildPluginPoc => _ => _
        .Executes(() =>
        {
            Log.Information(Assembly.GetEntryAssembly().Location);
        });

}
