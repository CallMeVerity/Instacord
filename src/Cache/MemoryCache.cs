using Instacord.Models;

namespace Instacord.Cache;

public sealed class MemoryCache
{
    private readonly int _capacity;
    private readonly LinkedList<string> _order = new();
    private readonly Dictionary<string, LinkedListNode<string>> _nodes = new();
    private readonly Dictionary<string, InstagramPost> _posts = new();
    private readonly object _lock = new();

    public MemoryCache(int capacity) => _capacity = capacity;

    public int Count
    {
        get { lock (_lock) { return _posts.Count; } }
    }

    public bool TryGet(string code, out InstagramPost? post)
    {
        lock (_lock)
        {
            if (!_posts.TryGetValue(code, out post))
            {
                post = null;
                return false;
            }
            _order.Remove(_nodes[code]);
            _order.AddLast(_nodes[code]);
            return true;
        }
    }

    public void Put(string code, InstagramPost post)
    {
        lock (_lock)
        {
            if (_posts.ContainsKey(code))
            {
                _posts[code] = post;
                _order.Remove(_nodes[code]);
                _order.AddLast(_nodes[code]);
                return;
            }

            while (_posts.Count >= _capacity)
            {
                var oldest = _order.First!.Value;
                _order.RemoveFirst();
                _nodes.Remove(oldest);
                _posts.Remove(oldest);
            }

            _posts[code] = post;
            var node = _order.AddLast(code);
            _nodes[code] = node;
        }
    }

    public void Remove(string code)
    {
        lock (_lock)
        {
            if (!_posts.Remove(code))
                return;
            _order.Remove(_nodes[code]);
            _nodes.Remove(code);
        }
    }
}