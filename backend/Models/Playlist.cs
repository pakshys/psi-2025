using System.Collections;

namespace backend.Models;

public class Playlist : IEnumerable<PlaylistItem>
{
    private readonly List<PlaylistItem> _items = new();
    public IReadOnlyList<PlaylistItem> Items => _items.AsReadOnly();

    public void Add(PlaylistItem item)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));

        item.Position = _items.Count;
        _items.Add(item);
    }

    public bool Remove(PlaylistItem item)
    {
        return _items.Remove(item);
    }
    
    public PlaylistItem? Next() => _items.OrderBy(i => i.Position).FirstOrDefault();
    public int Count => _items.Count;

    public IEnumerator<PlaylistItem> GetEnumerator() => _items.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}