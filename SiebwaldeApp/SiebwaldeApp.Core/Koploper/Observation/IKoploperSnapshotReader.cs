namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// Reads a coherent snapshot of the Koploper internal state, together with its health and
    /// any observed diagnostics. No polling, background service or hardware actions.
    /// </summary>
    public interface IKoploperSnapshotReader
    {
        /// <summary>Locates, verifies, reads and decodes one coherent snapshot.</summary>
        KoploperSnapshotReadResult ReadSnapshot();
    }
}
