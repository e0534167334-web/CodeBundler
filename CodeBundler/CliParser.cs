using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeBundler;

internal static class CliParser
{
    public static BundleOptions ParseBundleOptions(string[] args)
    {
        var values = new Dictionary<string, string?>(
            StringComparer.OrdinalIgnoreCase);

        var flags = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        var valueOptions = new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase)
        {
            ["--language"] = "language",
            ["-l"] = "language",
            ["--output"] = "output",
            ["-o"] = "output",
            ["--sort"] = "sort",
            ["-s"] = "sort",
            ["--author"] = "author",
            ["-a"] = "author"
        };

        var flagOptions = new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase)
        {
            ["--note"] = "note",
            ["-n"] = "note",
            ["--remove-empty-lines"] = "remove-empty-lines",
            ["-e"] = "remove-empty-lines"
        };

        for (var i = 0; i < args.Length; i++)
        {
            var token = args[i];

            if (flagOptions.TryGetValue(token, out var flag))
            {
                if (!flags.Add(flag))
                    throw new CliException(
                        $"האפשרות {token} הוגדרה יותר מפעם אחת.");

                continue;
            }

            if (!valueOptions.TryGetValue(token, out var key))
                throw new CliException(
                    $"אפשרות לא מוכרת: {token}. הריצי codebundler help.");

            if (i + 1 >= args.Length ||
                args[i + 1].StartsWith('-'))
            {
                throw new CliException(
                    $"חסר ערך עבור {token}.");
            }

            var value = args[++i].Trim();

            if (value.Length == 0)
                throw new CliException(
                    $"הערך של {token} אינו יכול להיות ריק.");

            if (!values.TryAdd(key, value))
                throw new CliException(
                    $"האפשרות {token} הוגדרה יותר מפעם אחת.");

        }

        if (!values.TryGetValue("language", out var languageText))
            throw new CliException(
                "האפשרות --language (-l) היא חובה.");

        string[] languages;

        if (languageText!.Equals(
                "all", StringComparison.OrdinalIgnoreCase))
        {
            languages = new[] { "all" };
        }
        else
        {
            languages = languageText
                .Split(',', StringSplitOptions.TrimEntries |
                               StringSplitOptions.RemoveEmptyEntries)
                .Select(LanguageCatalog.NormalizeLanguage)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (languages.Length == 0)
                throw new CliException(
                    "יש לציין לפחות שפה אחת או all.");
        }

        var sort = values.GetValueOrDefault("sort") ?? "name";

        sort = sort.ToLowerInvariant() switch
        {
            "name" => "name",
            "type" => "type",
            _ => throw new CliException(
                "ערך sort לא תקין. יש לבחור name או type.")
        };

        var output = values.GetValueOrDefault("output")
                     ?? "bundle.txt";

        var author = values.GetValueOrDefault("author");

        if (author is not null &&
            author.Any(char.IsControl))
        {
            throw new CliException(
                "שם היוצר מכיל תווים לא תקינים.");
        }

        return new BundleOptions(
            languages,
            output,
            flags.Contains("note"),
            sort,
            flags.Contains("remove-empty-lines"),
            author);
    }
}
