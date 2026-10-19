using System.Text;

namespace CodeBundler;

internal static class ResponseFileService
{
    public static string[] ExpandArguments(string[] args)
    {
        // מפעילים קובץ תגובה כאשר הוא הארגומנט היחיד.
        if (args.Length == 1 &&
            args[0].StartsWith('@') &&
            args[0].Length > 1)
        {
            var path = Path.GetFullPath(args[0][1..]);

            if (!File.Exists(path))
            {
                throw new CliException(
                    $"קובץ התגובה לא נמצא: {path}");
            }

            return TokenizeResponseFile(
                File.ReadAllText(path));
        }

        return args;
    }

    public static BundleOptions AskForOptions()
    {
        Console.WriteLine("יצירת קובץ תגובה לפקודת bundle");
        Console.WriteLine();

        string language;
        string[] languages;

        while (true)
        {
            language = AskRequired(
                "שפות (למשל csharp,python או all)");

            try
            {
                languages = language.Equals(
                    "all", StringComparison.OrdinalIgnoreCase)
                    ? new[] { "all" }
                    : language.Split(
                            ',',
                            StringSplitOptions.TrimEntries |
                            StringSplitOptions.RemoveEmptyEntries)
                        .Select(LanguageCatalog.NormalizeLanguage)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToArray();

                if (languages.Length == 0)
                    throw new CliException(
                        "יש לציין לפחות שפה אחת או all.");

                break;
            }
            catch (CliException ex)
            {
                Console.WriteLine($"שגיאה: {ex.Message}");
            }
        }

        var inputPath = AskRequired(
            "נתיב קלט לסריקת קבצים");

        var output = AskRequired(
            "שם קובץ הפלט או נתיב מלא");

        var note = AskYesNo(
            "להוסיף הערת מקור לכל קובץ?");

        var sort = AskChoice(
            "סדר מיון",
            ["name", "type"],
            "name");

        var removeEmpty = AskYesNo(
            "להסיר שורות ריקות?");

        var author = AskOptional(
            "שם היוצר (אפשר להשאיר ריק)");

        return new BundleOptions(
            Languages: languages,
            InputPath: inputPath,
            OutputPath: output,
            IncludeSourceNotes: note,
            SortBy: sort,
            RemoveEmptyLines: removeEmpty,
            Author: string.IsNullOrWhiteSpace(author) ? null : author
        );
    }

    public static string[] BuildResponseArguments(
        BundleOptions options)
    {
        var args = new List<string>
        {
            "bundle",
            "--language",
            string.Join(',', options.Languages),
            "--input",
            options.InputPath,
            "--output",
            options.OutputPath,
            "--sort",
            options.SortBy
        };

        if (options.IncludeSourceNotes)
            args.Add("--note");

        if (options.RemoveEmptyLines)
            args.Add("--remove-empty-lines");

        if (!string.IsNullOrWhiteSpace(options.Author))
        {
            args.Add("--author");
            args.Add(options.Author);
        }

        return args.Select(QuoteIfNeeded).ToArray();
    }

    private static string QuoteIfNeeded(string value)
    {
        if (!value.Any(char.IsWhiteSpace) &&
            !value.Contains('"'))
        {
            return value;
        }

        return "\"" +
               value.Replace("\\", "\\\\")
                    .Replace("\"", "\\\"") +
               "\"";
    }

    private static string[] TokenizeResponseFile(
        string content)
    {
        var tokens = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;
        var escaping = false;

        foreach (var ch in content)
        {
            if (escaping)
            {
                current.Append(ch);
                escaping = false;
            }
            else if (ch == '\\' && inQuotes)
            {
                escaping = true;
            }
            else if (ch == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (char.IsWhiteSpace(ch) && !inQuotes)
            {
                if (current.Length > 0)
                {
                    tokens.Add(current.ToString());
                    current.Clear();
                }
            }
            else
            {
                current.Append(ch);
            }
        }

        if (escaping)
            current.Append('\\');

        if (inQuotes)
            throw new CliException(
                "קובץ התגובה מכיל מרכאות לא סגורות.");

        if (current.Length > 0)
            tokens.Add(current.ToString());

        return tokens.ToArray();
    }

    private static string AskRequired(string question)
    {
        while (true)
        {
            Console.Write($"{question}: ");
            var answer = Console.ReadLine()?.Trim();

            if (!string.IsNullOrWhiteSpace(answer))
                return answer;

            Console.WriteLine("הערך נדרש.");
        }
    }

    private static string AskOptional(string question)
    {
        Console.Write($"{question}: ");
        return Console.ReadLine()?.Trim() ?? "";
    }

    private static bool AskYesNo(string question)
    {
        while (true)
        {
            Console.Write($"{question} (y/n): ");

            var answer = Console.ReadLine()?
                .Trim().ToLowerInvariant();

            if (answer is "y" or "yes" or "כן")
                return true;

            if (answer is "n" or "no" or "לא")
                return false;

            Console.WriteLine("נא לענות y או n.");
        }
    }

    private static string AskChoice(
        string question,
        string[] choices,
        string defaultValue)
    {
        while (true)
        {
            Console.Write(
                $"{question} ({string.Join("/", choices)}) " +
                $"[{defaultValue}]: ");

            var answer = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(answer))
                return defaultValue;

            if (choices.Contains(
                answer, StringComparer.OrdinalIgnoreCase))
            {
                return answer.ToLowerInvariant();
            }

            Console.WriteLine(
                $"ערך לא תקין. האפשרויות: " +
                $"{string.Join(", ", choices)}.");
        }
    }
}