// SiebwaldeApp.Core/Domain/TrackMetadata.cs
using System.Collections.Generic;

namespace SiebwaldeApp.Core
{
    public sealed class TrackMetadata
    {
        public string Zone { get; set; } = "";         // e.g. "StationTop", "StationBottom", "Pendelbaan", ...
        public TrackRole Role { get; set; }            // role/capability
        public HashSet<string> Tags { get; } = new();  // free-form labels (e.g. "Station", "EntryHead", etc.)
    }
}
