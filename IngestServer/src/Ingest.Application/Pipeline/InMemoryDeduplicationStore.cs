using System.Collections.Concurrent;

namespace Ingest.Application.Pipeline;

public sealed class InMemoryDeduplicationStore : IDeduplicationStore
{
    private readonly int _windowSize;
    private readonly ConcurrentDictionary<string, NodeWindow> _windows = new();

    public InMemoryDeduplicationStore(int windowSize = 64)
    {
        _windowSize = windowSize;
    }

    public bool TryRegister(string devEui, uint frameCounter)
    {
        var window = _windows.GetOrAdd(devEui, _ => new NodeWindow(_windowSize));
        return window.TryAdd(frameCounter);
    }

    private sealed class NodeWindow
    {
        private readonly int _capacity;
        private readonly HashSet<uint> _seen;
        private readonly Queue<uint> _order;
        private readonly object _lock = new();

        public NodeWindow(int capacity)
        {
            _capacity = capacity;
            _seen = new HashSet<uint>(capacity);
            _order = new Queue<uint>(capacity);
        }

        public bool TryAdd(uint frameCounter)
        {
            lock (_lock)
            {
                if (_seen.Contains(frameCounter))
                {
                    return false;
                }

                if (_order.Count >= _capacity)
                {
                    uint evicted = _order.Dequeue();
                    _seen.Remove(evicted);
                }

                _seen.Add(frameCounter);
                _order.Enqueue(frameCounter);
                return true;
            }
        }
    }
}