using System;
using SiebwaldeApp.Core;
using SiebwaldeApp.Core.Koploper;
using SiebwaldeApp.Core.TrackApplication.Topology;
using SiebwaldeApp.Koploper.Windows;

namespace SiebwaldeApp.Integration
{
    /// <summary>
    /// Composes the KIS-07 read pipeline (Windows process locator + memory reader + snapshot reader +
    /// reservation observer + logical-section shadow observer) and wires it into a
    /// <see cref="Kis09PassiveObserver"/>. This is composition only — it creates NO comm client,
    /// transport, or session guard; the observer reads the production
    /// <see cref="TrackApplicationVariables"/> container. The Windows memory reader is the read-only
    /// Koploper-process reader (KIS-07), NOT a PIC32 client.
    /// </summary>
    public static class Kis09PassiveObserverFactory
    {
        public static Kis09PassiveObserver Create(
            TrackApplicationVariables variables,
            LayoutProfile profile,
            Func<DateTimeOffset>? clock = null,
            KoploperSnapshotReadOptions? snapshotReadOptions = null,
            KoploperReservationObserverOptions? reservationOptions = null,
            KoploperLogicalSectionShadowObserverOptions? shadowOptions = null,
            LogicalPhysicalReconciliationObserverOptions? reconciliationOptions = null,
            TimeSpan? physicalStaleAfter = null)
        {
            ArgumentNullException.ThrowIfNull(variables);
            ArgumentNullException.ThrowIfNull(profile);

            Func<DateTimeOffset> sharedClock = clock ?? (() => DateTimeOffset.UtcNow);

            IKoploperMemoryLayout layout = Koploper94MemoryLayout.Instance;
            IKoploperProcessLocator processLocator = new WindowsKoploperProcessLocator();
            IKoploperExecutableVerifier executableVerifier = new KoploperExecutableVerifier(layout);
            Func<IKoploperMemoryReader> memoryReaderFactory = () => new WindowsKoploperMemoryReader();
            IKoploperRawObjectGraphReader rawGraphReader =
                new KoploperRawObjectGraphReader(layout, new KoploperDecodePlausibility());
            IKoploperBlockStateDecoder blockStateDecoder = new KoploperBlockStateDecoder();

            IKoploperSnapshotReader reservationReader = new KoploperSnapshotReader(
                processLocator, executableVerifier, memoryReaderFactory, rawGraphReader, blockStateDecoder,
                snapshotReadOptions, sharedClock);
            var reservationObserver = new KoploperReservationObserver(reservationReader, reservationOptions, sharedClock);

            IKoploperSnapshotReader shadowReader = new KoploperSnapshotReader(
                processLocator, executableVerifier, memoryReaderFactory, rawGraphReader, blockStateDecoder,
                snapshotReadOptions, sharedClock);
            var shadowObserver = new KoploperLogicalSectionShadowObserver(
                shadowReader,
                reservationObserver,
                profile.Name,
                profile.ToKoploperBlockBindingMap(),
                shadowOptions,
                sharedClock);

            return new Kis09PassiveObserver(
                variables,
                profile,
                reservationObserver,
                shadowObserver,
                reconciliationOptions,
                sharedClock,
                physicalStaleAfter);
        }
    }
}
