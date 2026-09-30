using System;
using System.Collections.Generic;
using SiebwaldeApp.Core;
using SiebwaldeApp.Core.TrackApplication.Simulator;
using SiebwaldeApp.Core.TrackApplication.Topology;

namespace SiebwaldeApp.Integration
{
    /// <summary>
    /// The FullSimulation composition projected from a single <see cref="LayoutProfile"/>: the
    /// profile itself, the deterministic transport config (detected slaves), and the production
    /// runtime types (<see cref="BlockTopology"/>, <see cref="KoploperBlockMap"/>,
    /// <see cref="SwitchMapping"/>, <see cref="TrackAmplifierGroups"/>) so the real control chain
    /// is composed from one durable profile.
    /// </summary>
    public sealed class FullSimulationComposition
    {
        public LayoutProfile Profile { get; init; } = null!;
        public TrackSimulatorConfig SimulatorConfig { get; init; } = null!;
        public BlockTopology BlockTopology { get; init; } = null!;
        public KoploperBlockMap KoploperBlockMap { get; init; } = null!;
        public SwitchMapping SwitchMapping { get; init; } = null!;
        public TrackAmplifierGroups TrackAmplifierGroups { get; init; } = null!;
    }

    /// <summary>
    /// Minimal factory that turns a <see cref="LayoutProfile"/> into the FullSimulation
    /// composition used by <see cref="TrackControlHost"/> and
    /// <see cref="TrackApplicationRuntimeHost"/>.
    /// </summary>
    public static class FullSimulationProfile
    {
        /// <summary>The default periodic SLAVEINFO refresh for FullSimulation.</summary>
        private static readonly TimeSpan PeriodicSlaveInfoInterval = TimeSpan.FromMilliseconds(500);

        /// <summary>Builds the FullSimulation composition from an already-loaded profile.</summary>
        public static FullSimulationComposition Compose(LayoutProfile profile)
        {
            if (profile is null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            return new FullSimulationComposition
            {
                Profile = profile,
                SimulatorConfig = new TrackSimulatorConfig(
                    detectedSlaves: profile.DetectedSlaves,
                    periodicSlaveInfoInterval: PeriodicSlaveInfoInterval),
                BlockTopology = profile.ToBlockTopology(),
                KoploperBlockMap = profile.ToKoploperBlockMap(),
                SwitchMapping = profile.ToSwitchMapping(),
                TrackAmplifierGroups = profile.ToTrackAmplifierGroups()
            };
        }

        /// <summary>
        /// Loads a profile JSON file and builds its composition. Returns false (with every error)
        /// when the file cannot be read or the profile is invalid.
        /// </summary>
        public static bool TryLoadFromFile(
            string path,
            out FullSimulationComposition? composition,
            out IReadOnlyList<string> errors)
        {
            if (!LayoutProfileLoader.TryLoadFromFile(path, out var profile, out errors) || profile is null)
            {
                composition = null;
                return false;
            }

            composition = Compose(profile);
            return true;
        }

        /// <summary>
        /// Loads a repository profile by its display name (<see cref="LayoutProfile.Name"/>) and
        /// builds its composition. Returns false (with every error) when the profile is unknown or
        /// cannot be read/validated.
        /// </summary>
        public static bool TryLoadByName(
            string name,
            out FullSimulationComposition? composition,
            out IReadOnlyList<string> errors)
        {
            if (!LayoutProfileLoader.TryLoadByName(name, out var profile, out errors) || profile is null)
            {
                composition = null;
                return false;
            }

            composition = Compose(profile);
            return true;
        }
    }
}
