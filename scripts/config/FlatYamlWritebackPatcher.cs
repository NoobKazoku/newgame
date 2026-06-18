namespace GFrameworkGodotTemplate.scripts.config;

/// <summary>
///     Provides value writeback for flat top-level YAML while preserving comments and grouping.
/// </summary>
public static class FlatYamlWritebackPatcher
{
    public static string MergePreservingComments(string existingYaml, string generatedYaml)
    {
        ArgumentNullException.ThrowIfNull(existingYaml);
        ArgumentNullException.ThrowIfNull(generatedYaml);

        var normalizedExisting = NormalizeLineEndings(existingYaml);
        var normalizedGenerated = NormalizeLineEndings(generatedYaml);
        var generatedEntries = ParseEntries(normalizedGenerated);
        if (generatedEntries.Count == 0) return existingYaml;

        var matchedKeys = new HashSet<string>(StringComparer.Ordinal);
        var outputLines = new List<string>();
        foreach (var existingLine in SplitLines(normalizedExisting))
        {
            if (!TryParseTopLevelEntry(existingLine, out var existingEntry) ||
                !generatedEntries.TryGetValue(existingEntry.Key, out var generatedEntry))
            {
                outputLines.Add(existingLine);
                continue;
            }

            matchedKeys.Add(existingEntry.Key);
            outputLines.Add(string.Equals(existingEntry.ValueText, generatedEntry.ValueText, StringComparison.Ordinal)
                ? existingLine
                : generatedEntry.RawLine);
        }

        var missingEntries = generatedEntries.Values
            .Where(entry => !matchedKeys.Contains(entry.Key))
            .ToArray();
        if (missingEntries.Length > 0)
        {
            if (outputLines.Count > 0 && !string.IsNullOrWhiteSpace(outputLines[^1]))
                outputLines.Add(string.Empty);

            outputLines.AddRange(missingEntries.Select(static entry => entry.RawLine));
        }

        var newline = DetectPreferredNewline(existingYaml, generatedYaml);
        var merged = string.Join("\n", outputLines);
        if (ShouldKeepTrailingNewline(existingYaml, generatedYaml)) merged += "\n";

        return merged.Replace("\n", newline, StringComparison.Ordinal);
    }

    private static Dictionary<string, YamlEntry> ParseEntries(string yaml)
    {
        var entries = new Dictionary<string, YamlEntry>(StringComparer.Ordinal);
        foreach (var line in SplitLines(yaml))
        {
            if (TryParseTopLevelEntry(line, out var entry)) entries[entry.Key] = entry;
        }

        return entries;
    }

    private static IEnumerable<string> SplitLines(string text)
    {
        return text.Split('\n');
    }

    private static bool TryParseTopLevelEntry(string line, out YamlEntry entry)
    {
        entry = default;
        if (string.IsNullOrWhiteSpace(line)) return false;

        var trimmedStart = line.TrimStart();
        if (trimmedStart.StartsWith('#') || trimmedStart.Length != line.Length) return false;

        var separatorIndex = line.IndexOf(':');
        if (separatorIndex <= 0) return false;

        var key = line[..separatorIndex].Trim();
        if (string.IsNullOrWhiteSpace(key) || key.Contains(' ')) return false;

        var valueText = line[(separatorIndex + 1)..].Trim();
        entry = new YamlEntry(key, valueText, line);
        return true;
    }

    private static string NormalizeLineEndings(string text)
    {
        return text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
    }

    private static string DetectPreferredNewline(string existingYaml, string generatedYaml)
    {
        return existingYaml.Contains("\r\n", StringComparison.Ordinal) ||
               generatedYaml.Contains("\r\n", StringComparison.Ordinal)
            ? "\r\n"
            : "\n";
    }

    private static bool ShouldKeepTrailingNewline(string existingYaml, string generatedYaml)
    {
        return existingYaml.EndsWith('\n') ||
               existingYaml.EndsWith("\r\n", StringComparison.Ordinal) ||
               generatedYaml.EndsWith('\n') ||
               generatedYaml.EndsWith("\r\n", StringComparison.Ordinal);
    }

    private readonly record struct YamlEntry(string Key, string ValueText, string RawLine);
}
