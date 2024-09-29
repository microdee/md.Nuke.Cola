
using System.Runtime.CompilerServices;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Cola;
using Nuke.Cola.FolderComposition;

public partial class Build
{
    Target ImportTestFolders => _ => _
        .Executes(() => 
        {
            var root = this.ScriptFolder();
            var target = root / "Target";

            this.ImportFolders("Test"
                , (root / "Unassuming", target)
                , (root / "FolderOnly_Origin", target)
                , (root / "WithManifest" / "Both_Origin", target / "WithManifest")
                , (root / "WithManifest" / "Copy_Origin", target / "WithManifest")
                , (root / "WithManifest" / "Link_Origin", target / "WithManifest")
            );
        });
}