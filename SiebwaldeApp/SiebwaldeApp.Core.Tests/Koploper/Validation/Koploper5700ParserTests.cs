using System;
using System.Text;
using SiebwaldeApp.Core.Koploper.Validation;
using Xunit;

namespace SiebwaldeApp.Core.Tests.Koploper.Validation
{
    /// <summary>
    /// Unit tests for the 5700 external-information parsing: the field-level decoder
    /// (<see cref="Koploper5700Parser.TryParseFields"/>) and the incremental streaming parser
    /// (<see cref="Koploper5700StreamParser"/>). No network or file I/O is involved.
    /// </summary>
    public class Koploper5700ParserTests
    {
        private static readonly string[] GoldenFields = { "&4", "3", "00:50:47", "13:06:29", "Route onbekend" };

        [Fact]
        public void TryParseFields_GoldenFiveFieldRecord_ReturnsRecord()
        {
            bool ok = Koploper5700Parser.TryParseFields(GoldenFields, out Koploper5700Record? parsed);

            Assert.True(ok);
            Assert.NotNull(parsed);
            Assert.Equal(4, parsed.LocomotiveId);
            Assert.Equal(3, parsed.BlockId);
            Assert.Equal("00:50:47", parsed.ModelTime);
            Assert.Equal("13:06:29", parsed.PcTime);
            Assert.Equal("Route onbekend", parsed.Description);
        }

        [Fact]
        public void TryParseFields_Malformed_ReturnsFalse()
        {
            bool ok = Koploper5700Parser.TryParseFields(new[] { "not-a-loco", "not-a-block" }, out Koploper5700Record? parsed);

            Assert.False(ok);
            Assert.Null(parsed);
        }

        [Fact]
        public void Feed_TryDequeue_RoundTrips()
        {
            var parser = new Koploper5700StreamParser();
            byte[] bytes = BuildRecord("&4", "3", "00:50:47", "13:06:29", "Route onbekend");

            parser.Feed(bytes);

            Assert.True(parser.TryDequeue(out Koploper5700Record? parsed));
            Assert.NotNull(parsed);
            Assert.Equal(4, parsed.LocomotiveId);
            Assert.Equal(3, parsed.BlockId);
            Assert.Equal("00:50:47", parsed.ModelTime);
            Assert.Equal("13:06:29", parsed.PcTime);
            Assert.Equal("Route onbekend", parsed.Description);
            Assert.False(parser.TryDequeue(out _));
        }

        private static byte[] BuildRecord(params string[] fields)
        {
            var builder = new StringBuilder();
            foreach (string field in fields)
            {
                builder.Append(field);
                builder.Append((char)Koploper5700Parser.Separator);
            }

            return Encoding.ASCII.GetBytes(builder.ToString());
        }
    }
}
