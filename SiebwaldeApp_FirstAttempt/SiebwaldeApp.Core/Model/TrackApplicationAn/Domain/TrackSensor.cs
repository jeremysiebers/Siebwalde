// File: SiebwaldeApp.Core/Domain/TrackSensor.cs
namespace SiebwaldeApp.Core
{
    using System;

    /// <summary>
    /// Dedicated sensor abstraction for the new TrackApplication code.
    /// Use this instead of the legacy Sensor until unified later.
    /// </summary>
    public class TrackSensor
    {
        public string Name { get; }
        public bool IsActive { get; private set; }
        public DateTime LastChangeUtc { get; private set; }

        public TrackSensor(string name) => Name = name;

        public void Set(bool active)
        {
            if (IsActive == active) return;
            IsActive = active;
            LastChangeUtc = DateTime.UtcNow;
        }
    }
}
