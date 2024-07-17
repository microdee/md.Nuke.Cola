using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ImGuiNET;
using Nuke.Common;
using Nuke.Common.IO;
using NativeFileDialogExtendedSharp;

namespace Nuke.Cola.BuildGui;

public class PathParameterEditor : TextInputParameterEditor
{
    bool FirstFrame = true;
    string[]? FileDropPayload;
    public override bool Supported(ParameterInfo param) =>
        param.InnerParamType == typeof(AbsolutePath)
        || param.InnerParamType == typeof(RelativePath);

    public override bool HasSuggestions => true;

    void PickFileDialog()
    {
        var result = Nfd.FileOpen(
            new NfdFilter[] { new() {Description = "Any file", Specification =  "*"}},
            NukeBuild.RootDirectory
        );

        if (result.Status == NfdStatus.Ok)
        {
            var relativePath = NukeBuild.RootDirectory
                .GetRelativePathTo(result.Path)
                .ToString();
            SetPath(relativePath);
        }
    }

    void PickFolderDialog()
    {
        var result = Nfd.PickFolder(NukeBuild.RootDirectory);

        if (result.Status == NfdStatus.Ok)
        {
            var relativePath = NukeBuild.RootDirectory
                .GetRelativePathTo(result.Path)
                .ToString();
            SetPath(relativePath);
        }
    }

    void SetPath(string payload)
    {
        if (IsCollection ?? false) TextContext.SetCurrentLine(ref Value, payload);
        else Value = payload;
    }

    void ApplyFileDrop()
    {
        if (FileDropPayload != null)
        {
            // TODO: Setting the value while editing doesn't work
            // Is it because of TextInput steal text drag-and-drop?
            if (ImGui.IsItemHovered())
            {
                var asRelative = FileDropPayload
                    .Select(f => NukeBuild.RootDirectory
                        .GetRelativePathTo(f)
                        .ToString()
                    );

                SetPath(string.Join('\n', asRelative));
            }
            FileDropPayload = null;
        }
    }

    protected override void SuggestionBody(ParameterInfo param, BuildGuiContext context)
    {
        if (ImGui.Button("Browse File")) PickFileDialog();
        if (ImGui.Button("Browse Folder")) PickFolderDialog();
    }

    public override void Draw(ParameterInfo param, BuildGuiContext context)
    {
        if (FirstFrame)
        {
            context.Window!.FileDrop += p => FileDropPayload = p;
            FirstFrame = false;
        }
        base.Draw(param, context);
    }

    protected override void PostTextInput(ParameterInfo param, BuildGuiContext context) =>
        ApplyFileDrop();
}