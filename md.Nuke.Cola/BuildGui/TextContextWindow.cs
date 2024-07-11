using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ImGuiNET;

namespace Nuke.Cola.BuildGui;

public class TextContextWindow
{
    public bool Open;
    Vector2 _textSizeUntilCaret = new();
    Vector2 _lastLineTextSize = new();
    int _caretPos = 0;
    bool _extraLine = false;
    public unsafe ImGuiInputTextCallback InputTextCallback(Action<ImGuiInputTextCallbackDataPtr>? onCallbackData = null)
    {
        return new(dataPtr =>
        {
            ImGuiInputTextCallbackDataPtr data = dataPtr;
            var text = data.GetBuffer();
            ImGuiNative.igCalcTextSize(
                (Vector2*)Unsafe.AsPointer(ref _textSizeUntilCaret),
                dataPtr->Buf,
                dataPtr->Buf + data.CursorPos,
                (byte)0, -1
            );
            _caretPos = data.CursorPos;
            var safeCaret = Math.Min(Math.Max(_caretPos - 1, 0), text.Length - 1);
            _extraLine = string.IsNullOrEmpty(text) ? false : text[safeCaret] == '\n';
            var lastLineIndex = Math.Max(text.Substring(0, _caretPos).LastIndexOf('\n'), 0);
            
            ImGuiNative.igCalcTextSize(
                (Vector2*)Unsafe.AsPointer(ref _lastLineTextSize),
                dataPtr->Buf + lastLineIndex,
                dataPtr->Buf + data.CursorPos,
                (byte)0, -1
            );

            var lastLine = text.Substring(lastLineIndex, _caretPos - lastLineIndex);
            
            onCallbackData?.Invoke(data);
            return 0;
        });
    }

    public void Window(Action windowBody)
    {
        var position = ImGui.GetItemRectMin();
        position.Y += _textSizeUntilCaret.Y + ImGui.GetTextLineHeight();
        position.Y += _extraLine ? ImGui.GetTextLineHeight() : 0;
        position.X += _lastLineTextSize.X;

        ImGui.SetNextWindowPos(position);
        if (ImGui.Begin(this.GuiLabel(suffix: "window"), ref Open,
            ImGuiWindowFlags.AlwaysAutoResize
            | ImGuiWindowFlags.NoTitleBar
            | ImGuiWindowFlags.NoResize
            | ImGuiWindowFlags.NoMove
            | ImGuiWindowFlags.NoScrollbar
            | ImGuiWindowFlags.NoCollapse
            | ImGuiWindowFlags.NoSavedSettings
            | ImGuiWindowFlags.NoFocusOnAppearing
            | ImGuiWindowFlags.NoDecoration
            | ImGuiWindowFlags.NoDocking
        )) {
            ImGui.InputFloat2("Text Size", ref _textSizeUntilCaret);
            ImGui.Text($"_caretPos: {_caretPos}");
            ImGui.Text($"_xOffset: {_lastLineTextSize.X}");
            windowBody();
        }
        ImGui.End();
    }
}

public static class TextContextHelpers
{
    public static string GetBuffer(this ImGuiInputTextCallbackDataPtr self)
    {
        return Marshal.PtrToStringUTF8(self.Buf, self.BufTextLen);
    }
}