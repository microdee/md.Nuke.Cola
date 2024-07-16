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
    public record Options(bool FollowHorizontalCaret = false);
    public class CalculatedState
    {
        public bool ShouldBeOpen = false;
        public bool IsOpen = false;
        public Vector2 TextSizeUntilCaret = new();
        public Vector2 LastLineTextSize = new();
        public int LastLineIndex = 0;
        public int CaretPos = 0;
        public int SafeCaretPos = 0;
        public bool IsNewLine = false;
    }
    public CalculatedState State = new();
    public bool ShouldBeOpen
    {
        get => State.ShouldBeOpen;
        set => State.ShouldBeOpen = value;
    }

    public unsafe ImGuiInputTextCallback InputTextCallback(Action<ImGuiInputTextCallbackDataPtr, CalculatedState>? onCallbackData = null)
    {
        return new(dataPtr =>
        {
            ImGuiInputTextCallbackDataPtr data = dataPtr;
            var text = data.GetBuffer();
            State.CaretPos = data.CursorPos;
            ImGuiNative.igCalcTextSize(
                (Vector2*)Unsafe.AsPointer(ref State.TextSizeUntilCaret),
                dataPtr->Buf,
                dataPtr->Buf + State.CaretPos,
                (byte)0, -1
            );
            State.SafeCaretPos = Math.Min(Math.Max(State.CaretPos - 1, 0), text.Length - 1);
            State.IsNewLine = !string.IsNullOrEmpty(text) && text[State.SafeCaretPos] == '\n';
            State.LastLineIndex = Math.Max(text[..State.CaretPos].LastIndexOf('\n'), 0);
            
            ImGuiNative.igCalcTextSize(
                (Vector2*)Unsafe.AsPointer(ref State.LastLineTextSize),
                dataPtr->Buf + State.LastLineIndex,
                dataPtr->Buf + State.CaretPos,
                (byte)0, -1
            );

            var lastLine = text[State.LastLineIndex..State.CaretPos];
            
            onCallbackData?.Invoke(data, State);
            return 0;
        });
    }

    public void Window(Action windowBody, Options? options = null)
    {
        options ??= new();

        var position = ImGui.GetItemRectMin();
        position.Y += State.TextSizeUntilCaret.Y + ImGui.GetTextLineHeight();
        position.Y += State.IsNewLine ? ImGui.GetTextLineHeight() : 0;
        if (options.FollowHorizontalCaret)
        {
            position.X += State.LastLineTextSize.X;
        }

        if (State.ShouldBeOpen && !State.IsOpen)
        {
            State.IsOpen = true;
        }

        if (State.IsOpen)
        {
            ImGui.SetNextWindowPos(position);
            if (ImGui.Begin(this.GuiLabel(suffix: "window"), ref State.IsOpen,
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
                if (!State.ShouldBeOpen && State.IsOpen && !(ImGui.IsWindowFocused() || ImGui.IsWindowHovered()))
                {
                    State.IsOpen = false;
                }
            }
            ImGui.End();
        }
    }

    public Range GetCurrentLineRange(string value)
    {
        var safeLastLineIndex = string.IsNullOrEmpty(value) || State.LastLineIndex == 0
            ? 0
            : Math.Min(State.LastLineIndex + 1, value.Length - 1);
            
        var nextLineIndex = value.IndexOf('\n', safeLastLineIndex);
        nextLineIndex = nextLineIndex < 0 ? value.Length : nextLineIndex;
        return new(safeLastLineIndex, nextLineIndex);
    }

    public string GetCurrentLine(string value) => value[GetCurrentLineRange(value)];

    public void SetCurrentLine(ref string value, string input)
    {
        var currentLine = GetCurrentLineRange(value);
        if (currentLine.End.GetOffset(value.Length) >= value.Length - 1 && !string.IsNullOrEmpty(value) && value.EndsWith('\n'))
        {
            value += input;
        }
        else
        {
            value = value.InsertEdit(input, currentLine);
        }
    }
}

public static class TextContextHelpers
{
    public static string GetBuffer(this ImGuiInputTextCallbackDataPtr self)
    {
        return Marshal.PtrToStringUTF8(self.Buf, self.BufTextLen);
    }
}