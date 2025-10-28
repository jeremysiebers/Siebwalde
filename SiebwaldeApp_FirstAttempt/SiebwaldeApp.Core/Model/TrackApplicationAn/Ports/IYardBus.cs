// File: Core/Ports/IYardBus.cs
namespace SiebwaldeApp.Core
{
    using System;

    // YARD PIC -> DOMAIN (events)
    public interface IYardIn
    {
        event Action<YardMergeRequestEvent> MergeRequest;           // operator requests to insert a train to mainline
        event Action<YardSignalFeedbackEvent> YardSignalChanged;    // optional feedback
        event Action<HardwareAliveEvent> HardwareAliveChanged;
    }

    // DOMAIN -> YARD PIC (commands)
    public interface IYardOut
    {
        void SetYardSignal(bool mergeSignalGreen);
        void SetYardSwitch(int switchId, bool position);
        void AuthorizeMerge(bool authorize); // allow yard to dispatch the requested freight to mainline
    }

    public readonly record struct YardMergeRequestEvent(bool RequestGrantedByOperator, DateTime Utc);
    public readonly record struct YardSignalFeedbackEvent(bool MergeSignalGreen, DateTime Utc);
}
