
using System.Runtime.CompilerServices;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Cola;
using Nuke.Cola.FolderComposition;
using System;
using Nuke.Cola.BuildPlugins;
using Serilog;

[ImplicitBuildInterface]
public interface IImportTestFolders : INukeBuild
{
    Target ImportTestFolders => _ => _
        .Executes(() => 
        {
            var root = this.ScriptFolder();
            var target = root / "Target";

            var result = this.ImportFolders(
                new ImportOptions(Suffixes: "Test")
                , (root / "Unassuming", target)
                , (root / "FolderOnly_Origin", target)
                , (root / "WithManifest", target)
                , (root / "WithManifest" / "Both_Origin", target / "WithManifest_Individual")
                , (root / "WithManifest" / "Copy_Origin", target / "WithManifest_Individual")
                , (root / "WithManifest" / "Link_Origin", target / "WithManifest_Individual")
                , (root / "ScriptControlled", target, new ExportManifest
                {
                    Link = {
                        new() { Directory = "Private/SharedSubfolder"},
                        new() { Directory = "Public/SharedSubfolder"},
                    },
                    Copy = {
                        new() {
                            File = "**/*_Origin.*",
                            ProcessContent = true
                        }
                    }
                }, "test-export.yml")
            );

            Log.Information("--- RESULT: ---");
            result.ForEach(r => Log.Debug("{0} -> {1} ({2})", r.From, r.To, r.Method));
        });
}