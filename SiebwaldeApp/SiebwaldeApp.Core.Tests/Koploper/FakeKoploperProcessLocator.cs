using SiebwaldeApp.Core.Koploper;
using SiebwaldeApp.Koploper.Windows;

namespace SiebwaldeApp.Core.Tests.Koploper
{
    /// <summary>
    /// <see cref="IKoploperProcessLocator"/> with a configurable <see cref="TryLocate"/> result.
    /// <see cref="IsRestart"/> delegates to the production <see cref="WindowsKoploperProcessLocator"/>
    /// so the restart predicate keeps a single source of truth; process-lifetime tests target the
    /// production type directly.
    /// </summary>
    internal sealed class FakeKoploperProcessLocator : IKoploperProcessLocator
    {
        private readonly KoploperProcessStatus _status;
        private readonly KoploperProcessInfo? _info;
        private readonly WindowsKoploperProcessLocator _restartDelegate = new();

        public FakeKoploperProcessLocator(KoploperProcessStatus status, KoploperProcessInfo? info)
        {
            _status = status;
            _info = info;
        }

        public KoploperProcessStatus TryLocate(out KoploperProcessInfo? info)
        {
            info = _info;
            return _status;
        }

        public bool IsRestart(KoploperProcessInfo? previous, KoploperProcessInfo current)
            => _restartDelegate.IsRestart(previous, current);
    }
}
