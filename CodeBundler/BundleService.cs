using System.Text;

namespace CodeBundler;

internal static class BundleService
{
    private static readonly HashSet<string> ExcludedDirectories =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "bin", "obj", "debug", "release",
            ".git", ".vs", ".idea",
            "node_modules", "packages", "publish",
            "dist", "coverage"
        };

    public static async Task<string> CreateAsync(
        BundleOptions options)
    {
        var root = Directory.GetCurrentDirectory();

        var outputPath = Path.GetFullPath(
            options.OutputPath, root);

        var outputDirectory = Path.GetDirectoryName(outputPath);

        if (string.IsNullOrWhiteSpace(outputDirectory) ||
            !Directory.Exists(outputDirectory))
        {
            throw new CliException(
                $"תיקיית היעד אינה קיימת: {outputDirectory}");
        }

        var files = EnumerateCodeFiles(root)
            .Where(path => !Path.GetFullPath(path).Equals(
                outputPath,
                StringComparison.OrdinalIgnoreCase))
            .Where(path => LanguageCatalog.IsCodeFile(
                path, options.Languages))
            .ToList();

        if (options.SortBy == "type")
        {
            files = files
                .OrderBy(
                    LanguageCatalog.GetLanguageForExtension,
                    StringComparer.OrdinalIgnoreCase)
                .ThenBy(
                    Path.GetFileName,
                    StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        else
        {
            files = files
                .OrderBy(
                    Path.GetFileName,
                    StringComparer.OrdinalIgnoreCase)
                .ThenBy(
                    path => path,
                    StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        if (files.Count == 0)
        {
            throw new CliException(
                "לא נמצאו קובצי קוד תואמים " +
                "בתיקייה הנוכחית ובתיקיות המשנה.");
        }

        var tempPath = Path.Combine(
            outputDirectory,
            $".{Path.GetFileName(outputPath)}." +
            $"{Guid.NewGuid():N}.tmp");

        try
        {
            await using (var stream = new FileStream(
                tempPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None))
            await using (var writer = new StreamWriter(
                stream, new UTF8Encoding(false)))
            {
                if (!string.IsNullOrWhiteSpace(options.Author))
                {
                    await writer.WriteLineAsync(
                        $"// Author: {options.Author}");

                    await writer.WriteLineAsync();
                }

                foreach (var file in files)
                {
                    var relativePath =
                        Path.GetRelativePath(root, file)
                            .Replace('\\', '/');

                    if (options.IncludeSourceNotes)
                    {
                        await writer.WriteLineAsync(
                            $"// Source: {relativePath}");
                    }

                    var lines = await File.ReadAllLinesAsync(file);

                    foreach (var line in lines)
                    {
                        if (options.RemoveEmptyLines &&
                            string.IsNullOrWhiteSpace(line))
                        {
                            continue;
                        }

                        await writer.WriteLineAsync(line);
                    }

                    await writer.WriteLineAsync();
                }
            }

            // מחליפים את קובץ היעד רק לאחר שהכתיבה הושלמה.
            File.Move(tempPath, outputPath, overwrite: true);
        }
        catch (UnauthorizedAccessException)
        {
            throw new CliException(
                $"אין הרשאה לכתוב לקובץ: {outputPath}");
        }
        catch (IOException ex)
        {
            throw new CliException(
                $"לא ניתן לשמור את קובץ ה-bundle: {ex.Message}");
        }
        finally
        {
            // ניקוי קובץ זמני אם הכתיבה או ההחלפה נכשלו.
            try
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
            catch (IOException)
            {
                // כישלון בניקוי לא משנה את תוצאת האריזה.
            }
            catch (UnauthorizedAccessException)
            {
                // אין הרשאה למחוק את הקובץ הזמני.
            }
        }

        return outputPath;
    }

    private static IEnumerable<string> EnumerateCodeFiles(
        string root)
    {
        var pending = new Stack<string>();
        pending.Push(root);

        while (pending.Count > 0)
        {
            var current = pending.Pop();
            string[] entries;

            try
            {
                entries = Directory
                    .EnumerateFileSystemEntries(current)
                    .ToArray();
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }
            catch (IOException)
            {
                continue;
            }

            foreach (var entry in entries)
            {
                if (Directory.Exists(entry))
                {
                    var name = Path.GetFileName(entry);

                    if (!ExcludedDirectories.Contains(name))
                        pending.Push(entry);
                }
                else
                {
                    yield return entry;
                }
            }
        }
    }
}