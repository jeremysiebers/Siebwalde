using SiebwaldeApp.Core;
using Xunit;

namespace SiebwaldeApp.Core.Tests
{
    /// <summary>
    /// Parsing tests for the switch mapping configuration.
    /// </summary>
    public class SwitchMappingTests
    {
        [Fact]
        public void ParsesTheOvalMapping()
        {
            var mapping = SwitchMapping.Parse("switches: 1:1:keep, 2:2:keep");

            Assert.Empty(mapping.Errors);
            Assert.Equal(2, mapping.Entries.Count);
            Assert.True(mapping.IsMapped(1));
            Assert.True(mapping.IsMapped(2));

            Assert.True(mapping.TryGetEntry(1, out var first));
            Assert.Equal(1, first.PhysicalAddress);
            Assert.False(first.Inverted);
            Assert.Null(first.DefaultPosition);
        }

        [Fact]
        public void PrefixIsOptional()
        {
            var mapping = SwitchMapping.Parse("1:1:keep,2:2:keep");

            Assert.Empty(mapping.Errors);
            Assert.Equal(2, mapping.Entries.Count);
        }

        [Fact]
        public void LineBreaksSeparateEntries()
        {
            var mapping = SwitchMapping.Parse("switches:\n1:1:keep\n2:2:keep");

            Assert.Empty(mapping.Errors);
            Assert.Equal(2, mapping.Entries.Count);
        }

        [Theory]
        [InlineData("g", SwitchPosition.Straight)]
        [InlineData("straight", SwitchPosition.Straight)]
        [InlineData("r", SwitchPosition.Diverging)]
        [InlineData("diverging", SwitchPosition.Diverging)]
        public void ParsesDefaultPositionMarkers(string marker, SwitchPosition expected)
        {
            var mapping = SwitchMapping.Parse($"1:1:{marker}");

            Assert.Empty(mapping.Errors);
            Assert.True(mapping.TryGetEntry(1, out var entry));
            Assert.Equal(expected, entry.DefaultPosition);
        }

        [Theory]
        [InlineData("keep")]
        [InlineData("KEEP")]
        public void ParsesKeepDefault(string marker)
        {
            var mapping = SwitchMapping.Parse($"1:1:{marker}");

            Assert.Empty(mapping.Errors);
            Assert.True(mapping.TryGetEntry(1, out var entry));
            Assert.Null(entry.DefaultPosition);
        }

        [Fact]
        public void OmittedDefaultMeansKeep()
        {
            var mapping = SwitchMapping.Parse("1:1");

            Assert.Empty(mapping.Errors);
            Assert.True(mapping.TryGetEntry(1, out var entry));
            Assert.Null(entry.DefaultPosition);
            Assert.False(entry.Inverted);
        }

        [Fact]
        public void ParsesInvertedMapping()
        {
            var mapping = SwitchMapping.Parse("1:5:inverted:g");

            Assert.Empty(mapping.Errors);
            Assert.True(mapping.TryGetEntry(1, out var entry));
            Assert.True(entry.Inverted);
            Assert.Equal(5, entry.PhysicalAddress);
            Assert.Equal(SwitchPosition.Straight, entry.DefaultPosition);
        }

        [Fact]
        public void ToPhysical_WithoutInversion_KeepsThePosition()
        {
            var mapping = SwitchMapping.Parse("1:1:keep");
            mapping.TryGetEntry(1, out var entry);

            Assert.Equal(SwitchPosition.Straight, entry.ToPhysical(SwitchPosition.Straight));
            Assert.Equal(SwitchPosition.Diverging, entry.ToPhysical(SwitchPosition.Diverging));
        }

        [Fact]
        public void ToPhysical_WithInversion_SwapsThePosition()
        {
            var mapping = SwitchMapping.Parse("1:1:inverted:keep");
            mapping.TryGetEntry(1, out var entry);

            Assert.Equal(SwitchPosition.Diverging, entry.ToPhysical(SwitchPosition.Straight));
            Assert.Equal(SwitchPosition.Straight, entry.ToPhysical(SwitchPosition.Diverging));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void BlankConfigurationYieldsNoEntriesAndNoErrors(string? configuration)
        {
            var mapping = SwitchMapping.Parse(configuration);

            Assert.Empty(mapping.Entries);
            Assert.Empty(mapping.Errors);
            Assert.False(mapping.HasErrors);
        }

        [Theory]
        [InlineData("1")]
        [InlineData("1:x")]
        [InlineData("x:1")]
        [InlineData("0:1")]
        [InlineData("1:0")]
        [InlineData("1:1:banana")]
        [InlineData("1:1:keep:extra:more")]
        public void MalformedEntriesAreRejectedAndRecorded(string entry)
        {
            var mapping = SwitchMapping.Parse(entry);

            Assert.Empty(mapping.Entries);
            Assert.Single(mapping.Errors);
            Assert.True(mapping.HasErrors);
        }

        [Fact]
        public void MalformedEntryNeverBecomesAPlausibleMapping()
        {
            // A typo in the physical address must not silently map to something usable.
            var mapping = SwitchMapping.Parse("1:two:keep, 2:2:keep");

            Assert.Single(mapping.Errors);
            Assert.False(mapping.IsMapped(1));
            Assert.True(mapping.IsMapped(2));
        }

        [Fact]
        public void DuplicateEcosAddressIsAConflictAndKeepsTheFirstEntry()
        {
            var mapping = SwitchMapping.Parse("1:1:keep, 1:9:keep");

            Assert.Single(mapping.Errors);
            Assert.Single(mapping.Entries);
            Assert.True(mapping.TryGetEntry(1, out var entry));
            Assert.Equal(1, entry.PhysicalAddress);
        }

        [Fact]
        public void ValidEntriesSurviveAlongsideMalformedOnes()
        {
            var mapping = SwitchMapping.Parse("1:1:g, broken, 2:2:r");

            Assert.Single(mapping.Errors);
            Assert.Equal(2, mapping.Entries.Count);
        }
    }
}
