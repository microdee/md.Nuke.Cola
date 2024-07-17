using System.ComponentModel;
using System.Reflection;
using ImGuiNET;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities;

namespace Nuke.Cola.BuildGui;

public class StringParameterEditor : EnumLikeParameterEditor
{
    public override bool Supported(ParameterInfo param) =>
        param.InnerParamType.ClearNullable() == typeof(string)
        || (param.InnerParamType.HasCustomAttribute<TypeConverterAttribute>()
            && !param.InnerParamType.IsAssignableTo(typeof(Enumeration))
        );

    public override bool HasSuggestions => 
        !string.IsNullOrWhiteSpace(CachedParam?.ParamAttr.ValueProviderMember);

    protected override string[] GetEntries(ParameterInfo param, BuildGuiContext context)
    {
        if (HasSuggestions)
        {
            return NukeInternals.ParameterService.GetParameterValueSet(param.Member, context.BuildObject!)
                .Select(v => v.Text)
                .ToArray();
        }
        return Array.Empty<string>();
    }
}