using System;

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// One immutable diagnostic event. Carries structured context so the UI never has to parse
    /// log text, and records which safety action (if any) was taken.
    /// </summary>
    public sealed class ControlDiagnostic
    {
        /// <summary>What kind of divergence or condition this is.</summary>
        public DiagnosticCode Code { get; init; }

        /// <summary>How serious it is, and therefore what the control path does.</summary>
        public DiagnosticSeverity Severity { get; init; }

        /// <summary>Short identifier of the affected element, for example "switch 1".</summary>
        public string Subject { get; init; } = "";

        /// <summary>Factual technical description; not UI wording.</summary>
        public string Detail { get; init; } = "";

        /// <summary>Affected locomotive address, when the condition is tied to one.</summary>
        public int? LocoAddress { get; init; }

        /// <summary>Affected block, when applicable.</summary>
        public int? Block { get; init; }

        /// <summary>Affected ECoS/physical switch address, when applicable.</summary>
        public int? SwitchAddress { get; init; }

        /// <summary>When the condition was observed.</summary>
        public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.Now;

        /// <summary>The safety action that was taken for this diagnostic.</summary>
        public SafetyAction SafetyAction { get; init; } = SafetyAction.None;

        /// <summary>
        /// Identity used to latch a persistent fault so the same fault does not trigger the
        /// same safety action over and over.
        /// </summary>
        public string Key => $"{Code}|{Subject}";

        /// <summary>Creates a copy with the safety action recorded, after it was applied.</summary>
        public ControlDiagnostic WithSafetyAction(SafetyAction action) => new()
        {
            Code = Code,
            Severity = Severity,
            Subject = Subject,
            Detail = Detail,
            LocoAddress = LocoAddress,
            Block = Block,
            SwitchAddress = SwitchAddress,
            Timestamp = Timestamp,
            SafetyAction = action
        };

        public override string ToString()
            => $"{Timestamp:HH:mm:ss} {Severity} {Code} [{Subject}] {Detail}" +
               (SafetyAction == SafetyAction.None ? "" : $" (action: {SafetyAction})");
    }
}
