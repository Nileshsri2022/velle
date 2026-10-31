// OpenInstaller — original code, MIT licensed.
//
// winget prints human-readable tables (no stable JSON output for search at
// the time of writing), so this parser reads those tables by column
// position, falling back to whitespace splitting when the header layout
// cannot be determined. It is deliberately pure and side-effect free so it
// can be unit tested without winget installed.
using OpenInstaller.Models;

namespace OpenInstaller.Services;

public static class WingetOutputParser
{
    public static bool IsNoPackageFound(string output)
        => output.Contains("No package found matching input criteria", StringComparison.OrdinalIgnoreCase)
            || output.Contains("No installed package found matching input criteria", StringComparison.OrdinalIgnoreCase);

    public static bool IsNoApplicableUpdate(string output)
        => output.Contains("No applicable update found", StringComparison.OrdinalIgnoreCase);

    /// <summary>Parse `winget search` table output into package rows.</summary>
    public static IReadOnlyList<PackageInfo> ParseSearch(string output)
    {
        var rows = ParseTable(output);
        var result = new List<PackageInfo>();
        foreach (var row in rows)
        {
            if (!row.TryGetValue("Id", out var id) || string.IsNullOrWhiteSpace(id))
            {
                continue;
            }
            result.Add(new PackageInfo
            {
                Name = row.TryGetValue("Name", out var name) ? name : string.Empty,
                Id = id,
                Version = row.TryGetValue("Version", out var version) ? version : string.Empty,
                Source = row.TryGetValue("Source", out var source) ? source : string.Empty,
            });
        }
        return result;
    }

    /// <summary>
    /// Parse `winget list` table output. Returns (id, name, installed
    /// version, available version, source) rows.
    /// </summary>
    public static IReadOnlyList<(string Id, string Name, string Version, string Available, string Source)> ParseList(string output)
    {
        var rows = ParseTable(output);
        var result = new List<(string, string, string, string, string)>();
        foreach (var row in rows)
        {
            if (!row.TryGetValue("Id", out var id) || string.IsNullOrWhiteSpace(id))
            {
                continue;
            }
            result.Add((
                id,
                row.TryGetValue("Name", out var name) ? name : string.Empty,
                row.TryGetValue("Version", out var version) ? version : string.Empty,
                row.TryGetValue("Available", out var available) ? available : string.Empty,
                row.TryGetValue("Source", out var source) ? source : string.Empty));
        }
        return result;
    }

    /// <summary>Parse `winget show` key/value output (e.g. "Publisher: ...").</summary>
    public static Dictionary<string, string> ParseShow(string output)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rawLine in output.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');
            var colon = line.IndexOf(':');
            if (colon <= 0)
            {
                continue;
            }
            var key = line[..colon].Trim();
            var value = line[(colon + 1)..].Trim();
            if (key.Length > 0 && !result.ContainsKey(key))
            {
                result[key] = value;
            }
        }
        return result;
    }

    /// <summary>
    /// Compare two version strings segment by segment, numerically where
    /// possible. Returns &lt; 0 if <paramref name="left"/> is older,
    /// 0 if equal, &gt; 0 if newer. Non-numeric segments compare as text.
    /// </summary>
    public static int CompareVersions(string? left, string? right)
    {
        var a = SplitVersion(left);
        var b = SplitVersion(right);
        var count = Math.Max(a.Length, b.Length);
        for (var i = 0; i < count; i++)
        {
            var x = i < a.Length ? a[i] : "0";
            var y = i < b.Length ? b[i] : "0";
            if (x == y)
            {
                continue;
            }
            if (int.TryParse(x, out var xi) && int.TryParse(y, out var yi))
            {
                return xi.CompareTo(yi);
            }
            return string.CompareOrdinal(x, y);
        }
        return 0;
    }

    private static string[] SplitVersion(string? version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return Array.Empty<string>();
        }
        var cleaned = version.Trim().TrimStart('v', 'V');
        // Keep only the leading dotted-numeric/text run, e.g. "1.2.3-beta".
        var parts = cleaned.Split('.', '-', '_');
        var result = new List<string>();
        foreach (var part in parts)
        {
            var digits = new string(part.TakeWhile(char.IsDigit).ToArray());
            result.Add(digits.Length > 0 ? digits : part);
            if (digits.Length == 0)
            {
                break;
            }
        }
        return result.ToArray();
    }

    /// <summary>
    /// Generic winget table parser. Finds the header line (containing
    /// "Name" and "Id"), skips the dashed separator line, then slices each
    /// data row at the header's column positions.
    /// </summary>
    private static List<Dictionary<string, string>> ParseTable(string output)
    {
        var lines = output.Split('\n')
            .Select(line => line.TrimEnd('\r'))
            .ToList();

        var headerIndex = lines.FindIndex(line =>
            line.Contains("Name", StringComparison.Ordinal)
            && line.Contains("Id", StringComparison.Ordinal)
            && line.Contains("Version", StringComparison.Ordinal));
        if (headerIndex < 0)
        {
            return new List<Dictionary<string, string>>();
        }

        var header = lines[headerIndex];
        var columns = new List<(string Name, int Start)>();
        foreach (var columnName in new[] { "Name", "Id", "Version", "Available", "Match", "Source" })
        {
            var start = header.IndexOf(columnName, StringComparison.Ordinal);
            if (start >= 0 && columns.All(c => c.Start != start))
            {
                columns.Add((columnName, start));
            }
        }
        columns.Sort((a, b) => a.Start.CompareTo(b.Start));
        if (columns.Count < 3 || columns[0].Name != "Name" || columns[1].Name != "Id")
        {
            return new List<Dictionary<string, string>>();
        }

        var rows = new List<Dictionary<string, string>>();
        for (var i = headerIndex + 1; i < lines.Count; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line))
            {
                break;
            }
            if (line.Trim().All(c => c == '-' || c == ' '))
            {
                continue;
            }
            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var c = 0; c < columns.Count; c++)
            {
                var start = columns[c].Start;
                if (start >= line.Length)
                {
                    row[columns[c].Name] = string.Empty;
                    continue;
                }
                var end = c + 1 < columns.Count ? Math.Min(columns[c + 1].Start, line.Length) : line.Length;
                row[columns[c].Name] = end > start ? line[start..end].Trim() : string.Empty;
            }
            // A data row whose Id cell is empty is not a real package row
            // (for example a wrapped continuation line); skip it.
            if (row.TryGetValue("Id", out var id) && !string.IsNullOrWhiteSpace(id))
            {
                rows.Add(row);
            }
        }
        return rows;
    }
}
