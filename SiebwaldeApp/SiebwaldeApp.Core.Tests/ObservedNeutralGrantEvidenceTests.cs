using System;
using System.Linq;
using SiebwaldeApp.Core;
using Xunit;

namespace SiebwaldeApp.Core.Tests
{
    /// <summary>
    /// Regression tests for the observed-neutral grant race.
    ///
    /// Movement permission may only be granted when every amplifier in the effective safety domain
    /// reports a genuine, current, protocol SLAVEINFO readback of the neutral PWM (399). An
    /// initialized/default <c>HoldingReg[0] = 399</c> must never be interpreted as a protocol
    /// observation, even when a fresh timestamp (from an unrelated frame) is present.
    ///
    /// These tests exercise the Core-level predicate directly, so they are deterministic and do not
    /// depend on scheduler luck.
    /// </summary>
    public class ObservedNeutralGrantEvidenceTests
    {
        private static readonly byte[] Domain = { 1, 2, 3 };
        private static readonly DateTimeOffset Now = new DateTimeOffset(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);
        private static readonly TimeSpan StaleAfter = TrackAmplifierDataFreshness.DefaultStaleAfter;

        private static TrackApplicationVariables NewVariables() => new TrackApplicationVariables();

        private static TrackAmplifierItem Amp(TrackApplicationVariables variables, byte slave)
            => variables.trackAmpItems.First(a => a.SlaveNumber == slave);

        /// <summary>Stamps a genuine, current SLAVEINFO readback (as the comm client does).</summary>
        private static void RecordProtocolReadback(TrackAmplifierItem amplifier, ushort hr0, DateTimeOffset at)
        {
            amplifier.SlaveDetected = 1;
            amplifier.HoldingReg[0] = hr0;
            amplifier.LastDataReceivedUtc = at;
            amplifier.ProtocolReadbackObserved = true;
        }

        private static bool Observed(TrackApplicationVariables variables)
            => TrackAmplifierNeutralObservation.AreAllObservedNeutral(
                variables.trackAmpItems, Domain, Now, StaleAfter);

        [Fact]
        public void DefaultHoldingRegNeutral_WithoutProtocolReadback_IsNotObserved()
        {
            var variables = NewVariables();
            variables.InitializeDefaultPwmSetpoints(AmplifierSpeedMapper.NeutralPwm);

            // The in-memory HR0 is 399, but there is no genuine protocol echo.
            Assert.Equal(AmplifierSpeedMapper.NeutralPwm, Amp(variables, 1).HoldingReg[0] & 0x03FF);

            Assert.False(Observed(variables));
        }

        [Fact]
        public void InitializeDefaultPwmSetpoints_ClearsProtocolReadbackMarker()
        {
            var variables = NewVariables();

            // A prior genuine readback is present...
            RecordProtocolReadback(Amp(variables, 1), AmplifierSpeedMapper.NeutralPwm, Now);
            Assert.True(Amp(variables, 1).ProtocolReadbackObserved);

            // ...and the local default write invalidates it, because it overwrites the readback.
            variables.InitializeDefaultPwmSetpoints(AmplifierSpeedMapper.NeutralPwm);

            Assert.False(Amp(variables, 1).ProtocolReadbackObserved);
            Assert.Equal(AmplifierSpeedMapper.NeutralPwm, Amp(variables, 1).HoldingReg[0] & 0x03FF);
            Assert.False(Observed(variables));
        }

        [Fact]
        public void Fresh399Echo_ForOnlyPartOfDomain_IsNotObserved()
        {
            var variables = NewVariables();
            RecordProtocolReadback(Amp(variables, 1), AmplifierSpeedMapper.NeutralPwm, Now);

            // Amplifiers 2 and 3 have no readback at all.
            Assert.False(Observed(variables));
        }

