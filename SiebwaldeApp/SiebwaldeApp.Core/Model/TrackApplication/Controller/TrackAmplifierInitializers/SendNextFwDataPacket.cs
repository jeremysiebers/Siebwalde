using SiebwaldeApp.Core;
using System.Collections.Generic;
using System.Threading;
using static SiebwaldeApp.Core.FiddleYardSimulatorVariables;

namespace SiebwaldeApp
{
    /// <summary>
    /// This class is used to send the whole flash microcontroller program memory to the Ethernet target used for flashing
    /// </summary>
    public class SendNextFwDataPacket
    {
        #region Local variables
        // Message conatiner for sending messages
        private SendMessage mSendMessage;
        // Set the recovery iteration counter
        private int IterationCounter { get; set; }
        // Set the Boot loader helper
        private TrackAmplifierBootloaderHelpers mTrackAmplifierBootloaderHelpers;
        private ITrackCommClient trackCommClient;
        private TrackAmplifierBootloaderHelpers bootloaderHelpers;

        // Set the Track IO handle
        private readonly ITrackCommClient _commClient;
        #endregion

        /// <summary>
        /// Instaniate
        /// </summary>
        public SendNextFwDataPacket(ITrackCommClient commClient, 
            TrackAmplifierBootloaderHelpers trackAmplifierBootloaderHelpers)
        {
            // Hold the Track IO Handle instance
            _commClient = commClient ?? throw new ArgumentNullException(nameof(commClient));

            // Hold the TrackAmplifierBootloaderHelpers
            mTrackAmplifierBootloaderHelpers = trackAmplifierBootloaderHelpers;

            // Create dummy data container
            byte[] DummyData = new byte[80];
            // Create Sendmessage container
            mSendMessage = new SendMessage(0, DummyData);
            // Set the iteration counter
            IterationCounter = 0;

        }

        /// <summary>
        /// Call to send the uController flash program data to the Ethernet target
        /// </summary>
        public void Execute()
        {
            mSendMessage.Command = TrackCommand.FILEDOWNLOAD_STATE_FW_DATA_RECEIVE;

            List<byte> Data = new List<byte>();

            for (int i = IterationCounter; i < (IterationCounter + Enums.JUMPSIZE); i++)
            {
                foreach (byte val in mTrackAmplifierBootloaderHelpers.GetHexFileData[i][1])
                {
                    Data.Add(val);
                }
            }
            mSendMessage.Data = Data.ToArray();
            //mTrackIOHandle.ActuatorCmd(mSendMessage);
            _commClient.SendAsync(mSendMessage, cancellationToken).ConfigureAwait(false);

            //Console.WriteLine("Send Package " + (IterationCounter + 1).ToString() + " to Ethernet target.");

            IterationCounter += Enums.JUMPSIZE;
        }
    }    
}
