
using System.Runtime.CompilerServices;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Cola;
using Nuke.Cola.FolderComposition;
using System;
using Nuke.Cola.BuildPlugins;
using Nuke.Common.Utilities;

[ImplicitBuildInterface]
public interface IOtherLicenseRegion : INukeBuild
{
    Target EnsureOtherLicense => _ => _
        .Executes(() =>
        {
            this.ProcessLicenseRegion(
                this.ScriptFolder(),
                new(
                    """
                    This is another very serious license for very serious people who handle things
                    with the utmost seriousness.
                    """,
                    "David Móráasz", 2025
                ),
                new() { AllowDirectory = d => !d.ToString().ContainsOrdinalIgnoreCase("ThirdParty") }
            );
        });
}