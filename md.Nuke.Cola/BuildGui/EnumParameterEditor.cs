using System.Reflection;
using ImGuiNET;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities;

namespace Nuke.Cola.BuildGui;

public class EnumParameterEditor : IParameterEditor
{
    string Value = "";
    string? Default;

    string[]? Entries = null;
    int Selected = 0;

    public bool Supported(MemberInfo member, Type type)
    {
        var clearType = type.ClearNullable();
        return clearType.IsEnum;
    }

    public void Draw(MemberInfo member, string name, BuildGuiContext context)
    {
        if (Entries == null)
        {
            var type = member.GetMemberType();
            Entries = type.GetEnumNames();
            Default = member.GetValue(context.BuildObject)?.ToString();
            if (!string.IsNullOrWhiteSpace(Default) && Entries != null)
            {
                Selected = Entries.ToList().IndexOf(Default);
            }
        }

        ImGui.Combo(name, ref Selected, Entries, Entries?.Length ?? 0);
    }

    public string Result => string.IsNullOrWhiteSpace(Value) ? (Default ?? "") : Value;
}

// TODO: rest of the types