        [Fact]
        public void FreshNonNeutralEcho_ForOneAmplifier_IsNotObserved()
        {
            var variables = NewVariables();
            foreach (var slave in Domain)
            {
                RecordProtocolReadback(Amp(variables, slave), AmplifierSpeedMapper.NeutralPwm, Now);
            }

            // One amplifier reports a fresh, genuine, but non-neutral readback.
            RecordProtocolReadback(Amp(variables, 2), 500, Now);

            Assert.False(Observed(variables));
        }

        [Fact]
        public void FreshNeutralEcho_ForAllAmplifiers_IsObserved()
        {
            var variables = NewVariables();
            foreach (var slave in Domain)
            {
                RecordProtocolReadback(Amp(variables, slave), AmplifierSpeedMapper.NeutralPwm, Now);
            }

            Assert.True(Observed(variables));
        }

        [Fact]
        public void Stale399FromPreviousRuntime_IsNotObserved()
        {
            var variables = NewVariables();

            // A 399 value with a fresh-looking timestamp but NO protocol marker: the equivalent of a
            // value that would be carried forward from a previous runtime without genuine evidence.
            var amp = Amp(variables, 1);
            amp.SlaveDetected = 1;
            amp.HoldingReg[0] = AmplifierSpeedMapper.NeutralPwm;
            amp.LastDataReceivedUtc = Now;
            // amp.ProtocolReadbackObserved deliberately remains false.

            Assert.False(Observed(variables));
        }

        [Fact]
        public void Restart_InvalidatesPriorObservation_UntilNewGenuineEchoes()
        {
            // Prior runtime: every amplifier observed neutral.
            var prior = NewVariables();
            foreach (var slave in Domain)
            {
                RecordProtocolReadback(Amp(prior, slave), AmplifierSpeedMapper.NeutralPwm, Now);
            }
            Assert.True(TrackAmplifierNeutralObservation.AreAllObservedNeutral(
                prior.trackAmpItems, Domain, Now, StaleAfter));

            // A restart creates a fresh container: prior observation evidence does not carry over.
            var fresh = NewVariables();
            Assert.False(TrackAmplifierNeutralObservation.AreAllObservedNeutral(
                fresh.trackAmpItems, Domain, Now, StaleAfter));

            // New genuine neutral echoes after the restart re-establish the observation.
            foreach (var slave in Domain)
            {
                RecordProtocolReadback(Amp(fresh, slave), AmplifierSpeedMapper.NeutralPwm, Now);
            }
            Assert.True(TrackAmplifierNeutralObservation.AreAllObservedNeutral(
                fresh.trackAmpItems, Domain, Now, StaleAfter));
        }

        [Fact]
        public void OriginalRace_DefaultNeutralWrittenAfterFreshFrame_IsNotObserved()
        {
            // Deterministic reproduction of the original race WITHOUT scheduler luck:
            //  1. a genuine (DetectSlaves) frame is parsed first: detected + fresh, but HR0 = 0,
            //  2. SetDefaultPwmSetpointsStep then writes the in-memory default 399 OVER the readback,
            //     leaving the (still fresh) timestamp in place,
            //  3. IsNeutralObserved must NOT grant from that stale timestamp + in-memory 399.
            var variables = NewVariables();

            foreach (var slave in Domain)
            {
                var amp = Amp(variables, slave);
                amp.SlaveDetected = 1;
                amp.HoldingReg = new ushort[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                amp.LastDataReceivedUtc = Now;
                amp.ProtocolReadbackObserved = true;
            }

            variables.InitializeDefaultPwmSetpoints(AmplifierSpeedMapper.NeutralPwm);

            // The default 399 is present and the timestamp is still fresh, but the readback marker
            // was invalidated, so it is not a genuine neutral observation.
            Assert.Equal(AmplifierSpeedMapper.NeutralPwm, Amp(variables, 1).HoldingReg[0] & 0x03FF);
            Assert.False(Amp(variables, 1).ProtocolReadbackObserved);

            Assert.False(Observed(variables));
        }
    }
}
