using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using Nuke.Common;
using Nuke.Common.Utilities;
using Nuke.Common.Utilities.Collections;

namespace md.Nuke.Cola;

public class PathExtensions
{
    public static IEnumerable<AbsolutePath> SubTree(this AbsolutePath origin, Func<AbsolutePath, bool> filter = null) =>
        origin.DescendantsAndSelf(d =>
            from sd in d.GlobDirectories("*")
            where filter?.Invoke(sd) ?? true
            select sd
        );
        
    public static bool LookAroundFor(Func<string, bool> predicate, out AbsolutePath result, AbsolutePath? rootDirectory = null)
    {
        result = null;
        rootDirectory ??= NukeBuild.RootDirectory;

        var parents = rootDirectory
            .DescendantsAndSelf(d => d.Parent, d => Path.GetPathRoot(d) != d );

        foreach(var p in parents)
            foreach(var f in Directory.EnumerateFiles(p))
            {
                if(predicate(f))
                {
                    result = (AbsolutePath) f;
                    return true;
                }
            }

        foreach(var p in rootDirectory.SubTreeProject())
            foreach(var f in Directory.EnumerateFiles(p))
            {
                if(predicate(f))
                {
                    result = (AbsolutePath) f;
                    return true;
                }
            }
        return false;
    }

    public static AbsolutePath GetVersionSubfolder(this AbsolutePath root, string version)
    {
        if (Path.IsPathRooted(version))
        {
            return (AbsolutePath) version;
        }
        if ((root / version).DirectoryExists())
        {
            return root / version;
        }
        version = version.Contains('.') ? version : version + ".0";
        if (Version.TryParse(version, out var semVersion))
        {
            var majorMinorPatch = $"{semVersion.Major}.{semVersion.Minor}.{semVersion.Build}".Replace("-1", "0");
            var majorMinor = $"{semVersion.Major}.{semVersion.Minor}".Replace("-1", "0");
            var candidate =
                root.GlobDirectories("*").FirstOrDefault(d => d.Name.Contains(majorMinorPatch))
                ?? root.GlobDirectories("*").FirstOrDefault(d => d.Name.Contains(majorMinor))
                ?? root.GlobDirectories("*").FirstOrDefault(d => d.Name.Contains(semVersion.Major.ToString()));
            return candidate;
        }
        return null;
    }
}