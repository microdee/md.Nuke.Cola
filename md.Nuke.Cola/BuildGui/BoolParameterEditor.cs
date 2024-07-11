using System.Reflection;
using ImGuiNET;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities;

namespace Nuke.Cola.BuildGui;

public class BoolParameterEditor : IParameterEditor
{
    bool Value;
    bool? Default;

    public bool Supported(MemberInfo member)
    {
        var clearType = member.GetMemberType().ClearNullable();
        return clearType == typeof(bool);
    }

    public void Draw(MemberInfo member, string name, BuildGuiContext context)
    {
        Default ??= member.GetValue<bool>(context.BuildObject);
        ImGui.Checkbox(name, ref Value);
    }

    public string? Result => Value ? "" : null;
}