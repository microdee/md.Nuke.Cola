using System.ComponentModel;
using System.Reflection;
using ImGuiNET;
using Nuke.Common;
using Nuke.Common.Execution;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities;

namespace Nuke.Cola.BuildGui;

public class ExecutableTargetParameterEditor : EnumLikeParameterEditor
{
    public override bool Supported(ParameterInfo param)
        => param.InnerParamType == typeof(ExecutableTarget);

    protected override string[] GetEntries(ParameterInfo param, BuildGuiContext context) =>
        context.BuildObject!.GetType().GetProperties()
            .Where(p => p.PropertyType == typeof(Target))
            .Select(p => p.Name)
            .ToArray();
}