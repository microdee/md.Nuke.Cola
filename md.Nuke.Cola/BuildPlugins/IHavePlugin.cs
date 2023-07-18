using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nuke.Common.IO;

namespace Nuke.Cola.BuildPlugins;

/// <summary>
/// Implementation of this plugin must provide one build plugin which can be
/// compiled to a .NET DLL which the intermediate script can reference.
/// Plugins must expose .NET interfaces with default implementations as build
/// components. Non default-implemented interface members will cause compile
/// errors. It may seem strange but it is one intended feature of NUKE.
/// </summary>
public interface IHavePlugin
{
    /// <summary>
    /// This should be called before attempting to gather resulting types from
    /// the plugin.
    /// </summary>
    void Compile(BuildContext context);

    /// <summary>
    /// List of build interfaces which are found in the plugin.
    /// </summary>
    IEnumerable<Importable> BuildInterfaces { get; }

    /// <summary>
    /// Original source path of the plugin (can be either a script or a project file)
    /// </summary>
    AbsolutePath SourcePath { get; }
}