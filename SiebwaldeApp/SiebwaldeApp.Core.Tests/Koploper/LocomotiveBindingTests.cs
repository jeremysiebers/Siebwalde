using System.Collections.Generic;
using SiebwaldeApp.Core.TrackApplication.Topology;
using Xunit;

namespace SiebwaldeApp.Core.Tests.Koploper
{
    /// <summary>
    /// Validation tests for the <see cref="LocomotiveBinding"/> identity binding in
    /// <see cref="LayoutProfileLoader"/>: the whole mapping being absent is legal, but each present
    /// binding must be valid, and duplicate internal-locomotive-id or DCC-address bindings are invalid.
    /// </summary>
    public class LocomotiveBindingTests
    {
        [Fact]
        public void SimpleLoopProfile_HasTwoLocomotiveBindings()
        {
            Assert.True(
                LayoutProfileLoader.TryLoadFromFile(LayoutProfileLoader.SimpleLoopPath, out var profile, out var errors),
                string.Join("; ", errors));
            Assert.NotNull(profile);

            Assert.Equal(2, profile!.LocomotiveBindings.Count);
            Assert.Contains(profile.LocomotiveBindings, b => b.InternalLocomotiveId == 1 && b.DccAddress == 1);
            Assert.Contains(profile.LocomotiveBindings, b => b.InternalLocomotiveId == 2 && b.DccAddress == 2);

            var map = profile.ToLocomotiveBindingMap();
            Assert.Equal(1, map[1]);
            Assert.Equal(2, map[2]);
        }

        [Fact]
        public void DuplicateInternalLocomotiveId_IsInvalid()
        {
            string json = Json(@"[ { ""internalLocomotiveId"": 1, ""dccAddress"": 1 }, { ""internalLocomotiveId"": 1, ""dccAddress"": 2 } ]");

            bool ok = LayoutProfileLoader.TryLoad(json, out var profile, out var errors);

            Assert.False(ok);
            Assert.Null(profile);
            Assert.Contains(errors, e => e.Contains("Duplicate locomotive binding for internal locomotive id 1."));
        }

        [Fact]
        public void DuplicateDccAddress_IsInvalid()
        {
            string json = Json(@"[ { ""internalLocomotiveId"": 1, ""dccAddress"": 1 }, { ""internalLocomotiveId"": 2, ""dccAddress"": 1 } ]");

            bool ok = LayoutProfileLoader.TryLoad(json, out var profile, out var errors);

            Assert.False(ok);
            Assert.Null(profile);
            Assert.Contains(errors, e => e.Contains("DCC address 1 is bound to more than one internal locomotive id."));
        }

        [Fact]
        public void InvalidInternalLocomotiveId_IsInvalid()
        {
            string json = Json(@"[ { ""internalLocomotiveId"": 0, ""dccAddress"": 1 } ]");

            bool ok = LayoutProfileLoader.TryLoad(json, out var profile, out var errors);

            Assert.False(ok);
            Assert.Null(profile);
            Assert.Contains(errors, e => e.Contains("invalid internal locomotive id 0"));
        }

        [Fact]
        public void InvalidDccAddress_IsInvalid()
        {
            string json = Json(@"[ { ""internalLocomotiveId"": 1, ""dccAddress"": 0 } ]");

            bool ok = LayoutProfileLoader.TryLoad(json, out var profile, out var errors);

            Assert.False(ok);
            Assert.Null(profile);
            Assert.Contains(errors, e => e.Contains("invalid DCC address 0"));
        }

        [Fact]
        public void EmptyLocomotiveBindings_IsLegal()
        {
            string json = Json("[]");

            bool ok = LayoutProfileLoader.TryLoad(json, out var profile, out var errors);

            Assert.True(ok, string.Join("; ", errors));
            Assert.NotNull(profile);
            Assert.Empty(profile!.LocomotiveBindings);
        }

        private static string Json(string locomotiveBindings)
            => "{ \"name\": \"Test\", \"detectedSlaves\": [1], " +
               "\"sections\": [ { \"id\": 1, \"amplifierSlave\": 1, \"bezetmelders\": [\"1.01\"], \"lengthMm\": 1000.0 } ], " +
               "\"blocks\": [ { \"id\": 1, \"sectionIds\": [1] } ], " +
               "\"locomotiveBindings\": " + locomotiveBindings + " }";
    }
}
