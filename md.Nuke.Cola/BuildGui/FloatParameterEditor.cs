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

    public object Clone()
    {
        return new FloatParameterEditor();
    }

    public bool Supported(MemberInfo member, Type type)
    {
        var clearType = type.ClearNullable();
        return clearType == typeof(float) || clearType == typeof(double) || clearType == typeof(decimal);
    }

    public void Draw(MemberInfo member, string name, BuildGuiContext context)
    {
        this.PrefixCheckBox(ref Enabled);
        Default ??= member.GetValue<double>(context.BuildObject);
        ImGui.InputDouble(name, ref Value);
    }

    public string? Result => Enabled ? Value.ToString() : null;
}