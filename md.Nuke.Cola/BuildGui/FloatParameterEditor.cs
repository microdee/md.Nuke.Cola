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

    public bool Supported(ParameterInfo param)
    {
        var clearType = param.RawParamType.ClearNullable();
        return clearType == typeof(float) || clearType == typeof(double) || clearType == typeof(decimal);
    }

    public void Draw(ParameterInfo param, BuildGuiContext context)
    {
        this.BeginParameterRow(ref Enabled, param, context);
        Default ??= param.Member.GetValue<double>(context.BuildObject);
        ImGui.InputDouble(this.GuiLabel(suffix: "value"), ref Value);
        this.EndParameterRow(context);
    }

    public string? Result => Enabled ? Value.ToString() : null;
}