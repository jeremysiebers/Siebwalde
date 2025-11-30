using System.Collections.Generic;

namespace SiebwaldeApp.Core
{
    public class TrackApplicationVariables
    {
        #region Variables

        /// <summary>
        /// Variables of this class
        /// </summary>
        public List<TrackAmplifierItem> trackAmpItems;
        private TrackAmplifierItem trackAmp;
        public TrackControllerCommands trackControllerCommands;

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


        #endregion
    }
}
