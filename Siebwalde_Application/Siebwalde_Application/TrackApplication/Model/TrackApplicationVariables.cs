using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Siebwalde_Application.TrackApplication.Model
{
    public class TrackApplicationVariables
    {
        public Services.PublicEnums mPublicEnums;
        public Data.TrackIOHandle mTrackIoHandle;

        /*#--------------------------------------------------------------------------#*/
        /*  Description: Track Application variables
         * 
         *  Input(s)   :
         *
         *  Output(s)  : 
         *
         *  Returns    :
         *
         *  Pre.Cond.  :
         *
         *  Post.Cond. :
         *
         *  Notes      : 
         *  
         */
        /*#--------------------------------------------------------------------------#*/
        public int mTrackSendingPort;
        public int mTrackReceivingPort;

        public TrackAmplifierUpdater[] TrackAmplifierInt;
        public TrackAmplifier[] TrackAmplifiers;
        public EthernetTargetMessageUpdater EthTargetMessage;

        /*#--------------------------------------------------------------------------#*/
        /*  Description: TrackVariables constructor
         * 
         *  Input(s)   :
         *
         *  Output(s)  : 
         *
         *  Returns    :
         *
         *  Pre.Cond.  :
         *
         *  Post.Cond. :
         *
         *  Notes      : 
         *  
         */
        /*#--------------------------------------------------------------------------#*/
        public TrackApplicationVariables(Services.PublicEnums publicEnums, int TrackReceivingPort, int TrackSendingPort)
        {
            mPublicEnums = publicEnums;
            mTrackReceivingPort = TrackReceivingPort;
            mTrackSendingPort = TrackSendingPort;
            /*
             * Init the Data
             */
            mTrackIoHandle = new Data.TrackIOHandle(mPublicEnums, mTrackReceivingPort, mTrackSendingPort, this);

            /*
             * Init the variables
             */
            UInt16[] HoldingRegInit = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            TrackAmplifiers = new TrackAmplifier[56];

            TrackAmplifierInt = new TrackAmplifierUpdater[56];

            for (UInt16 i = 0; i < 56; i++)
            {
                TrackAmplifierInt[i] = new TrackAmplifierUpdater();
            }

            for (UInt16 i = 0; i < 56; i++)
            {
                TrackAmplifiers[i] = new TrackAmplifier(mPublicEnums.Header(), i, 0, HoldingRegInit, 0, 0, 0, 0, 0, mPublicEnums.Footer(),
                    (MbHeader, SlaveNumber, SlaveDetected, HoldingReg, MbReceiveCounter, MbSentCounter, MbCommError, MbExceptionCode,
                    SpiCommErrorCounter, MbFooter) => Test(MbHeader, SlaveNumber, SlaveDetected, HoldingReg, MbReceiveCounter, MbSentCounter, MbCommError, MbExceptionCode,
                    SpiCommErrorCounter, MbFooter));
                TrackAmplifierInt[i].Attach(TrackAmplifiers[i]);
            }

            EthTargetMessage = new EthernetTargetMessageUpdater();
            EthernetTargetMessage EthTargetMessages = new EthernetTargetMessage(0, 0, 0, 0, (taskid, taskcommand, taskstate, taskmessage) => Test2(taskid, taskcommand, taskstate, taskmessage));
            EthTargetMessage.Attach(EthTargetMessages);
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: TrackVariables Start
         * 
         *  Input(s)   :
         *
         *  Output(s)  : 
         *
         *  Returns    :
         *
         *  Pre.Cond.  :
         *
         *  Post.Cond. :
         *
         *  Notes      : 
         *  
         */
        /*#--------------------------------------------------------------------------#*/
        public void Start()
        {
            mTrackIoHandle.Start();
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: TrackVariables Methods
         * 
         *  Input(s)   :
         *
         *  Output(s)  : 
         *
         *  Returns    :
         *
         *  Pre.Cond.  :
         *
         *  Post.Cond. :
         *
         *  Notes      : 
         *  
         */
        /*#--------------------------------------------------------------------------#*/
        public void Test(ushort mbHeader, ushort slaveNumber, ushort slaveDetected, ushort[] holdingReg, ushort mbReceiveCounter, ushort mbSentCounter, uint mbCommError, ushort mbExceptionCode, ushort spiCommErrorCounter, ushort mbFooter)
        {
            Console.WriteLine("Slave number " + slaveNumber.ToString() + " updated.");
        }

        public void Test2(UInt16 taskid, UInt16 taskcommand, UInt16 taskstate, UInt16 taskmessage)
        {
            //
        }
    }
}
