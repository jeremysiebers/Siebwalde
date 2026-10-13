using System.Collections.Generic;
using SiebwaldeApp.Core;
using SiebwaldeApp.Core.Koploper;

namespace SiebwaldeApp.Integration
{
    /// <summary>
    /// Routes the escalation flags of a <see cref="CarrierDecision"/> into the existing
    /// <see cref="ControlSafetyGuard"/> safety latch.
    ///
    /// <c>LayoutStopRequired</c> (the KIS-07 shadow is invalid/unavailable) is interpreted by the
    /// authority lifecycle: only a <see cref="AuthorityLifecycleState.AuthorityLostAfterValid"/>
    /// (N+X-qualified) shadow loss becomes a latched layout-wide stop. During
    /// <see cref="AuthorityLifecycleState.AuthoritySuspect"/> and
    /// <see cref="AuthorityLifecycleState.AuthorityNotYetObserved"/> the same projector output must
    /// NOT latch a permanent layout stop: Suspect is a transient glitch (continuity, non-latching)
    /// and NotYetObserved is "startup not ready" (fail closed, non-latching).
    ///
    /// Each <see cref="CarrierDecision.LatchedLocoIds"/> DCC address becomes a locomotive-scoped stop.
    /// This reuses the production safety latch and does not invent a second safety domain.
    /// </summary>
    public static class CarrierEscalation
    {
        public static void Route(
            CarrierDecision decision,
            ControlSafetyGuard guard,
            AuthorityLifecycleState authorityLifecycle)
        {
            if (decision is null || guard is null)
            {
                return;
            }

            if (decision.LayoutStopRequired)
            {
                if (authorityLifecycle == AuthorityLifecycleState.AuthorityLostAfterValid)
                {
                    guard.Apply(new ControlDiagnostic
                    {
                        Code = DiagnosticCode.StateUnknown,
                        Severity = DiagnosticSeverity.StopRequired,
                        Subject = "carrier-layout-stop",
                        LocoAddress = null,
                        Detail = "Ownership-aware carrier requires a layout stop (KIS authority lost after being valid)."
                    });
                }
                else if (authorityLifecycle == AuthorityLifecycleState.AuthoritySuspect)
                {
                    // Transient suspect: fail closed without a latched fault. A Suspect decision
                    // carrying LayoutStopRequired is only possible on a boundary cycle; it must
                    // never latch a permanent layout stop.
                    guard.Notify(new ControlDiagnostic
                    {
                        Code = DiagnosticCode.StateUnknown,
                        Severity = DiagnosticSeverity.Warning,
                        Subject = "carrier-authority-suspect",
                        LocoAddress = null,
                        Detail = "KIS_AUTHORITY_SUSPECT: previously-valid KIS shadow is transiently invalid; movement is frozen without a latched fault."
                    });
                }
                else
                {
                    // Startup not-ready: fail closed without a latched fault. The shadow is
                    // invalid but no valid shadow has ever been observed, so this is not an
                    // operational authority loss and must not become a permanent layout stop.
                    guard.Notify(new ControlDiagnostic
                    {
                        Code = DiagnosticCode.StateUnknown,
                        Severity = DiagnosticSeverity.Warning,
                        Subject = "carrier-authority-not-ready",
                        LocoAddress = null,
                        Detail = "KIS_AUTHORITY_NOT_READY: no valid authoritative KIS shadow observed yet; movement denied without a latched fault."
                    });
                }
            }

            foreach (int dcc in decision.LatchedLocoIds)
            {
                guard.Apply(new ControlDiagnostic
                {
                    Code = DiagnosticCode.OccupancyMismatch,
                    Severity = DiagnosticSeverity.StopRequired,
                    Subject = $"carrier-loco-stop-{dcc}",
                    LocoAddress = dcc,
                    Detail = "Ownership-aware carrier requires a locomotive stop (reserved section unexpectedly occupied)."
                });
            }
        }
    }
}
