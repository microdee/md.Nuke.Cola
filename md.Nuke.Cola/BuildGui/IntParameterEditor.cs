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

    public bool Supported(ParameterInfo param)
    {
        var clearType = param.RawParamType.ClearNullable();
        return clearType == typeof(int) || clearType == typeof(short) || clearType == typeof(long);
    }

    public void Draw(ParameterInfo param, BuildGuiContext context)
    {
        this.BeginParameterRow(ref Enabled, param, context);
        Default ??= param.Member.GetValue<int>(context.BuildObject);
        ImGui.InputInt(this.GuiLabel(suffix: "value"), ref Value);
        this.EndParameterRow(context);
    }

    public string? Result => Enabled ? Value.ToString() : null;
}