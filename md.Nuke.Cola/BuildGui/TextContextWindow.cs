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
    private Vector2 _textSizeUntilCaret = new();
    public unsafe ImGuiInputTextCallback InputTextCallback(Action<ImGuiInputTextCallbackDataPtr>? onCallbackData = null)
    {
        return new(dataPtr =>
        {
            ImGuiInputTextCallbackDataPtr data = dataPtr;
            var text = data.GetBuffer();
            ImGuiNative.igCalcTextSize(
                (Vector2*)Unsafe.AsPointer(ref _textSizeUntilCaret),
                dataPtr->Buf,
                dataPtr->Buf + data.BufTextLen,
                (byte)0, -1
            );
            onCallbackData?.Invoke(data);
            return 0;
        });
    }

    public void Window(Action windowBody)
    {
        var position = ImGui.GetItemRectMin();
        position.Y += _textSizeUntilCaret.Y;

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