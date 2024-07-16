using System.Reflection;
using ImGuiNET;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities;

namespace Nuke.Cola.BuildGui;

public class StringParameterEditor : TextInputParameterEditor
{
    public override bool Supported(MemberInfo member)
    {
        var type = member.GetMemberType();
        if (type.IsArray)
        {
            return type.GetElementType() == typeof(string);
        }
        return type.GetInnerType() == typeof(string);
    }
}