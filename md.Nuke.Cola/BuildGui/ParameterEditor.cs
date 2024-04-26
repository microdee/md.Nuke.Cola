using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ImGuiNET;
using Nuke.Common.Utilities;

namespace Nuke.Cola.BuildGui;

public interface IParameterEditor
{
    bool Supported(MemberInfo member, Type type);
    void Draw(MemberInfo member, string name, BuildGuiContext context);

    string Result { get; }
}

public class IntParameterEditor : IParameterEditor
{
    int Value;
    int? Default;

    public bool Supported(MemberInfo member, Type type)
    {
        var clearType = type.ClearNullable();
        return clearType == typeof(int) || clearType == typeof(short) || clearType == typeof(long);
    }

    public void Draw(MemberInfo member, string name, BuildGuiContext context)
    {
        Default ??= member.GetValue<int>(context.BuildObject);
        ImGui.InputInt(name, ref Value);
    }

    public string Result => Value.ToString();
}

public class FloatParameterEditor : IParameterEditor
{
    double Value;
    double? Default;

    public bool Supported(MemberInfo member, Type type)
    {
        var clearType = type.ClearNullable();
        return clearType == typeof(float) || clearType == typeof(double) || clearType == typeof(decimal);
    }

    public void Draw(MemberInfo member, string name, BuildGuiContext context)
    {
        Default ??= member.GetValue<double>(context.BuildObject);
        ImGui.InputDouble(name, ref Value);
    }

    public string Result => Value.ToString();
}

public class StringParameterEditor : IParameterEditor
{
    string Value = "";
    string? Default;

    public bool Supported(MemberInfo member, Type type)
    {
        return type == typeof(string);
    }

    public void Draw(MemberInfo member, string name, BuildGuiContext context)
    {
        Default ??= member.GetValue<string>(context.BuildObject);
        if (Default == null)
        {
            ImGui.InputText(name, ref Value, 512);
        }
        else
        {
            ImGui.InputTextWithHint(name, Default, ref Value, 512);
        }
    }

    public string Result => string.IsNullOrWhiteSpace(Value) ? (Default ?? "") : Value;
}

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