using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Nuke.Common.Utilities;

namespace Nuke.Cola.BuildGui;

public class EnumParameterEditor : EnumLikeParameterEditor
{
    public override bool Supported(MemberInfo member)
    {
        var type = member.GetMemberType();
        var clearType = type.GetInnerType().ClearNullable();
        return clearType.IsEnum;
    }
    
    protected override string[] GetEntries(MemberInfo member, string name, BuildGuiContext context) =>
        member.GetMemberType().GetInnerType().GetEnumNames();
}