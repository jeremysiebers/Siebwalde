using SiebwaldeApp.EcosEmu;
using Xunit;

namespace SiebwaldeApp.Core.Tests
{
    public class SimpleEcosCommandParserTests
    {
        private readonly SimpleEcosCommandParser _parser = new();

        [Fact]
        public void ParsesNameObjectIdAndOptions()
        {
            var command = _parser.Parse("set(1, speed)");

            Assert.NotNull(command);
            Assert.Equal("set", command!.Name);
            Assert.Equal(1, command.ObjectId);
            Assert.Equal(new[] { "speed" }, command.Options);
        }

        [Fact]
        public void ParsesCommandWithoutObjectId()
        {
            var command = _parser.Parse("queryObjects");

            Assert.NotNull(command);
            Assert.Equal("queryObjects", command!.Name);
            Assert.Null(command.ObjectId);
            Assert.Empty(command.Options);
        }

        [Fact]
        public void ParsesIdWithMultipleOptions()
        {
            var command = _parser.Parse("set(10, addr, 3)");

            Assert.NotNull(command);
            Assert.Equal("set", command!.Name);
            Assert.Equal(10, command.ObjectId);
            Assert.Equal(new[] { "addr", "3" }, command.Options);
        }

        [Fact]
        public void EmptyInput_ReturnsNull()
        {
            Assert.Null(_parser.Parse("   "));
        }

        [Fact]
        public void MalformedWithoutParens_KeepsRawAsName()
        {
            var command = _parser.Parse("garbage");

            Assert.NotNull(command);
            Assert.Equal("garbage", command!.Name);
            Assert.Null(command.ObjectId);
        }

        [Fact]
        public void QuotedComma_IsSplit_DocumentingCurrentLimitation()
        {
            // Documents current behavior: a comma inside a quoted value is still split.
            var command = _parser.Parse("set(1, \"a,b\")");

            Assert.NotNull(command);
            Assert.Equal(1, command!.ObjectId);
            Assert.Equal(new[] { "\"a", "b\"" }, command.Options);
        }
    }
}
