using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GlobExpressions;
using Nuke.Cola.Search;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Utilities;
using Nuke.Common.Utilities.Collections;
using Scriban;

namespace Nuke.Cola;

public record LicenseCommentData(string License, string Author, int Year);

public interface ILicenseCommentTemplate
{
    string LeadingCommentTemplate { get; }
    string[] FileFilters  { get; }
    string RemoveExistingComment(string fileContent);
    string TransformLicenseText(string license);
};

public abstract class DoxygenLicenseCommentTemplate : ILicenseCommentTemplate
{
    public string LeadingCommentTemplate =>
        """
        /** @noop License Comment
         *  @file
         *  @copyright
        {{ license }}
         *  
         *  @author {{ author }}
         *  @date {{ year }}
         */
        """;
    public abstract string[] FileFilters { get; }
    public string RemoveExistingComment(string fileContent)
    {
        return fileContent.SplitLineBreaks()
            .SkipWhile(l => l.StartsWithAnyOrdinalIgnoreCase(
                "/** @noop License Comment",
                " *  ",
                " */"
            ))
            .JoinNewLine();
    }

    public string TransformLicenseText(string license)
    {
        return license.SplitLineBreaks()
            .Select(l => string.IsNullOrWhiteSpace(l)
                ? " *  @copyright"
                : " *  " + l
            )
            .JoinNewLine();
    }
}

public class CppLicenseCommentTemplate : DoxygenLicenseCommentTemplate
{
    public override string[] FileFilters => [ "*.h", "*.cpp", "*.cxx", "*.hpp", "*.cppm", "*.ixx" ];
}

public class CSharpLicenseCommentTemplate : DoxygenLicenseCommentTemplate
{
    public override string[] FileFilters => [ "*.cs" ];
}

public class LicenseRegion
{
    private Dictionary<Type, ILicenseCommentTemplate> DefaultCommentTemplates = new()
    {
        [typeof(CppLicenseCommentTemplate)] = new CppLicenseCommentTemplate(),
        [typeof(CSharpLicenseCommentTemplate)] = new CSharpLicenseCommentTemplate(),
    };

    public LicenseRegion WithCommentTemplate<T>(T template) where T : ILicenseCommentTemplate
    {
        DefaultCommentTemplates[typeof(T)] = template;
        return this;
    }

    public IEnumerable<ILicenseCommentTemplate> Templates => DefaultCommentTemplates.Values;

    public Func<AbsolutePath, bool>? AllowDirectory { get; set; }
    public Func<AbsolutePath, bool>? AllowFile { get; set; }

    public string LicenseRegionFile { get; set; } = "*LicenseRegion*";
    public bool AllowDotFiles { get; set; } = false;
    public bool AllowDotDirectories { get; set; } = false;
}

public static class LicenseRegionStatic
{
    public static void ProcessLicenseRegion(this INukeBuild self, AbsolutePath root, LicenseCommentData licenseData, LicenseRegion? options = null)
    {
        options ??= new LicenseRegion();
        var fileFilters = options.Templates.SelectMany(t => t.FileFilters).ToArray();
        var files = root.GlobFiles(fileFilters)
            .Where(f => options.AllowFile?.Invoke(f) ?? true)
            .Where(f => options.AllowDotFiles || !f.Name.StartsWith('.'))
            .Where(f => !Glob.IsMatch(f, options.LicenseRegionFile, GlobOptions.CaseInsensitive));

        foreach (var file in files)
        {
            var template = options.Templates.First(t => t.FileFilters.Any(f => Glob.IsMatch(file, f, GlobOptions.CaseInsensitive)));
            var fileText = template.RemoveExistingComment(file.ReadAllText());
            var license = template.TransformLicenseText(licenseData.License);
            var commentTemplate = Template.Parse(template.LeadingCommentTemplate);
            var commentText = commentTemplate.Render(licenseData with {License = license});
            file.WriteAllText(commentText + Environment.NewLine + fileText);
        }

        root.GetDirectories()
            .Where(d => options.AllowDirectory?.Invoke(d) ?? true)
            .Where(d => options.AllowDotDirectories || !d.Name.StartsWith('.'))
            .Where(d => d.GlobFiles(options.LicenseRegionFile).IsEmpty())
            .ForEach(d => self.ProcessLicenseRegion(d, licenseData, options));
    }
}