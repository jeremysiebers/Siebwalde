using System;
using System.IO;
using System.Text;
using System.Threading;

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Represents ONE atomically-acquired exclusive track-controller session owner.
    ///
    /// The track-controller session (the real UDP transport bound to
    /// <see cref="CoreConfiguration.TrackControllerReceivingPort"/>) must have exactly one owner
    /// process at a time: the WPF <c>SiebwaldeApp</c> runtime and the command-131 CLI
    /// (<c>SiebwaldeApp.FlashOneConsole</c>) are mutually exclusive. This lease is the primary,
    /// cross-process authority for that exclusivity, closing the time-of-check/time-of-use race in
    /// the read-only ownership preflight (process name + UDP listener table) that runs before the
    /// UDP bind.
    ///
    /// Acquisition is a single non-blocking <see cref="Mutex.WaitOne(int)"/> on a FIXED named
    /// mutex. The named mutex is automatically abandoned by the OS if the owning process terminates
    /// abruptly, so an unexpected exit always releases ownership — no watchdog thread is needed.
    /// A small owner file under <see cref="CoreConfiguration.LogDirectory"/> records the current
    /// owner's identity for operator-facing diagnostics only; the mutex is the sole authority.
    /// </summary>
    public sealed class TrackControllerSessionLease : IDisposable
    {
        /// <summary>Fixed cross-process mutex name (session scope, single user session).</summary>
        private const string MutexName = @"Local\Siebwalde.TrackController.Session";

        /// <summary>Owner-file name written under <see cref="CoreConfiguration.LogDirectory"/>.</summary>
        private const string OwnerFileName = ".siebwalde-track-controller-owner";

        /// <summary>Owner identity of the WPF control application (SiebwaldeApp.csproj).</summary>
        public const string WpfOwnerName = "SiebwaldeApp";

        /// <summary>Owner identity of the command-131 CLI (SiebwaldeApp.FlashOneConsole).</summary>
        public const string FlashOneConsoleOwnerName = "SiebwaldeApp.FlashOneConsole";

        private readonly Mutex _mutex;
        private readonly Thread _ownerThread;
        private readonly ManualResetEventSlim _acquireCompleted = new(false);
        private readonly ManualResetEventSlim _release = new(false);

        private volatile bool _acquired;
        private int _disposed;

        /// <summary>
        /// Starts the dedicated owner thread and blocks until it reports the (immediate, non-blocking)
        /// acquisition result in <see cref="Acquired"/>. A <see cref="Mutex"/> is thread-affine
        /// (only the acquiring thread may release it), so a single owner thread performs BOTH the
        /// acquire and the release for the lease lifetime — the async caller is free to hop threads
        /// across its <c>ConfigureAwait(false)</c> continuations. This owner thread is a lock owner,
        /// NOT a watchdog: it performs no monitoring, timeouts or recovery.
        /// </summary>
        private TrackControllerSessionLease(Mutex mutex)
        {
            _mutex = mutex;
            _ownerThread = new Thread(OwnerThreadBody)
            {
                IsBackground = true,
                Name = "Siebwalde.TrackControllerSessionOwner",
            };
            _ownerThread.Start();

            // WaitOne(0) is immediate, so this returns almost instantly once the thread starts.
            _acquireCompleted.Wait();
        }

        /// <summary>True once the owner thread has acquired the mutex.</summary>
        private bool Acquired => _acquired;

        /// <summary>
        /// Atomically acquires the exclusive controller-session lease, or returns null when another
        /// process already holds it. When acquisition fails, <paramref name="blockingOwner"/> carries
        /// the persisted owner string of the holder (best-effort; null when it cannot be read).
        /// </summary>
        public static TrackControllerSessionLease? TryAcquire(string ownerName, out string? blockingOwner)
        {
            blockingOwner = null;

            if (string.IsNullOrWhiteSpace(ownerName))
            {
                throw new ArgumentNullException(nameof(ownerName));
            }

            Mutex mutex;
            try
            {
                // createdNew is informational only: opening an existing named mutex returns false,
                // creating a new one returns true. Neither case acquires ownership — that happens
                // exclusively via the owner thread's non-blocking WaitOne(0) below.
                mutex = new Mutex(initiallyOwned: false, MutexName, out _);
            }
            catch
            {
                // Cannot even open/create the named mutex (e.g. an ACL mismatch on a stale mutex):
                // fail closed and report no blocking owner (it cannot be determined).
                return null;
            }

            var lease = new TrackControllerSessionLease(mutex);
            if (!lease.Acquired)
            {
                // Another session holds the mutex: the owner thread failed WaitOne(0). Tear the
                // (unowned) lease down and report the blocking owner best-effort.
                lease.Dispose();
                blockingOwner = ReadOwnerBestEffort();
                return null;
            }

            // We own the mutex: record our identity for operator-facing diagnostics. This write is
            // atomic (FileShare.None) and best-effort — the mutex, not the file, is the authority.
            TryWriteOwner(BuildOwnerString(ownerName));

            return lease;
        }

        /// <summary>Releases the mutex (on its owner thread) and removes the owner file. Idempotent.</summary>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return; // already disposed
            }

            try
            {
                // Remove the owner file only if WE actually acquired (and therefore wrote) it. A
                // lease that failed to acquire must never delete the successful holder's owner file,
                // or a subsequent best-effort read of the blocking owner would come back empty.
                // Deleting BEFORE releasing the mutex also means we cannot delete a successor's
                // freshly-written file: while we still own it, no successor can exist yet.
                if (_acquired)
                {
                    TryDeleteOwner();
                }
            }
            finally
            {
                // Wake the owner thread so it releases the mutex, then wait for the release to
                // complete so a subsequent TryAcquire cannot race a still-held mutex.
                _release.Set();
                _ownerThread.Join();
            }
        }

        private void OwnerThreadBody()
        {
            try
            {
                try
                {
                    _acquired = _mutex.WaitOne(0);
                }
                catch (AbandonedMutexException)
                {
                    // The previous owner died without releasing. WaitOne on an abandoned named mutex
                    // transfers ownership to this thread and then signals the abandonment, so we ARE
                    // now the owner. Treat it as acquired so a crashed session never wedges the
                    // controller.
                    _acquired = true;
                }
                catch
                {
                    _acquired = false;
                }

                _acquireCompleted.Set();

                if (!_acquired)
                {
                    return; // finally disposes the unowned mutex
                }

                // Hold the mutex for the whole lease lifetime, then release it on THIS thread.
                _release.Wait();
            }
            finally
            {
                try
                {
                    if (_acquired)
                    {
                        _mutex.ReleaseMutex();
                    }
                }
                catch
                {
                    // Not owned / abandoned; the OS reclaims the named mutex when this thread exits.
                }

                _mutex.Dispose();
            }
        }

        private static string BuildOwnerString(string ownerName)
            => $"{ownerName} (PID {Environment.ProcessId}, {DateTimeOffset.UtcNow:O})";

        private static string OwnerFilePath
            => Path.Combine(CoreConfiguration.LogDirectory, OwnerFileName);

        /// <summary>
        /// Writes the owner string to the shared side-channel file. <see cref="FileShare.None"/>
        /// makes the write exclusive/atomic, and the stream is closed immediately so a blocker (or a
        /// later diagnostic reader) can read it back. Best-effort: any failure is swallowed because
        /// the mutex remains the authority and the file is only operator-facing diagnostics.
        /// </summary>
        private static void TryWriteOwner(string owner)
        {
            try
            {
                var path = OwnerFilePath;
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                using var stream = new FileStream(
                    path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
                stream.SetLength(0);
                using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                writer.Write(owner);
                writer.Flush();
                stream.Flush(flushToDisk: true);
            }
            catch
            {
                // Diagnostic side-channel only; the mutex remains the sole authority.
            }
        }

        /// <summary>Reads the persisted owner string, or null when it is absent/unreadable.</summary>
        private static string? ReadOwnerBestEffort()
        {
            try
            {
                var path = OwnerFilePath;
                if (!File.Exists(path))
                {
                    return null;
                }

                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(stream, Encoding.UTF8);
                var text = reader.ReadToEnd();
                return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
            }
            catch
            {
                return null;
            }
        }

        private static void TryDeleteOwner()
        {
            try
            {
                var path = OwnerFilePath;
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // Best-effort cleanup; a stale owner file is harmless (diagnostic only).
            }
        }
    }
}
