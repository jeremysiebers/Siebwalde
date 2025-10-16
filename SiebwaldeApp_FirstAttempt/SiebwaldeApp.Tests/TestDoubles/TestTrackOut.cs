// SiebwaldeApp.Tests/TestDoubles/TestTrackOut.cs
using System.Collections.Generic;
using SiebwaldeApp.Core;

namespace SiebwaldeApp.Tests
{
    public sealed class TestTrackOut : ITrackOut
    {
        public record Cmd(string Name, object[] Args);
        public List<Cmd> Sent { get; } = new();

        private void Add(string name, params object[] args) => Sent.Add(new Cmd(name, args));

        public void SetAmplifierStop(int trackNumber, bool stop) => Add(nameof(SetAmplifierStop), trackNumber, stop);
        public void SetSignalEntry(bool isTopSide, bool green) => Add(nameof(SetSignalEntry), isTopSide, green);
        public void SetSignalExit(bool isTopSide, bool green) => Add(nameof(SetSignalExit), isTopSide, green);
        public void SetSwitch(int switchId, SwitchPosition position) => Add(nameof(SetSwitch), switchId, position);
        public void StopBeforeStation(bool isTopSide) => Add(nameof(StopBeforeStation), isTopSide);
    }
}
