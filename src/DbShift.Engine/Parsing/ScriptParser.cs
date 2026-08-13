using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using DbShift.Core.Enums;
using DbShift.Core.Exceptions;
using DbShift.Core.ValueObjects;

namespace DbShift.Engine.Parsing;

/// <summary>
/// Parses Flyway-style migration scripts into <see cref="ParsedMigration"/> objects and
/// provides deterministic hashing, syntax validation and dependency extraction.
///
/// Supported filename conventions:
/// <list type="bullet">
/// <item><c>V001__Name.sql</c>           - versioned schema/data/patch migration.</item>
/// <item><c>V202601150001__Name.sql</c>  - timestamp based versioned migration.</item>
/// <item><c>R__Name.sql</c>              - repeatable migration.</item>
/// <item><c>U001__Name.sql</c>           - rollback migration.</item>
/// </list>
/// </summary>
public sealed partial class ScriptParser
{
    private const string RepeatablePrefix = "R";
    private const string Separator = "__";

#if NET7_0_OR_GREATER
    [GeneratedRegex(@"^[ \t]*--\s*Depends\s*:\s*([^\r\n]+)", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
    private static partial Regex DependsRegex();

    [GeneratedRegex(@"^[ \t]*--\s*Author\s*:\s*([^\r\n]+)", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
    private static partial Regex AuthorRegex();

    [GeneratedRegex(@"^[ \t]*--\s*Description\s*:\s*([^\r\n]+)", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
    private static partial Regex DescriptionRegex();
#else
    private static readonly Regex _dependsRegex =
        new(@"^[ \t]*--\s*Depends\s*:\s*([^\r\n]+)", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

    private static readonly Regex _authorRegex =
        new(@"^[ \t]*--\s*Author\s*:\s*([^\r\n]+)", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

    private static readonly Regex _descriptionRegex =
        new(@"^[ \t]*--\s*Description\s*:\s*([^\r\n]+)", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

    private static Regex DependsRegex() => _dependsRegex;
    private static Regex AuthorRegex() => _authorRegex;
    private static Regex DescriptionRegex() => _descriptionRegex;
#endif

    /// <summary>Parses a single migration script from its file path and content.</summary>
    public ParsedMigration Parse(string filePath, string content)
    {
        // Normalise line endings so the same script produces the same hash and the same
        // metadata extraction regardless of whether it was authored on Windows (CRLF) or
        // Unix (LF). Without this, editing a file on a different OS would change the hash
        // and trigger spurious "checksum drift" failures.
        var normalized = NormalizeLineEndings(content);

        var fileName = GetFileName(filePath);
        var category = GetCategory(filePath);

        var stem = fileName.EndsWith(".sql", StringComparison.OrdinalIgnoreCase)
            ? fileName[..^4]
            : fileName;

        var separatorIndex = stem.IndexOf(Separator, StringComparison.Ordinal);
        if (separatorIndex <= 0 || separatorIndex >= stem.Length - Separator.Length)
        {
            throw new ScriptParseException(
                $"Migration filename '{fileName}' is invalid. Expected format '<prefix><version>__<name>.sql' " +
                "(e.g. V001__CreateTable.sql, R__RefreshView.sql, U001__Rollback_CreateTable.sql).");
        }

        var prefix = stem[..separatorIndex];
        var name = stem[(separatorIndex + Separator.Length)..];

        var (version, type, isRepeatable) = Classify(prefix, category);

        return new ParsedMigration
        {
            FilePath = filePath,
            FileName = fileName,
            Version = version,
            Name = name,
            Type = type,
            Category = category,
            IsRepeatable = isRepeatable,
            Hash = GenerateHash(normalized),
            Content = normalized,
            Dependencies = ExtractDependencies(normalized),
            Author = ExtractFirst(AuthorRegex(), normalized),
            Description = ExtractFirst(DescriptionRegex(), normalized)
        };
    }

    /// <summary>Computes a deterministic SHA-256 hex hash for the supplied script content.</summary>
    /// <remarks>Line endings are normalised to LF before hashing so the hash is stable across platforms.</remarks>
    public string GenerateHash(string content)
    {
        var normalized = NormalizeLineEndings(content);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>
    /// Returns true when the script contains at least one executable (non-comment, non-blank) line.
    /// Replaces the historical <c>ValidateSyntax</c>, which only performed a non-blank check and so
    /// accepted comment-only scripts that would be no-ops at the database.
    /// </summary>
    public bool HasExecutableContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return false;
        }

        foreach (var line in content.Split('\n'))
        {
            var trimmed = line.Trim('\r', ' ', '\t');
            if (trimmed.Length == 0) continue;
            if (trimmed.StartsWith("--", StringComparison.Ordinal)) continue;
            return true;
        }
        return false;
    }

    /// <summary>Extracts the comma separated dependencies declared via <c>-- Depends:</c>.</summary>
    public string[] ExtractDependencies(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return Array.Empty<string>();
        }

        var match = DependsRegex().Match(content);
        if (!match.Success)
        {
            return Array.Empty<string>();
        }

        return match.Groups[1].Value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToArray();
    }

    private static string NormalizeLineEndings(string content) =>
        string.IsNullOrEmpty(content) ? content : content.Replace("\r\n", "\n").Replace('\r', '\n');

    private static (string Version, MigrationType Type, bool IsRepeatable) Classify(string prefix, string category)
    {
        // Repeatable: exact single-letter "R" prefix (case-insensitive). Must be checked before
        // the V/U branches below because those use StartsWith and "R" doesn't conflict, but the
        // ordering documents intent and protects against future prefix additions.
        if (prefix.Length == 1 && prefix.Equals(RepeatablePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return (RepeatablePrefix, MigrationType.Repeatable, true);
        }

        // Rollback: "U" followed by at least one digit. Anchoring to digits prevents filenames
        // like "Unlock__X.sql" from being mis-parsed as rollback version "nlock".
        if (prefix.Length > 1
            && (prefix[0] == 'U' || prefix[0] == 'u')
            && IsValidVersion(prefix[1..]))
        {
            return (prefix[1..], MigrationType.Rollback, false);
        }

        // Versioned: "V" followed by at least one digit (sequence or timestamp).
        if (prefix.Length > 1
            && (prefix[0] == 'V' || prefix[0] == 'v')
            && IsValidVersion(prefix[1..]))
        {
            var type = category.ToLowerInvariant() switch
            {
                "data" => MigrationType.Data,
                "patch" => MigrationType.Patch,
                _ => MigrationType.Schema
            };
            return (prefix[1..], type, false);
        }

        throw new ScriptParseException(
            $"Unrecognised migration prefix '{prefix}'. Valid prefixes are V<digits>, U<digits> and R (e.g. V001__Name.sql, U001__Name.sql, R__Name.sql).");
    }

    /// <summary>
    /// A version token must be all digits (covers both sequence "001" and timestamp "202601150001" forms).
    /// Empty tokens are rejected so that "V__Name.sql" does not silently parse with an empty version.
    /// </summary>
    private static bool IsValidVersion(string token)
    {
        if (string.IsNullOrEmpty(token)) return false;
        foreach (var c in token) if (!char.IsDigit(c)) return false;
        return true;
    }

    private static string ExtractFirst(Regex regex, string content)
    {
        var match = regex.Match(content);
        return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
    }

    private static string GetFileName(string filePath)
    {
        var normalized = filePath.Replace('\\', '/');
        var index = normalized.LastIndexOf('/');
        return index < 0 ? normalized : normalized[(index + 1)..];
    }

    private static string GetCategory(string filePath)
    {
        var normalized = filePath.Replace('\\', '/').TrimEnd('/');
        var segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length >= 2 ? segments[^2] : string.Empty;
    }
}
