using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ImGuiNET;
using Nuke.Common.Utilities;

namespace Nuke.Cola.BuildGui;

public interface IParameterEditor
{
    bool Supported(MemberInfo member);
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

    public static void BeginParameterRow(this object self, ref bool value, string name, BuildGuiContext ctx)
    {
        const float resizeHandle = 6;
        var initOffset = ImGui.GetCursorPos();
        var columnSize = ctx.ParameterColumnSize - initOffset.X;
        ImGui.BeginGroup();
        {
            ImGui.Checkbox(self.GuiLabel(name, "prefix_enable"), ref value);
        }
        ImGui.EndGroup();

        ImGui.SetCursorPos(new(initOffset.X + columnSize - resizeHandle, initOffset.Y));
        ImGui.Button(self.GuiLabel(suffix: "resizer"), new(resizeHandle, 0));
        if (ImGui.IsItemHovered())
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeEW);
        }
        if (ImGui.IsItemActive())
        {
            ctx.ParameterColumnSize += ImGui.GetIO().MouseDelta.X;
        }
        ImGui.SetCursorPos(new(initOffset.X + columnSize + 1, initOffset.Y));
        var rightColSize = ImGui.GetContentRegionMax().X - columnSize;
        ImGui.SetNextItemWidth(rightColSize);
        ImGui.BeginGroup();
    }

    public static void EndParameterRow(this object self)
    {
        ImGui.EndGroup();
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

    public static bool IsCollectionOrArray(this Type type) => type.IsArray || type.IsCollectionLike();

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
            if (defaultEditor.Supported(member))
            {
                return Activator.CreateInstance(defaultEditor.GetType()) as IParameterEditor;
            }
        }
        return null;
    }
}