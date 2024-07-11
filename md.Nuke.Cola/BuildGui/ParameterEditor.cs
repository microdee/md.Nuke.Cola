using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ImGuiNET;
using Nuke.Common.Utilities;

namespace Nuke.Cola.BuildGui;

public interface IParameterEditor : ICloneable
{
    bool Supported(MemberInfo member, Type type);
    void Draw(MemberInfo member, string name, BuildGuiContext context);

    string? Result { get; }
}

public static class ParameterEditor
{
    public static string GuiLabel(this object self, string? prefix = null, string? suffix = null)
    {
        var result = new string?[]
        {
            prefix, $"##{self.GetHashCode()}", suffix
        }.Where(s => s != null);
        return string.Join("", result);
    }

    public static void PrefixCheckBox(this object self, ref bool value, string name = "_enable")
    {
        ImGui.Checkbox(self.GuiLabel(suffix: name), ref value);
        ImGui.SameLine();
    }

    public static Type GetInnerType(this Type type)
    {
        if (type.IsArray)
        {
            return type.GetElementType()!;
        }
        if (type.IsCollectionLike())
        {
            return type.GetGenericArguments()[0];
        }
        return type;
    }

    private static HashSet<Type> _parameterEditors = new();
    private static List<IParameterEditor> _defaultParameterEditors = new();

    static ParameterEditor()
    {
        _parameterEditors = Assembly.GetCallingAssembly().GetTypes()
            .Where(t => t.IsAssignableTo(typeof(IParameterEditor)) && !t.IsInterface)
            .ToHashSet();

        _defaultParameterEditors = _parameterEditors.Select(t =>
        {
            return (IParameterEditor)Activator.CreateInstance(t)!;
        })
        .ToList();
    }

    public static IParameterEditor? MakeEditor(MemberInfo member)
    {
        foreach(var defaultEditor in _defaultParameterEditors)
        {
            if (defaultEditor.Supported(member, member.GetMemberType()))
            {
                return defaultEditor.Clone() as IParameterEditor;
            }
        }
        return null;
    }
}