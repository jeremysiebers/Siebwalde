using System.Collections.Generic;

namespace SiebwaldeApp.Core
{
    public class TrackApplicationVariables
    {
        #region Variables

        /// <summary>
        /// Variables of this class
        /// </summary>
        public const int MaxAmplifiers = 50;
        public List<TrackAmplifierItem> trackAmpItems;
        private TrackAmplifierItem trackAmp;
        public TrackControllerCommands trackControllerCommands;
        private TrackAmplifierWriteData[] trackAmpWriteItems;

        /// <summary>
        /// Pending write data per amplifier (key = SlaveNumber).
        /// This is filled from the application model / UI and
        /// consumed by TrackControlMain at 10 Hz.
        /// </summary>
        public Dictionary<ushort, TrackAmplifierWriteData> PendingWrites { get; } =
            new Dictionary<ushort, TrackAmplifierWriteData>();

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor to instantiate all variables used for trackcontrol
        /// </summary>
        public TrackApplicationVariables()
        {
            #region Instantiate List of TrackAmplifierItem and add items

            ushort[] HoldingRegInit = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            trackAmpItems = new List<TrackAmplifierItem>();

            for (ushort i = 0; i < 56; i++)
            {
                trackAmpItems.Add(trackAmp = new TrackAmplifierItem
                {
                    SlaveNumber = i,
                    SlaveDetected = 0,
                    HoldingReg = HoldingRegInit,
                    MbReceiveCounter = 0,
                    MbSentCounter = 0,
                    MbCommError = 0,
                    MbExceptionCode = 0,
                    SpiCommErrorCounter = 0
                });
            }

            trackAmpWriteItems = new TrackAmplifierWriteData[MaxAmplifiers];
            for (int i = 0; i < MaxAmplifiers; i++)
            {
                // Slave numbers 1..50
                trackAmpWriteItems[i] = new TrackAmplifierWriteData((ushort)(i + 1));
            }

            #endregion

            #region Instantiate TrackControllerCommands

            // TrackControllerCommands
            trackControllerCommands = new TrackControllerCommands();

            #endregion
        }

        #endregion

        #region Method GetAmplifierListing()

        /// <summary>
        /// Helper function to get amplifier list
        /// </summary>
        /// <returns></returns>
        public List<TrackAmplifierItem> GetAmplifierListing()
        {
            return trackAmpItems;
        }

        /// <summary>
        /// Initializes all known amplifiers with a default PWM set point
        /// in HoldingReg0 bits 0..9. This does not enable power; it only
        /// sets the idle value that will be used once the amplifiers are enabled.
        /// </summary>
        public void InitializeDefaultPwmSetpoints(ushort defaultPwm)
        {
            if (trackAmpItems == null || trackAmpItems.Count == 0)
                return;

            if (defaultPwm > 799)
                defaultPwm = 799;

            foreach (var amp in trackAmpItems)
            {
                var regs = amp.HoldingReg;
                if (regs == null || regs.Length == 0)
                    continue;

                ushort reg0 = regs[0];

                // Clear bits 0..9
                reg0 = (ushort)(reg0 & ~0x03FF);

                // Set default PWM
                reg0 |= (ushort)(defaultPwm & 0x03FF);

                regs[0] = reg0;
                amp.HoldingReg = regs;
            }
        }

        /// <summary>
        /// Builds the HoldingReg0 value from the desired PWM setpoint and EmoStop flag.
        /// Bits 0..9 : PWM setpoint (0..799)
        /// Bit 15    : EmoStop (1 = stop as fast as possible)
        /// Other bits are currently left as 0.
        /// </summary>
        private static ushort BuildHr0FromControl(int pwmSetpoint, bool emoStop)
        {
            int clampedPwm = pwmSetpoint;

            if (clampedPwm < 0) clampedPwm = 0;
            if (clampedPwm > 799) clampedPwm = 799;

            ushort value = (ushort)(clampedPwm & 0x03FF); // bits 0..9

            if (emoStop)
            {
                value |= (1 << 15);
            }

            // Future: direction, brake bits can be added here as needed.

            return value;
        }

        /// <summary>
        /// Updates the desired control values (PWM + EmoStop) for a given amplifier.
        /// This does not send anything immediately; it only marks data as pending
        /// in <see cref="PendingWrites"/>.
        /// A periodic writer in TrackControlMain will consume and send the values.
        /// </summary>
        public void SetDesiredAmplifierControl(ushort slaveNumber, int pwmSetpoint, bool emoStop)
        {
            if (slaveNumber == 0)
                return;

            ushort hr0 = BuildHr0FromControl(pwmSetpoint, emoStop);

            if (!PendingWrites.TryGetValue(slaveNumber, out var writeData))
            {
                writeData = new TrackAmplifierWriteData(slaveNumber);
                PendingWrites[slaveNumber] = writeData;
            }

            writeData.SetDesiredHr0(hr0);
        }
        #endregion
    }
}
