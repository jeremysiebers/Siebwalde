// SiebwaldeApp.Core/TrackRegistry.cs

namespace SiebwaldeApp.Core
{
    public sealed class TrackRegistry
    {
        private readonly Dictionary<int, (TrackBlock Block, TrackMetadata Meta)> _byId = new();

        public void Register(TrackBlock block, TrackMetadata meta) => _byId[block.Id] = (block, meta);

        public (TrackBlock Block, TrackMetadata Meta)? Get(int id) =>
            _byId.TryGetValue(id, out var t) ? t : null;

        public IEnumerable<TrackBlock> AllBlocks() => _byId.Values.Select(v => v.Block);

        public IEnumerable<TrackBlock> Query(string zone = null, TrackRole? role = null, string tag = null)
        {
            IEnumerable<KeyValuePair<int, (TrackBlock Block, TrackMetadata Meta)>> q = _byId;
            if (!string.IsNullOrWhiteSpace(zone))
                q = q.Where(kv => kv.Value.Meta.Zone == zone);
            if (role.HasValue)
                q = q.Where(kv => kv.Value.Meta.Role == role.Value);
            if (!string.IsNullOrWhiteSpace(tag))
                q = q.Where(kv => kv.Value.Meta.Tags.Contains(tag));
            return q.Select(kv => kv.Value.Block);
        }
    }
}
