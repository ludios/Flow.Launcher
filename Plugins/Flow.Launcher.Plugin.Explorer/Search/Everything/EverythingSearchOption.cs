// Model-output: Claude Fable 5.1
using System.Collections.Generic;

namespace Flow.Launcher.Plugin.Explorer.Search.Everything
{
    /// <param name="Keyword">What the user typed. A leading '@' makes Everything treat the rest as a regular expression.</param>
    /// <param name="UseRegex">Whether Everything must treat <paramref name="Keyword"/> as a regular expression. Query
    /// preparation turns this on when it strips a leading '@'.</param>
    /// <param name="ExcludedExtensions">File extensions (without dots) that Everything must leave out of its results,
    /// so that they do not use up <paramref name="MaxCount"/>. Null for none.</param>
    /// <param name="ExcludedPaths">Folders whose contents, recursively, Everything must leave out for the same reason.
    /// Null for none.</param>
    public record struct EverythingSearchOption(
        string Keyword,
        EverythingSortOption SortOption,
        bool IsContentSearch = false,
        string ContentSearchKeyword = default,
        string ParentPath = default,
        bool IsRecursive = true,
        int Offset = 0,
        int MaxCount = 100,
        bool IsFullPathSearch = true,
        bool IsRunCounterEnabled = true,
        bool UseRegex = false,
        IReadOnlyCollection<string> ExcludedExtensions = null,
        IReadOnlyCollection<string> ExcludedPaths = null
    );
}
