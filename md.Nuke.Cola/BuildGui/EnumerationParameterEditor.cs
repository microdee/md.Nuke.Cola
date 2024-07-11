using System.ComponentModel;
using System.Reflection;
using ImGuiNET;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities;

namespace Nuke.Cola.BuildGui;

public class EnumerationParameterEditor : IParameterEditor
{
    string Value = "";
    string? Default;

    string[]? Entries = null;
    int Selected = 0;
    bool Enabled = false;

    public bool Supported(MemberInfo member)
    {
        return member.GetMemberType().IsAssignableTo(typeof(Enumeration));
    }

    public void Draw(MemberInfo member, string name, BuildGuiContext context)
    {
        ImGui.Text($"TODO Enumeration: {name}");
    }

    private void GenerateEntries(MemberInfo member, Type type)
    {
        var typeConverterAttr = type.GetCustomAttribute<TypeConverterAttribute>(true);
        if (typeConverterAttr == null)
        {
            Entries = Array.Empty<string>();
            return;
        }
        // Entries = type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
        //     .Select()

    }

    public string? Result => Enabled ? (string.IsNullOrWhiteSpace(Value) ? (Default ?? "") : Value) : null;
}