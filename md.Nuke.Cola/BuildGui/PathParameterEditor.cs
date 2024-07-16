using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ImGuiNET;
using Nuke.Common;
using Nuke.Common.IO;

namespace Nuke.Cola.BuildGui;

public class PathParameterEditor : TextInputParameterEditor
{
    bool FirstFrame = true;
    string[]? FileDropPayload;
    public override bool Supported(ParameterInfo param) =>
        param.InnerParamType == typeof(AbsolutePath)
        || param.InnerParamType == typeof(RelativePath);

    public override void Draw(ParameterInfo param, BuildGuiContext context)
    {
        if (FirstFrame)
        {
            context.Window!.FileDrop += p => FileDropPayload = p;
            FirstFrame = false;
        }
        base.Draw(param, context);
    }

    protected override void PostTextInput(ParameterInfo param, BuildGuiContext context)
    {
        if (FileDropPayload != null)
        {
            // TODO: Setting the value while editing doesn't work
            // Is it because of TextInput steal text drag-and-drop?
            if (ImGui.IsItemHovered() || ImGui.IsItemActive())
            {
                var asRelative = FileDropPayload
                    .Select(f => NukeBuild.RootDirectory
                        .GetRelativePathTo(f)
                        .ToString()
                    );

                if (IsCollection ?? false)
                {
                    var payload = string.Join('\n', asRelative);
                    TextContext.SetCurrentLine(ref Value, payload);
                }
                else
                {
                    Value = asRelative.FirstOrDefault() ?? "wat";
                }
            }
            FileDropPayload = null;
        }
    }
}