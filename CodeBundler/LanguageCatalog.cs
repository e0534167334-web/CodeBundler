namespace CodeBundler;

internal static class LanguageCatalog
{
    private static readonly Dictionary<string, string[]> Extensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["csharp"] = [".cs"],
            ["c#"] = [".cs"],
            ["python"] = [".py"],
            ["java"] = [".java"],
            ["javascript"] = [".js", ".mjs", ".cjs"],
            ["typescript"] = [".ts", ".tsx"],
            ["cpp"] = [".cpp", ".cc", ".cxx", ".hpp", ".hh", ".hxx"],
            ["c"] = [".c", ".h"],
            ["html"] = [".html", ".htm"],
            ["css"] = [".css"],
            ["sql"] = [".sql"],
            ["json"] = [".json"],
            ["xml"] = [".xml"],
            ["go"] = [".go"],
            ["rust"] = [".rs"],
            ["php"] = [".php"],
            ["ruby"] = [".rb"],
            ["swift"] = [".swift"],
            ["kotlin"] = [".kt", ".kts"],
            ["powershell"] = [".ps1"],
            ["bash"] = [".sh"]
        };

    public static string NormalizeLanguage(string value)
    {
        var key = value.Trim().ToLowerInvariant();

        if (!Extensions.ContainsKey(key))
        {
            throw new CliException(
                $"שפה לא נתמכת: '{value}'. " +
                "בדקי את שם השפה או השתמשי ב-all.");
        }

        return key;
    }

    public static bool IsCodeFile(
        string path,
        IReadOnlyList<string> languages)
    {
        var extension = Path.GetExtension(path);

        if (languages.Contains(
                "all", StringComparer.OrdinalIgnoreCase))
        {
            return Extensions.Values
                .SelectMany(items => items)
                .Contains(extension,
                    StringComparer.OrdinalIgnoreCase);
        }

        return languages
            .SelectMany(language => Extensions[language])
            .Contains(extension,
                StringComparer.OrdinalIgnoreCase);
    }

    public static string GetLanguageForExtension(string path)
    {
        var extension = Path.GetExtension(path);

        return Extensions
            .FirstOrDefault(pair =>
                pair.Value.Contains(
                    extension,
                    StringComparer.OrdinalIgnoreCase))
            .Key ?? "unknown";
    }
}