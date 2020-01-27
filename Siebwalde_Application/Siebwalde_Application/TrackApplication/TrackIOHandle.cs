using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Siebwalde_Application.TrackApplication
{
    public interface iTrackIOHandle
    {
        void ActuatorCmd(string name, string cmd);
    }

    public class TrackIOHandle : iTrackIOHandle
    {
        public TrackIOHandleVariables MTIOHandleVar;
        public TrackApplication MTApp;
        public iTrackController m_iMTCtrl; // connect variable to TrackController class for defined interfaces

        public TrackAmplifierUpdater[] TrackAmplifierInt;
        public TrackAmplifier[] TrackAmplifiers;
        public EthernetTargetMessageUpdater EthTargetMessage;

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
        public TrackIOHandle(iTrackController iMTCtrl)
        {
            m_iMTCtrl = iMTCtrl;                            // connect to MTController interface, save interface in variable
            MTIOHandleVar = new TrackIOHandleVariables();            
            MTApp = new TrackApplication(MTIOHandleVar, this);

            UInt16[] HoldingRegInit = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            TrackAmplifiers = new TrackAmplifier[56];
                       
            TrackAmplifierInt = new TrackAmplifierUpdater[56];
            
            for (UInt16 i = 0; i < 56; i++)
            {
                TrackAmplifierInt[i] = new TrackAmplifierUpdater();
            }
            
            for (UInt16 i = 0; i < 56; i++)
            {
                TrackAmplifiers[i] = new TrackAmplifier(TrackIOHandleVariables.HEADER, i, 0, HoldingRegInit, 0, 0, 0, 0, 0, TrackIOHandleVariables.FOOTER,
                    (MbHeader, SlaveNumber, SlaveDetected, HoldingReg, MbReceiveCounter, MbSentCounter, MbCommError, MbExceptionCode,
                    SpiCommErrorCounter, MbFooter) => test(MbHeader, SlaveNumber, SlaveDetected, HoldingReg, MbReceiveCounter, MbSentCounter, MbCommError, MbExceptionCode,
                    SpiCommErrorCounter, MbFooter));
                TrackAmplifierInt[i].Attach(TrackAmplifiers[i]);
            }

            EthTargetMessage = new EthernetTargetMessageUpdater();
            EthernetTargetMessage EthTargetMessages = new EthernetTargetMessage(0, 0, 0, 0, (taskid, taskcommand, taskstate, taskmessage) => test2(taskid, taskcommand, taskstate, taskmessage));
            EthTargetMessage.Attach(EthTargetMessages);
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
            m_iMTCtrl.GetMTReceiver().NewData += HandleNewData;            
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
            m_iMTCtrl.GetMTSender().SendUdp(Encoding.ASCII.GetBytes(cmd));            
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
            if (Header == TrackIOHandleVariables.HEADER && Sender == TrackIOHandleVariables.SLAVEINFO)
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

                TrackAmplifierInt[SlaveNumber].UpdateTrackAmplifier(MbHeader, SlaveNumber, SlaveDetected, HoldingReg, 
                MbReceiveCounter, MbSentCounter, MbCommError, MbExceptionCode, SpiCommErrorCounter, MbFooter);
            }
            else if (Header == TrackIOHandleVariables.HEADER)
            {
                UInt16 taskcommand = reader.ReadByte();
                UInt16 taskstate = reader.ReadByte();
                UInt16 taskmessage = reader.ReadByte();

                EthTargetMessage.UpdateEthernetTargetMessage(Sender, taskcommand, taskstate, taskmessage);
            }
            
            //m_iMTCtrl.MTLinkActivityUpdate();
        }

        public void test(ushort mbHeader, ushort slaveNumber, ushort slaveDetected, ushort[] holdingReg, ushort mbReceiveCounter, ushort mbSentCounter, uint mbCommError, ushort mbExceptionCode, ushort spiCommErrorCounter, ushort mbFooter)
        {
            //Console.WriteLine("Slave number " + slaveNumber.ToString() + " updated.");
        }

        public void test2(UInt16 taskid, UInt16 taskcommand, UInt16 taskstate, UInt16 taskmessage)
        {
            //
        }
    }    
}