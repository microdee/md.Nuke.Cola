using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ImGuiNET;
using Nuke.Common;
using Nuke.Common.Utilities;

namespace Nuke.Cola.BuildGui;

public record ParameterInfo(
    string Name,
    MemberInfo Member,
    Type RawParamType,
    Type InnerParamType,
    ParameterAttribute ParamAttr
);

public interface IParameterEditor
{
    bool Supported(ParameterInfo param);
    void Draw(ParameterInfo param, BuildGuiContext context);

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

    public static void BeginParameterRow(this object self, ref bool value, ParameterInfo param, BuildGuiContext ctx)
    {
        var initOffset = ImGui.GetCursorPos();
        var columnSize = ctx.ParameterColumnSize - initOffset.X;
        ImGui.BeginGroup();
        {
            ImGui.Checkbox(self.GuiLabel(param.Name, "prefix_enable"), ref value);
            ImGui.SetItemTooltip(param.ParamAttr.Description);
        }
        ImGui.EndGroup();

        // TODO: right column incorrect width
        ImGui.SetCursorPos(new(initOffset.X + columnSize, initOffset.Y));
        var rightColSize = ImGui.GetContentRegionAvail().X;
        ImGui.SetNextItemWidth(rightColSize);
        ImGui.BeginGroup();
    }

    public static void EndParameterRow(this object self, BuildGuiContext ctx)
    {
        ImGui.EndGroup();
        var groupRectMin = ImGui.GetItemRectMin();
        var groupRectSize = ImGui.GetItemRectSize();
        ImGui.SetCursorScreenPos(new(groupRectMin.X - BuildGuiContext.ResizeHandle - 2, groupRectMin.Y));
        ImGui.Button(self.GuiLabel(suffix: "resizer"), new(BuildGuiContext.ResizeHandle, groupRectSize.Y));
        if (ImGui.IsItemHovered())
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeEW);
        }
        if (ImGui.IsItemActive())
        {
            ctx.ParameterColumnSize += ImGui.GetIO().MouseDelta.X;
        }
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
    private static Dictionary<Type, IParameterEditor> _defaultParameterEditors = new();

    static ParameterEditor()
    {
        _parameterEditors = Assembly.GetCallingAssembly().GetTypes()
            .Where(t => t.IsAssignableTo(typeof(IParameterEditor))
                && !t.IsInterface
                && !t.IsAbstract
            )
            .ToHashSet();

        _defaultParameterEditors = _parameterEditors
            .Select(t =>
            {
                return (IParameterEditor)Activator.CreateInstance(t)!;
            })
            .ToDictionary(o => o.GetType());
    }

    public static T GetDefaultEditor<T>() where T : IParameterEditor => (T) _defaultParameterEditors[typeof(T)];

    public static T? TryGetDefaultEditor<T>() where T : class, IParameterEditor => _defaultParameterEditors[typeof(T)] as T;

    public static IParameterEditor? MakeEditor(ParameterInfo param)
    {
        foreach(var defaultEditor in _defaultParameterEditors.Values)
        {
            if (defaultEditor.Supported(param))
            {
                return Activator.CreateInstance(defaultEditor.GetType()) as IParameterEditor;
            }
        }
        return null;
    }
}