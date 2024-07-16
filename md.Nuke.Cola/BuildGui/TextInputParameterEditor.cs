using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Threading.Tasks;
using ImGuiNET;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities;

namespace Nuke.Cola.BuildGui;

public abstract class TextInputParameterEditor : IParameterEditor
{
    protected string Value = "";
    protected string? Default;
    protected bool? IsCollection;   
    protected bool Enabled = false;
    
    protected TextContextWindow TextContext = new();
    public virtual bool HasSuggestions => false;

    public virtual string? Result
    {
        get
        {
            if (!Enabled) return null;

            var collection = Value.Split('\n').Select(s => s.Trim().DoubleQuoteIfNeeded());
            var result = string.Join(' ', collection);
            return string.IsNullOrWhiteSpace(result) ? Default : result;
        }
    }

    public abstract bool Supported(ParameterInfo param);

    protected virtual void InputTextCallback(ImGuiInputTextCallbackDataPtr data, TextContextWindow.CalculatedState state) {}

    protected virtual void SuggestionBody(ParameterInfo param, BuildGuiContext context) {}
    protected virtual void PostTextInput(ParameterInfo param, BuildGuiContext context) {}
    protected virtual void ExtraUI(ParameterInfo param, BuildGuiContext context) {}

    public virtual void Draw(ParameterInfo param, BuildGuiContext context)
    {
        this.BeginParameterRow(ref Enabled, param, context);

        if (IsCollection ??= param.RawParamType.IsCollectionOrArray())
        {
            if (Default == null)
            {
                if (param.Member.GetValue(context.BuildObject) is IEnumerable<object> collection)
                {
                    Default = collection != null ? string.Join('\n', collection) : "";
                    Value = Default ?? "";
                }
            }

            var size = ImGui.CalcTextSize(string.IsNullOrWhiteSpace(Value) ? "NONE" : Value);
            size.Y += Value.EndsWith('\n') ? ImGui.GetTextLineHeight() : 0;
            ImGui.InputTextMultiline(
                this.GuiLabel(suffix: "value"), ref Value, 1024 * 16,
                new(-1.0f, size.Y + 10),
                ImGuiInputTextFlags.CallbackAlways,
                TextContext.InputTextCallback(InputTextCallback)
            );
        }
        else
        {
            Default ??= param.Member.GetValue(context.BuildObject)?.ToString();
            if (Default == null)
            {
                ImGui.InputText(
                    this.GuiLabel(suffix: "value"), ref Value, 512,
                    ImGuiInputTextFlags.CallbackAlways,
                    TextContext.InputTextCallback(InputTextCallback)
                );
            }
            else
            {
                ImGui.InputTextWithHint(
                    this.GuiLabel(suffix: "value"), Default, ref Value, 512,
                    ImGuiInputTextFlags.CallbackAlways,
                    TextContext.InputTextCallback(InputTextCallback)
                );
            }
        }

        PostTextInput(param, context);

        if (HasSuggestions)
        {
            TextContext.ShouldBeOpen = ImGui.IsItemActive();
            TextContext.Window(() => SuggestionBody(param, context));
        }

        ExtraUI(param, context);
        
        this.EndParameterRow(context);
    }
}