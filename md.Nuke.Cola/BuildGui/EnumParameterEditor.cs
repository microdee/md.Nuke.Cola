using System.Numerics;
using System.Reflection;
using ImGuiNET;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities;

namespace Nuke.Cola.BuildGui;

public class EnumParameterEditor : IParameterEditor
{
    string Value = "";
    string? Default;
    bool? IsCollection;

    bool Enabled = false;

    TextContextWindow EnumSelector = new();

    public bool Supported(MemberInfo member)
    {
        var type = member.GetMemberType();
        var clearType = type.GetInnerType().ClearNullable();
        return clearType.IsEnum;
    }

    public void Draw(MemberInfo member, string name, BuildGuiContext context)
    {
        this.BeginParameterRow(ref Enabled, name, context);

        if (IsCollection ??= member.GetMemberType().IsCollectionOrArray())
        {
            if (Default == null)
            {
                if (member.GetValue(context.BuildObject) is IEnumerable<object> collection)
                {
                    Default = collection != null ? string.Join('\n', collection) : "";
                    Value = Default;
                }
            }
            var lineCount = Math.Max(Value.AsSpan().Count('\n'), 1);
            ImGui.InputTextMultiline(
                this.GuiLabel(suffix: "value"), ref Value, 1024 * 16,
                new(-1.0f, ImGui.GetTextLineHeight() * lineCount),
                ImGuiInputTextFlags.CallbackEdit,
                EnumSelector.InputTextCallback()
            );
        }
        else
        {
            Default ??= member.GetValue(context.BuildObject).ToString();
            if (Default == null)
            {
                ImGui.InputText(
                    this.GuiLabel(suffix: "value"), ref Value, 512,
                    ImGuiInputTextFlags.CallbackAlways,
                    EnumSelector.InputTextCallback()
                );
            }
            else
            {
                ImGui.InputTextWithHint(
                    this.GuiLabel(suffix: "value"), Default, ref Value, 512,
                    ImGuiInputTextFlags.CallbackAlways,
                    EnumSelector.InputTextCallback()
                );
            }
        }
        if (ImGui.IsItemActive())
        {
            EnumSelector.Window(() =>
            {
                ImGui.Text("Lorem Ipsum dolor sit amet");
            });
        }
        
        this.EndParameterRow(context);
    }

    public string? Result => Enabled ? (string.IsNullOrWhiteSpace(Value) ? Default : Value) : null;
}