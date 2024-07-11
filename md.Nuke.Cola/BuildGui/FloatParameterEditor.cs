using System.Reflection;
using ImGuiNET;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities;

namespace Nuke.Cola.BuildGui;

public class FloatParameterEditor : IParameterEditor
{
    double Value;
    double? Default;
    bool Enabled = false;

    public bool Supported(MemberInfo member)
    {
        var clearType = member.GetMemberType().ClearNullable();
        return clearType == typeof(float) || clearType == typeof(double) || clearType == typeof(decimal);
    }

    public void Draw(MemberInfo member, string name, BuildGuiContext context)
    {
        ImGui.Checkbox(this.GuiLabel(name), ref Enabled);
        ImGui.SameLine();
        Default ??= member.GetValue<double>(context.BuildObject);
        ImGui.InputDouble(this.GuiLabel(suffix: "value"), ref Value);
    }

    public string? Result => Enabled ? Value.ToString() : null;
}