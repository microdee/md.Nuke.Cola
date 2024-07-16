using System.Reflection;
using ImGuiNET;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities;

namespace Nuke.Cola.BuildGui;

public class BoolParameterEditor : IParameterEditor
{
    bool Value;
    bool? Default;

    public bool Supported(ParameterInfo param)
    {
        var clearType = param.RawParamType.ClearNullable();
        return clearType == typeof(bool);
    }

    public void Draw(ParameterInfo param, BuildGuiContext context)
    {
        Default ??= param.Member.GetValue<bool>(context.BuildObject);
        ImGui.Checkbox(param.Name, ref Value);
        ImGui.SetItemTooltip(param.Description);
    }

    public string? Result => Value ? "" : null;
}