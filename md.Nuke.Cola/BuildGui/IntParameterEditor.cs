using System.Reflection;
using ImGuiNET;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities;

namespace Nuke.Cola.BuildGui;

public class IntParameterEditor : IParameterEditor
{
    int Value;
    int? Default;
    bool Enabled = false;

    public bool Supported(MemberInfo member)
    {
        var clearType = member.GetMemberType().ClearNullable();
        return clearType == typeof(int) || clearType == typeof(short) || clearType == typeof(long);
    }

    public void Draw(MemberInfo member, string name, BuildGuiContext context)
    {
        ImGui.Checkbox(this.GuiLabel(name), ref Enabled);
        ImGui.SameLine();
        Default ??= member.GetValue<int>(context.BuildObject);
        ImGui.InputInt(this.GuiLabel(suffix: "value"), ref Value);
    }

    public string? Result => Enabled ? Value.ToString() : null;
}