using System.Text;

namespace CodeBundler;

internal static class CliApplication
{
    public static async Task<int> RunAsync(
        string[] rawArgs)
    {
        try
        {
            var args =
                ResponseFileService.ExpandArguments(rawArgs);

            if (args.Length == 0 ||
                args[0] is "--help" or "-h" or "help")
            {
                PrintHelp();
                return 0;
            }

            return args[0].ToLowerInvariant() switch
            {
                "bundle" =>
                    await RunBundleAsync(args[1..]),

                "create-rsp" =>
                    CreateResponseFile(),

                _ => UnknownCommand(args[0])
            };
        }
        catch (CliException ex)
        {
            Console.Error.WriteLine($"שגיאה: {ex.Message}");
            return 2;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                $"שגיאה לא צפויה: {ex.Message}");

            return 1;
        }
    }

    private static async Task<int> RunBundleAsync(
        string[] args)
    {
        var options = CliParser.ParseBundleOptions(args);
        var result = await BundleService.CreateAsync(options);

        Console.WriteLine(
            $"האריזה הושלמה בהצלחה: {result}");

        return 0;
    }

    private static int CreateResponseFile()
    {
        var options = ResponseFileService.AskForOptions();

        var path = Path.GetFullPath("bundle.rsp");

        File.WriteAllLines(
            path,
            ResponseFileService.BuildResponseArguments(options),
            new UTF8Encoding(false));

        Console.WriteLine($"קובץ תגובה נוצר: {path}");
        Console.WriteLine("להרצה: codebundler @bundle.rsp");

        return 0;
    }

    private static int UnknownCommand(string command)
    {
        Console.Error.WriteLine(
            $"פקודה לא מוכרת: {command}");

        PrintHelp();
        return 2;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("""
        CodeBundler - אריזת קובצי קוד לקובץ אחד

        פקודות:
          bundle       אריזת קובצי קוד
          create-rsp   יצירת קובץ תגובה
          help         הצגת העזרה

        אפשרויות:
          --language, -l              חובה: שפות או all
          --output, -o                שם קובץ או נתיב
          --note, -n                  הערת מקור
          --sort, -s                  name או type
          --remove-empty-lines, -e    הסרת שורות ריקות
          --author, -a                שם היוצר

        דוגמאות:
          codebundler bundle -l csharp -o bundle.txt
          codebundler bundle -l all -o bundle.txt -n
          codebundler @bundle.rsp
        """);
    }
}