// OpenNotch — original code, MIT licensed.
namespace OpenNotch.Core.Models;

public enum ClipboardKind { Text, Link, Image }

public sealed class ClipboardItem
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Text { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public bool Pinned { get; set; }

    public ClipboardItem(string text) : this(text, DateTimeOffset.UtcNow) { }
    public ClipboardItem(string text, DateTimeOffset createdAtUtc)
    {
        Text = text; CreatedAtUtc = createdAtUtc;
    }

    public ClipboardKind Kind
    {
        get
        {
            var t = Text.Trim();
            // A link item is a single URL (no spaces/newlines). Anything else is text.
            // Images are not stored in v1, so no item is Image yet — the filter
            // exists in the UI now, and Step 2 will fill it.
            if (!t.Contains(' ') && !t.Contains('\n') && !t.Contains('\r')
                && (t.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                    || t.StartsWith("http://", StringComparison.OrdinalIgnoreCase)))
                return ClipboardKind.Link;
            return ClipboardKind.Text;
        }
    }

    public int CharCount => Text.Length;
    public int WordCount => Text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
    public string SingleLine => Text.Replace("\r", " ").Replace("\n", " ").Trim();
    public string DisplayText => Pinned ? "📌 " + SingleLine : SingleLine;
    public string Preview => SingleLine.Length <= 200 ? SingleLine : SingleLine[..200] + "…";

    public string TimeAgo => TimeAgoAt(DateTimeOffset.UtcNow);
    public string TimeAgoAt(DateTimeOffset now)
    {
        var span = now - CreatedAtUtc;
        if (span < TimeSpan.FromMinutes(1)) return "just now";
        if (span < TimeSpan.FromHours(1)) return $"{(int)span.TotalMinutes}m ago";
        if (span < TimeSpan.FromDays(1)) return $"{(int)span.TotalHours}h ago";
        return $"{(int)span.TotalDays}d ago";
    }

    public string MetaText => $"{TimeAgo} • {WordCount} words, {CharCount} chars • {Kind}";
}
