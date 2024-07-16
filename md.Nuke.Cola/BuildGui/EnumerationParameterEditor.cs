using System.ComponentModel;
using System.Reflection;
using ImGuiNET;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities;

namespace Nuke.Cola.BuildGui;

public class EnumerationParameterEditor : EnumLikeParameterEditor
{
    public override bool Supported(MemberInfo member)
    {
        var type = member.GetMemberType();
        var clearType = type.GetInnerType().ClearNullable();
        return clearType.IsAssignableTo(typeof(Enumeration))
            && clearType.HasCustomAttribute<TypeConverterAttribute>();
    }

    protected override string[] GetEntries(MemberInfo member, string name, BuildGuiContext context) =>
        member.GetMemberType().GetInnerType()
            .GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(f => f.GetMemberType().IsAssignableTo(member.GetMemberType().GetInnerType()))
            .Select(f => f.Name)
            .ToArray();
}