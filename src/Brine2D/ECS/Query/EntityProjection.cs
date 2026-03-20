namespace Brine2D.ECS.Query;

/// <summary>
/// Lightweight read-only list that projects entities from a cached-query tuple list.
/// Wraps the live <see cref="List{T}"/> reference — no copy, no extra storage.
/// </summary>
internal sealed class EntityProjection<TTuple> : IReadOnlyList<Entity>
{
    private readonly List<TTuple> _source;
    private readonly Func<TTuple, Entity> _selector;

    internal EntityProjection(List<TTuple> source, Func<TTuple, Entity> selector)
    {
        _source = source;
        _selector = selector;
    }

    public int Count => _source.Count;
    public Entity this[int index] => _selector(_source[index]);

    public IEnumerator<Entity> GetEnumerator() => new Enumerator(this);

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => new Enumerator(this);

    private sealed class Enumerator : IEnumerator<Entity>
    {
        private readonly EntityProjection<TTuple> _projection;
        private int _index = -1;

        internal Enumerator(EntityProjection<TTuple> projection) => _projection = projection;

        public Entity Current => _projection._selector(_projection._source[_index]);
        object System.Collections.IEnumerator.Current => Current;
        public bool MoveNext() => ++_index < _projection._source.Count;
        public void Reset() => _index = -1;
        public void Dispose() { }
    }
}