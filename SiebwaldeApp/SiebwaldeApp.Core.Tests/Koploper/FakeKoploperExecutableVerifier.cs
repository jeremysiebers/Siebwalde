using SiebwaldeApp.Core.Koploper;

namespace SiebwaldeApp.Core.Tests.Koploper
{
    /// <summary>
    /// <see cref="IKoploperExecutableVerifier"/> with a fixed result and observed identity.
    /// </summary>
    internal sealed class FakeKoploperExecutableVerifier : IKoploperExecutableVerifier
    {
        private readonly KoploperVersionGateResult _result;
        private readonly KoploperExecutableIdentity? _identity;

        public FakeKoploperExecutableVerifier(KoploperVersionGateResult result, KoploperExecutableIdentity? identity)
        {
            _result = result;
            _identity = identity;
        }

        public KoploperVersionGateResult Verify(string executablePath, out KoploperExecutableIdentity? observed)
        {
            observed = _identity;
            return _result;
        }
    }
}
