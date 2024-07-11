using System.Reflection;
using ImGuiNET;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities;

namespace Nuke.Cola.BuildGui;

public class StringParameterEditor : IParameterEditor
{
    string Value = "";
    string? Default;
    bool IsCollection = false;
    bool Enabled = false;
    TextContextWindow TempWindow = new();

    public object Clone()
    {
        return new StringParameterEditor()
        {
            IsCollection = IsCollection
        };
    }

    public bool Supported(MemberInfo member, Type type)
    {
        if (type.IsArray)
        {
            IsCollection = true;
            return type.GetElementType() == typeof(string);
        }
        IsCollection = type.IsAssignableTo(typeof(IEnumerable<string>));
        return type == typeof(string) || IsCollection;
    }

    public void Draw(MemberInfo member, string name, BuildGuiContext context)
    {
        this.PrefixCheckBox(ref Enabled);
        if (IsCollection)
        {
            if (Default == null)
            {
                var collection = member.GetValue<IEnumerable<string>>(context.BuildObject);
                Default = collection != null ? string.Join('\n', collection) : "";
                Value = Default;
            }
            var lineCount = Math.Max(Value.AsSpan().Count('\n'), 1);
            ImGui.InputTextMultiline(
                name, ref Value, 1024 * 16,
                new(-1.0f, ImGui.GetTextLineHeight() * lineCount),
                ImGuiInputTextFlags.CallbackEdit,
                TempWindow.InputTextCallback()
            );
            TempWindow.Window(() =>
            {
                ImGui.Text("Lorem Ipsum dolor sit amet");
            });
        }
        else
        {
            Default ??= member.GetValue<string>(context.BuildObject);
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

    public string? Result
    {
        get
        {
            if (!Enabled) return null;

            var collection = Value.Split('\n').Select(s => s.Trim().DoubleQuoteIfNeeded());
            var result = string.Join(' ', collection);
            return string.IsNullOrWhiteSpace(result) ? Default : result;
        }
    }
}