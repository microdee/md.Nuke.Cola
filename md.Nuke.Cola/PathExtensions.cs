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
using Nuke.Common.IO;
using Serilog;

namespace Nuke.Cola;

public static class PathExtensions
{
    public static void Delete(this AbsolutePath path)
    {
        if (path.FileExists()) path.DeleteFile();
        if (path.DirectoryExists()) path.DeleteDirectory();
    }

    public static FileSystemInfo? AsInfo(this AbsolutePath path)
    {
        if (path.FileExists())
            return new FileInfo(path);
        if (path.DirectoryExists())
            return new DirectoryInfo(path);
        return null;
    }

    public static FileSystemInfo? AsLinkInfo(this AbsolutePath link)
    {
        var info = link.AsInfo();
        return info?.LinkTarget == null ? null : info;
    }

    private static FileSystemInfo LinkBoilerplate(
        AbsolutePath link, AbsolutePath real,
        Action assertRealExists,
        Func<string, string, FileSystemInfo> createLink
    ) {
        var info = link.AsLinkInfo();
        var linkTarget = info?.ResolveLinkTarget(true)?.FullName;
        if (linkTarget == real)
        {
            Log.Information("Link already exists {1} <- {0}", link, real);
            return info!;
        }
        if (linkTarget != null)
        {
            Log.Warning("Existing link points to somewhere else {1} <- {0}", link, linkTarget);
            Log.Warning("    Replacing target with {0}", real);
            link.Delete();
        }
        else Log.Information("Linking {1} <- {0}", link, real);

        assertRealExists();
        Assert.False(link.FileExists() || link.DirectoryExists());

        if (!link.Parent.DirectoryExists())
            link.Parent.CreateDirectory();
        
        return createLink(link, real);
    }

    public static FileSystemInfo LinksFile(this AbsolutePath link, AbsolutePath real)
        => LinkBoilerplate(
            link, real,
            () => Assert.FileExists(real),
            File.CreateSymbolicLink
        );

    public static FileSystemInfo LinkedByFile(this AbsolutePath real, AbsolutePath link)
        => LinksFile(link, real);

    public static FileSystemInfo LinksDirectory(this AbsolutePath link, AbsolutePath real)
        => LinkBoilerplate(
            link, real,
            () => Assert.DirectoryExists(real),
            Directory.CreateSymbolicLink
        );

    public static FileSystemInfo LinkedByDirectory(this AbsolutePath real, AbsolutePath link)
        => LinksDirectory(link, real);
    
    public static FileSystemInfo Links(this AbsolutePath link, AbsolutePath real)
    {
        Assert.True(real.FileExists() || real.DirectoryExists());
        if (real.FileExists()) return link.LinksFile(real);
        return link.LinksDirectory(real);
    }

    public static FileSystemInfo LinkedBy(this AbsolutePath real, AbsolutePath link)
        => Links(link, real);

    public static IEnumerable<AbsolutePath> SubTree(this AbsolutePath origin, Func<AbsolutePath, bool>? filter = null)
        => origin.DescendantsAndSelf(d =>
            from sd in d.GlobDirectories("*")
            where filter?.Invoke(sd) ?? true
            select sd
        );
        
    public static bool LookAroundFor(Func<string, bool> predicate, out AbsolutePath? result, Func<AbsolutePath, bool>? directoryFilter = null, AbsolutePath? rootDirectory = null)
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

        foreach(var p in rootDirectory.SubTree(directoryFilter))
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

    public static AbsolutePath? GetVersionSubfolder(this AbsolutePath root, string version)
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