// OpenNotch — original code, MIT licensed.
using OpenNotch.Core.Models;

namespace OpenNotch.Core.Services;

public sealed class ClipboardHistory
{
    private readonly List<ClipboardItem> _items = new();
    private readonly int _maxItems;

    public ClipboardHistory(int maxItems = 50) => _maxItems = maxItems > 0 ? maxItems : 50;

    public IReadOnlyList<ClipboardItem> Items => _items.AsReadOnly();

    public bool Add(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        var trimmed = text.Trim();
        if (_items.Count > 0 && string.Equals(_items[0].Text, trimmed, StringComparison.Ordinal))
            return false;
        _items.Insert(0, new ClipboardItem(trimmed));
        Trim();
        return true;
    }

    private void Trim()
    {
        while (_items.Count > _maxItems)
        {
            var victim = _items.LastOrDefault(i => !i.Pinned);
            if (victim is null) break;
            _items.Remove(victim);
        }
    }

    public IReadOnlyList<ClipboardItem> Search(string? query, ClipboardKind? kind = null)
    {
        var q = query?.Trim() ?? "";
        var pool = _items.AsEnumerable();
        if (kind is not null) pool = pool.Where(i => i.Kind == kind.Value);
        if (!string.IsNullOrEmpty(q))
            pool = pool.Where(i => i.Text.Contains(q, StringComparison.OrdinalIgnoreCase));
        pool = pool.ToList();
        // Pinned first, then newest first (list is already newest first).
        return pool.OrderByDescending(i => i.Pinned).ThenByDescending(i => i.CreatedAtUtc).ToList();
        // Note: OrderByDescending on CreatedAtUtc is stable enough for v1;
        // list order is the primary newest-first order, pinned is the sort key.
    }

    public bool Pin(Guid id) => SetPin(id, true);
    public bool Unpin(Guid id) => SetPin(id, false);
    private bool SetPin(Guid id, bool pinned)
    {
        var item = _items.FirstOrDefault(i => i.Id == id);
        if (item is null) return false;
        item.Pinned = pinned; return true;
    }

    public bool Remove(Guid id)
    {
        var item = _items.FirstOrDefault(i => i.Id == id);
        if (item is null) return false;
        _items.Remove(item); return true;
    }

    public void ClearUnpinned() => _items.RemoveAll(i => !i.Pinned);
    public void ClearAll() => _items.Clear();
}
