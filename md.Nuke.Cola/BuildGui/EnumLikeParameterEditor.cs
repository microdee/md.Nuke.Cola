using System.Numerics;
using System.Reflection;
using ImGuiNET;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities;

namespace Nuke.Cola.BuildGui;

public abstract class EnumLikeParameterEditor : TextInputParameterEditor
{
    protected string[]? EnumEntries;

    public override bool HasSuggestions => true;

    protected virtual bool Filter => (EnumEntries?.Length ?? 0) > 9;

    protected abstract string[] GetEntries(ParameterInfo param, BuildGuiContext context);

    protected override void SuggestionBody(ParameterInfo param, BuildGuiContext context)
    {
        if (EnumEntries != null)
        {
            var currentLine = Filter ? TextContext.GetCurrentLine(Value) : "";
            var entries = Filter
                ? EnumEntries.Where(e => e.ContainsOrdinalIgnoreCase(currentLine))
                : EnumEntries;
            
            foreach(var entry in entries)
            {
                if (IsCollection ?? false)
                {
                    ImGui.PushID(entry + "_add");
                    if (ImGui.Button("+", new(0, ImGui.GetTextLineHeightWithSpacing())))
                    {
                        Value = Value.Insert(
                            TextContext.GetCurrentLineRange(Value).End.GetOffset(Value.Length),
                            "\n" + entry
                        );
                    }
                    ImGui.SetItemTooltip("Add as new item");
                    ImGui.PopID();
                    ImGui.SameLine();
                }
                if (ImGui.Selectable(entry, false))
                {
                    TextContext.SetCurrentLine(ref Value, entry);
                }
            }
        }
    }

    public override void Draw(ParameterInfo param, BuildGuiContext context)
    {
        EnumEntries ??= GetEntries(param, context);
        base.Draw(param, context);
    }
}
