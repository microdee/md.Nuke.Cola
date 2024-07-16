using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Nuke.Common.Utilities;

namespace Nuke.Cola.BuildGui;

public class EnumParameterEditor : EnumLikeParameterEditor
{
    public override bool Supported(ParameterInfo param) =>
        param.InnerParamType.ClearNullable().IsEnum;
    
    protected override string[] GetEntries(ParameterInfo param, BuildGuiContext context) =>
        param.InnerParamType.GetEnumNames();
}