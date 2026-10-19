namespace CodeBundler;

internal sealed record BundleOptions(
    IReadOnlyList<string> Languages,
    string InputPath,
    string OutputPath,
    bool IncludeSourceNotes,
    string SortBy,
    bool RemoveEmptyLines,
    string? Author=null);