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

    public bool Supported(MemberInfo member, Type type)
    {
        return type.IsAssignableTo(typeof(Enumeration));
    }

    public void Draw(MemberInfo member, string name, BuildGuiContext context)
    {
        if (Entries == null)
        {

        }

        ImGui.Combo(name, ref Selected, Entries, Entries?.Length ?? 0);
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

    public string Result => string.IsNullOrWhiteSpace(Value) ? (Default ?? "") : Value;
}

// TODO: rest of the types