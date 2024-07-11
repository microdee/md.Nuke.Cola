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
    bool IsCollection = false;

    bool Enabled = false;

    public object Clone()
    {
        return new EnumParameterEditor()
        {
            IsCollection = IsCollection
        };
    }

    TextContextWindow EnumSelector = new();

    public bool Supported(MemberInfo member, Type type)
    {
        var clearType = type.GetInnerType().ClearNullable();
        IsCollection = type.IsCollectionLike();
        return clearType.IsEnum;
    }

    public void Draw(MemberInfo member, string name, BuildGuiContext context)
    {
        this.PrefixCheckBox(ref Enabled);
        if (IsCollection)
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
                name, ref Value, 1024 * 16,
                new(-1.0f, ImGui.GetTextLineHeight() * lineCount),
                ImGuiInputTextFlags.CallbackEdit,
                EnumSelector.InputTextCallback()
            );
            EnumSelector.Window(() =>
            {
                ImGui.Text("Lorem Ipsum dolor sit amet");
            });
        }
        else
        {
            Default ??= member.GetValue(context.BuildObject).ToString();
            if (Default == null)
            {
                ImGui.InputText(name, ref Value, 512);
            }
            else
            {
                ImGui.InputTextWithHint(name, Default, ref Value, 512);
            }
        }
    }

    public string? Result => Enabled ? (string.IsNullOrWhiteSpace(Value) ? Default : Value) : null;
}