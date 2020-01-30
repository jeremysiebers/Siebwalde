using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Siebwalde_Application.TrackApplication.Data
{
    public interface iTrackIOHandle
    {
        void ActuatorCmd(string name, string cmd);
    }

    public class TrackIOHandle : iTrackIOHandle
    {   
        public Sender mTrackSender;
        public Receiver mTrackReceiver;
        public Services.PublicEnums mPublicEnums;
        public Model.TrackApplicationVariables mTrackApplicationVariables;

        public int mTrackSendingPort;
        public int mTrackReceivingPort;

        /*#--------------------------------------------------------------------------#*/
        /*  Description: TrackIOHandle
         *               Constructor
         *              
         *               
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
         */
        /*#--------------------------------------------------------------------------#*/
        public TrackIOHandle(Services.PublicEnums publicEnums, int TrackReceivingPort, int TrackSendingPort, Model.TrackApplicationVariables trackApplicationVariables)
        {
            mPublicEnums = publicEnums;
            mTrackReceivingPort = TrackReceivingPort;
            mTrackSendingPort = TrackSendingPort;
            mTrackApplicationVariables = trackApplicationVariables;

            mTrackReceiver = new Receiver(mTrackReceivingPort);
            mTrackSender = new Sender(mPublicEnums.TrackTarget());
        }
        
        /*#--------------------------------------------------------------------------#*/
        /*  Description: IO Handle start
         *               to  Couple  real target to application to get real sensor feedback
         *               or to  Couple  simulator output back to application
         *               Also reset target/simulator to achieve known startup, target
         *               maybe already be running/initialized
         *               
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
         */
        /*#--------------------------------------------------------------------------#*/

        public void Start()
        {            
            mTrackSender.ConnectUdp(mTrackSendingPort);
            mTrackReceiver.NewData += HandleNewData;
            mTrackReceiver.Start();
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: ActuatorCmd
         *               Sends all commands from FYApplication to real target or
         *               to simulator.
         *               
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
         *  Notes      : Kicked by this application
         */
        /*#--------------------------------------------------------------------------#*/
        public void ActuatorCmd(string name, string cmd)
        {
            mTrackSender.SendUdp(Encoding.ASCII.GetBytes(cmd));            
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: HandleNewData
         *               to handle new data from target or from simulator
         *               
         *               
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
         */
        /*#--------------------------------------------------------------------------#*/
        public void HandleNewData(byte[] b)
        {
            string _b = Encoding.UTF8.GetString(b, 0, b.Length);        // convert received byte array to string array 
            
            var stream = new MemoryStream(b);
            var reader = new BinaryReader(stream);

            UInt16 Header = reader.ReadByte();
            UInt16 Sender = reader.ReadByte(); // and is also taskid
            if (Header == mPublicEnums.Header() && Sender == mPublicEnums.SlaveInfo())
            {
                UInt16 MbHeader = reader.ReadByte();
                UInt16 SlaveNumber = reader.ReadByte();
                UInt16 SlaveDetected = reader.ReadByte();
                UInt16 Padding = reader.ReadByte();

                UInt16[] HoldingReg = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, };
                for (int i = 0; i < 12; i++)
                {
                    HoldingReg[i] = reader.ReadUInt16();
                }

                UInt16 MbReceiveCounter = reader.ReadUInt16();
                UInt16 MbSentCounter = reader.ReadUInt16();

                UInt32 MbCommError = reader.ReadUInt32();

                UInt16 MbExceptionCode = reader.ReadByte();
                UInt16 SpiCommErrorCounter = reader.ReadByte();
                UInt16 MbFooter = reader.ReadByte();

                mTrackApplicationVariables.TrackAmplifierInt[SlaveNumber].UpdateTrackAmplifier(MbHeader, SlaveNumber, SlaveDetected, HoldingReg, 
                MbReceiveCounter, MbSentCounter, MbCommError, MbExceptionCode, SpiCommErrorCounter, MbFooter);
            }
            else if (Header == mPublicEnums.Header())
            {
                UInt16 taskcommand = reader.ReadByte();
                UInt16 taskstate = reader.ReadByte();
                UInt16 taskmessage = reader.ReadByte();

                mTrackApplicationVariables.EthTargetMessage.UpdateEthernetTargetMessage(Sender, taskcommand, taskstate, taskmessage);
            }
            
            //m_iMTCtrl.MTLinkActivityUpdate();
        }
                
    }    
}