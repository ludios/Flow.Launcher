// Model-output: Claude Fable 5.1
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Flow.Launcher.Plugin.Explorer.Search.Everything
{
    public static class EverythingHelper
    {
        #region Query

        // Characters that Everything would read as search syntax if they appeared inside an ext: list.
        private static readonly char[] ExtensionSyntaxCharacters = [' ', '\t', '"', '|', '!', '<', '>', ';', ':', '\\', '/', '*', '?'];

        public readonly record struct PreparedQuery(
            EverythingSearchOption Option,
            string SearchText);

        /// <summary>
        /// Builds the text handed to Everything's search call. Exclusions are written as query terms so that Everything
        /// drops excluded items before applying <see cref="EverythingSearchOption.MaxCount"/>; filtering them only
        /// afterwards lets them use up the capped result slots and hide wanted items that sort after them.
        /// </summary>
        /// <param name="option">The search criteria.</param>
        /// <returns>The search text, and the option with <see cref="EverythingSearchOption.UseRegex"/> set and the
        /// regex prefix stripped from the keyword when there was one. In regex mode the whole text is the pattern, so
        /// no exclusion terms are added and the caller has to filter afterwards.</returns>
        public static PreparedQuery PrepareQuery(EverythingSearchOption option)
        {
            if (option.Offset < 0)
                throw new ArgumentOutOfRangeException(nameof(option.Offset), option.Offset, "Offset must be greater than or equal to 0");
            if (option.MaxCount < 0)
                throw new ArgumentOutOfRangeException(nameof(option.MaxCount), option.MaxCount, "MaxCount must be greater than or equal to 0");

            var keyword = option.Keyword;
            if (!string.IsNullOrEmpty(keyword) && keyword.StartsWith("@", StringComparison.Ordinal))
            {
                option.UseRegex = true;
                keyword = keyword[1..];
            }

            var builder = new StringBuilder();
            // Everything evaluates '|' before AND by default, but that order is configurable; grouping an OR keyword
            // keeps the ANDed terms below applying to all of its branches either way.
            var groupOr = !option.UseRegex && keyword?.Contains('|') == true;
            builder.Append(groupOr ? $"<{keyword}>" : keyword);

            if (!string.IsNullOrWhiteSpace(option.ParentPath))
            {
                builder.Append($" {(option.IsRecursive ? "" : "parent:")}\"{option.ParentPath}\"");
            }

            if (option.IsContentSearch)
            {
                builder.Append($" content:\"{option.ContentSearchKeyword}\"");
            }

            if (!option.UseRegex)
            {
                AppendExclusionTerms(builder, option.ExcludedExtensions, option.ExcludedPaths);
            }

            return new PreparedQuery(option with { Keyword = keyword }, builder.ToString());
        }

        /// <summary>
        /// Appends one "!ext:a;b" term and one !"path\" term per folder; Everything ANDs space-separated terms.
        /// Entries that cannot be written safely in Everything's syntax are skipped; the caller's own post-filter
        /// still catches them, just without protection from the result cap.
        /// </summary>
        /// <param name="builder">The search text so far; receives the new terms.</param>
        /// <param name="excludedExtensions">Extensions without dots, or null.</param>
        /// <param name="excludedPaths">Folders to exclude recursively, or null.</param>
        private static void AppendExclusionTerms(StringBuilder builder, IReadOnlyCollection<string> excludedExtensions, IReadOnlyCollection<string> excludedPaths)
        {
            if (excludedExtensions != null)
            {
                // ext: only ever matches files, which mirrors the caller's file-only extension filter.
                var extensions = excludedExtensions
                    .Where(x => x.Length > 0 && x.IndexOfAny(ExtensionSyntaxCharacters) < 0)
                    .ToList();
                if (extensions.Count > 0)
                {
                    builder.Append(" !ext:").AppendJoin(';', extensions);
                }
            }

            if (excludedPaths != null)
            {
                // A term containing a backslash is matched against the full path, and quoting makes everything inside
                // it literal. The trailing separator stops "C:\a\" from also excluding "C:\ab\".
                foreach (var path in excludedPaths)
                {
                    if (string.IsNullOrWhiteSpace(path) || path.Contains('"'))
                    {
                        continue;
                    }

                    var folder = path.Replace('/', '\\').TrimEnd('\\');
                    builder.Append(" !\"").Append(folder).Append("\\\"");
                }
            }
        }

        #endregion

        #region Result

        /// <summary>
        /// Convert the highlighted string from Everything API to a list of highlight indexes for our Result.
        /// </summary>
        /// <param name="highlightString">Text inside a * quote is highlighted, two consecutive *'s is a single literal *. For example, in the highlighted text: abc*123* the 123 part is highlighted.</param>
        /// <returns>A list of zero-based character indices that should be highlighted.</returns>
        public static List<int> EverythingHighlightStringToHighlightList(string highlightString)
        {
            var highlightData = new List<int>();

            if (string.IsNullOrEmpty(highlightString))
                return highlightData;

            var isHighlighted = false;
            var actualIndex = 0; // Index in the actual string (without * markers)
            var length = highlightString.Length;

            for (var i = 0; i < length; i++)
            {
                if (highlightString[i] == '*')
                {
                    // Check if it's a literal * (two consecutive *)
                    if (i + 1 < length && highlightString[i + 1] == '*')
                    {
                        // Two consecutive *'s represent a single literal *
                        if (isHighlighted)
                        {
                            highlightData.Add(actualIndex);
                        }
                        actualIndex++;
                        i++; // Skip the next *
                    }
                    else
                    {
                        isHighlighted = !isHighlighted;
                    }
                }
                else
                {
                    // Regular character
                    if (isHighlighted)
                    {
                        highlightData.Add(actualIndex);
                    }
                    actualIndex++;
                }
            }

            return highlightData;
        }

        #endregion
    }
}
