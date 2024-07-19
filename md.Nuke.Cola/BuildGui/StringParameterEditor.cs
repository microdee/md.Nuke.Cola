using System.ComponentModel;
using System.Reflection;
using ImGuiNET;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities;

namespace Nuke.Cola.BuildGui;

public class StringParameterEditor : TextInputParameterEditor
{
    public override bool Supported(ParameterInfo param) =>
        param.InnerParamType.ClearNullable() == typeof(string)
        || (param.InnerParamType.HasCustomAttribute<TypeConverterAttribute>()
            && !param.InnerParamType.IsAssignableTo(typeof(Enumeration))
        );
}