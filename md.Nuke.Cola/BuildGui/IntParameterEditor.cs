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

    public object Clone()
    {
        return new IntParameterEditor();
    }

    public bool Supported(MemberInfo member, Type type)
    {
        var clearType = type.ClearNullable();
        return clearType == typeof(int) || clearType == typeof(short) || clearType == typeof(long);
    }

    public void Draw(MemberInfo member, string name, BuildGuiContext context)
    {
        this.PrefixCheckBox(ref Enabled);
        Default ??= member.GetValue<int>(context.BuildObject);
        ImGui.InputInt(name, ref Value);
    }

    public string? Result => Enabled ? Value.ToString() : null;
}