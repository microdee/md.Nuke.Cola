using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Nuke.Common.Utilities;

namespace Nuke.Cola.BuildPlugins;

[AttributeUsage(AttributeTargets.Interface)]
public class ImplicitBuildInterfaceAttribute : Attribute {}

/// <summary>
/// Gather build interfaces from main assembly which should be implicitly included in the main
/// build class without that knowing about it.
/// </summary>
public class ImplicitBuildInterfacePluginProvider : IProvidePlugins
{
    public IEnumerable<IHavePlugin> GatherPlugins(BuildContext context)
    {
        var interfaces = Assembly.GetEntryAssembly()!
            .GetBuildInterfaces()
            .Where(i => i.Interface.HasCustomAttribute<ImplicitBuildInterfaceAttribute>())
            .ToList();
        
        if (interfaces.Count > 0)
            yield return new ImplicitBuildInterfacePlugin
            {
                Interfaces = interfaces
            };
    }

    public void InitializeEngine(BuildContext context) {}
}