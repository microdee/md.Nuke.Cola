using System.ComponentModel;
using System.Reflection;
using ImGuiNET;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities;

namespace Nuke.Cola.BuildGui;

public class EnumerationParameterEditor : EnumLikeParameterEditor
{
    public override bool Supported(ParameterInfo param)
    {
        var clearType = param.InnerParamType.ClearNullable();
        return clearType.IsAssignableTo(typeof(Enumeration))
            && clearType.HasCustomAttribute<TypeConverterAttribute>();
    }

    protected override string[] GetEntries(ParameterInfo param, BuildGuiContext context) =>
        param.InnerParamType
            .GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(f => f.GetMemberType().IsAssignableTo(param.InnerParamType))
            .Select(f => f.Name)
            .ToArray();
}