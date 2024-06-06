using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Nuke.Cola.BuildGui;

public interface IParameterEditor
{
    bool Supported(MemberInfo member, Type type);
    void Draw(MemberInfo member, string name, BuildGuiContext context);

    string Result { get; }
}

// TODO: rest of the types