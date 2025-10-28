
namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Minimal fake for the Yard PIC interfaces.
    /// </summary>
    public sealed class FakeYardAdapter : IYardIn, IYardOut, IDisposable
    {
        // ---- IYardIn events (no-op) ----
        public event Action<YardMergeRequestEvent> MergeRequest { add { } remove { } }
        public event Action<YardSignalFeedbackEvent> YardSignalChanged { add { } remove { } }
        public event Action<HardwareAliveEvent> HardwareAliveChanged { add { } remove { } }

        // ---- IYardOut commands (no-op) ----
        public void SetYardSignal(bool mergeSignalGreen) { }
        public void SetYardSwitch(int switchId, bool position) { }
        public void AuthorizeMerge(bool authorize) { }

        public void Dispose() { }
    }
}
