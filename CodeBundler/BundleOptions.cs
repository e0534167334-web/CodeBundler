namespace CodeBundler;

/// <summary>
/// כל הגדרות האריזה מרוכזות באובייקט אחד.
/// </summary>
internal sealed record BundleOptions(
    IReadOnlyList<string> Languages,
    string OutputPath,
    bool IncludeSourceNotes,
    string SortBy,
    bool RemoveEmptyLines,
    string? Author);