
using System.Runtime.CompilerServices;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Cola;
using Nuke.Cola.FolderComposition;
using System;
using Nuke.Cola.BuildPlugins;
using Nuke.Common.Utilities;

[ImplicitBuildInterface]
public interface IMainLicenseRegion : INukeBuild
{
    Target EnsureMainLicense => _ => _
        .DependsOn<IOtherLicenseRegion>()
        .Executes(() =>
        {
            this.ProcessLicenseRegion(
                this.ScriptFolder(),
                new(
                    """
                    This Source Code Form is subject to the terms of the Mozilla Public License, v2.0.
                    If a copy of the MPL was not distributed with this file You can obtain one at
                    https://mozilla.org/MPL/2.0/
                    """,
                    "David Móráasz", 2025
                ),
                new() { AllowDirectory = d => !d.ToString().ContainsOrdinalIgnoreCase("ThirdParty") }
            );
        });
}