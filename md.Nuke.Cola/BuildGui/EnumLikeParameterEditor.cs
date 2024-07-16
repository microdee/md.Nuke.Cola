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

    protected abstract string[] GetEntries(ParameterInfo param, BuildGuiContext context);

    protected override void SuggestionBody()
    {
        if (EnumEntries != null)
            foreach(var entry in EnumEntries)
            {
                if (ImGui.Selectable(entry, false))
                {
                    var safeLastLineIndex = string.IsNullOrEmpty(Value) || EnumSelector.State.LastLineIndex == 0
                        ? 0
                        : Math.Min(EnumSelector.State.LastLineIndex + 1, Value.Length - 1);

                    var nextLineIndex = Value.IndexOf('\n', safeLastLineIndex);
                    if (safeLastLineIndex == Value.Length - 1 && !string.IsNullOrEmpty(Value))
                    {
                        Value += entry;
                    }
                    else
                    {
                        nextLineIndex = nextLineIndex < 0 ? Value.Length : nextLineIndex;
                        Value = Value.InsertEdit(entry, safeLastLineIndex, nextLineIndex);
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
