using System.Collections.Generic;
using SiebwaldeApp.Core.Koploper;

namespace SiebwaldeApp.Core.Tests.Koploper
{
    /// <summary>
    /// <see cref="IKoploperProcessLocator"/> with a scripted sequence of <see cref="TryLocate"/>
    /// results: an explicit queue is drained first, then a configurable fallback is used for any
    /// remaining calls. <see cref="IsRestart"/> uses the same PID + start-time predicate as the
    /// production locator.
    /// </summary>
    internal sealed class ScriptedKoploperProcessLocator : IKoploperProcessLocator
    {
        private readonly Queue<(KoploperProcessStatus Status, KoploperProcessInfo? Info)> _queue = new();
        private (KoploperProcessStatus Status, KoploperProcessInfo? Info)? _fallback;

        /// <summary>Number of <see cref="TryLocate"/> calls observed.</summary>
        public int CallCount { get; private set; }

        public void Enqueue(KoploperProcessStatus status, KoploperProcessInfo? info) => _queue.Enqueue((status, info));

        public void SetFallback(KoploperProcessStatus status, KoploperProcessInfo? info) => _fallback = (status, info);

        public KoploperProcessStatus TryLocate(out KoploperProcessInfo? info)
        {
            CallCount++;

            if (_queue.Count > 0)
            {
                (KoploperProcessStatus status, KoploperProcessInfo? queued) = _queue.Dequeue();
                info = queued;
                return status;
            }

            var fallback = _fallback ?? (KoploperProcessStatus.NotFound, null);
            info = fallback.Info;
            return fallback.Status;
        }

        public bool IsRestart(KoploperProcessInfo? previous, KoploperProcessInfo current)
            => previous is null
                || previous.ProcessId != current.ProcessId
                || previous.ProcessStartTimeUtc != current.ProcessStartTimeUtc;
    }
}
