using System;
using SiebwaldeApp.Core;
using SiebwaldeApp.EcosEmu;

namespace SiebwaldeApp.Integration
{
    /// <summary>
    /// Safety stop implementation over the existing hardware backend. It deliberately uses only
    /// the mechanisms the ECoS path already uses:
    ///
    /// - per locomotive: <see cref="IHardwareBackend.SetLocoSpeed"/> with ECoS speed 0, which the
    ///   backend maps to the neutral amplifier setpoint (standstill);
    /// - whole layout: <see cref="IHardwareBackend.SetPower"/>(false), the same central
    ///   power-off that Koploper's <c>set(1,stop)</c> triggers.
    ///
    /// No second locomotive-control path is introduced. The backend is bound after composition
    /// because it is created by the integration that also needs the switch controller.
    /// </summary>
    public sealed class EcosHardwareStopSink : ISafetyStopSink
    {
        private readonly Action<string>? _log;

        public EcosHardwareStopSink(Action<string>? log = null)
        {
            _log = log;
        }

        /// <summary>The backend the stop is issued through. Set once composition has finished.</summary>
        public IHardwareBackend? Hardware { get; set; }

        /// <inheritdoc />
        public bool StopLoco(int address)
        {
            if (Hardware is null)
            {
                _log?.Invoke($"Safety stop for loco {address} not delivered: no hardware backend is bound.");
                return false;
            }

            // Direction is irrelevant for a stop: ECoS speed 0 maps to the neutral setpoint for
            // either direction.
            Hardware.SetLocoSpeed(address, 0, 0);
            _log?.Invoke($"Safety stop: loco {address} set to ECoS speed 0 (neutral).");
            return true;
        }

        /// <inheritdoc />
        public void StopLayout()
        {
            if (Hardware is null)
            {
                _log?.Invoke("Safety stop for the layout not delivered: no hardware backend is bound.");
                return;
            }

            Hardware.SetPower(false);
            _log?.Invoke("Safety stop: layout power off (all mapped amplifiers to neutral).");
        }
    }
}
