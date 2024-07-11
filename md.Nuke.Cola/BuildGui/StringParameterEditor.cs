using System.Reflection;
using ImGuiNET;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities;

namespace Nuke.Cola.BuildGui;

public class StringParameterEditor : IParameterEditor
{
    string Value = "";
    string? Default;
    bool? IsCollection;
    bool Enabled = false;

    public bool Supported(MemberInfo member)
    {
        var type = member.GetMemberType();
        if (type.IsArray)
        {
            return type.GetElementType() == typeof(string);
        }
        return type.GetInnerType() == typeof(string);
    }

    public void Draw(MemberInfo member, string name, BuildGuiContext context)
    {
        this.BeginParameterRow(ref Enabled, name, context);

        if (IsCollection ??= member.GetMemberType().IsCollectionOrArray())
        {
            if (Default == null)
            {
                var collection = member.GetValue<IEnumerable<string>>(context.BuildObject);
                Default = collection != null ? string.Join('\n', collection) : "";
                Value = Default;
            }
            var lineCount = Value.AsSpan().Count('\n') + 1;
            var padding = ImGui.GetTextLineHeightWithSpacing() - ImGui.GetTextLineHeight(); 
            var height = ImGui.GetTextLineHeight() * lineCount + padding * 2;
            ImGui.InputTextMultiline(this.GuiLabel(suffix: "value"), ref Value, 1024 * 16, new(0.0f, height));
        }
        else
        {
            Default ??= member.GetValue<string>(context.BuildObject);
            if (Default == null)
            {
                ImGui.InputText(this.GuiLabel(suffix: "value"), ref Value, 512);
            }
            else
            {
                ImGui.InputTextWithHint(this.GuiLabel(suffix: "value"), Default, ref Value, 512);
            }
        }

        this.EndParameterRow();
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