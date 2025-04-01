using System;
using System.Linq;
using System.Reflection;
using Nuke.Cola;
using Nuke.Cola.BuildPlugins;
using Nuke.Cola.Tooling;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities.Collections;
using Serilog;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.PathConstruction;

public class Build : NukeBuild
{
    public static int Main () => Plugins.Execute<Build>(Execute);

    protected override void OnBuildCreated() => NoLogo = true;

    public Target BuildPluginPoc => _ => _
        .Executes(() =>
        {
            Log.Information(Assembly.GetEntryAssembly().Location);
        });

    public Target TestXRepo => _ => _
        .Executes(() =>
        {
            var imguiConfig =
                """
                dx12=true
                freetype=true
                """
                .AsSingleLine(",");
            
            XRepoTasks.Install("imgui", imguiConfig)("");
            var imguiInfo = XRepoTasks.Info("imgui", imguiConfig)("").ParseXRepoInfo();
            Log.Information("Linkdirs: {0}", imguiInfo["imgui"]?["fetchinfo"]?["linkdirs"]?.Value);
            
            XRepoTasks.Install("vcpkg::spdlog")("");
            var spdlogInfo = XRepoTasks.Info("vcpkg::spdlog")("").ParseXRepoInfo();
            Log.Information("Linkdirs: {0}", spdlogInfo["vcpkg::spdlog"]?["fetchinfo"]?["linkdirs"]?.Value);
            
            var conanSpdlogSpec = "conan::spdlog/1.14.1";
            XRepoTasks.Install(conanSpdlogSpec)("");
            var conanSpdlogInfo = XRepoTasks.Info(conanSpdlogSpec)("").ParseXRepoInfo();
            Log.Information("Linkdirs: {0}", conanSpdlogInfo[conanSpdlogSpec]?["fetchinfo"]?["linkdirs"]?.Value);
        });
}
