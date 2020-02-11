using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Collections;

namespace Siebwalde_Application
{
    /*#--------------------------------------------------------------------------#*/
    /*  Description: ASensor class interface 
     *               to set sensors received from target
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
    public abstract class ASensor
    {

        public delegate void StatusUpdate(int Value, bool ForceUpdate);
        public event StatusUpdate OnStatusUpdate = null;


        public void Attach(Sensor SensorUpdate)
        {
            OnStatusUpdate += new StatusUpdate(SensorUpdate.Update);
        }

        public void Detach(Sensor SensorUpdate)
        {
            OnStatusUpdate -= new StatusUpdate(SensorUpdate.Update);

        }

        public void Notify(int Value, bool ForceUpdate)
        {
            if (OnStatusUpdate != null)
            {
                OnStatusUpdate(Value, ForceUpdate);
            }
        }
    }

    public class SensorUpdater : ASensor
    {
        public void UpdateSensorValue(int Value, bool ForceUpdate)
        {
            Notify(Value, ForceUpdate);
        }
    }

    interface SensorUpdate
    {
        void Update(int NewSensorValue, bool NewForceUpdate);
    }

    public class Sensor : SensorUpdate
    {
        //Name of the Sensor
        string name;
        //String to be written when updated
        string LogString;
        //Variable to hold actual value
        int Value;
        //When variable has changed generate an event
        Action<string, int, string> m_OnChangedAction;

        public Sensor(string name, string LogString, int Value, Action<string, int, string> OnChangedAction)
        {
            this.name = name;
            this.LogString = LogString;
            this.Value = Value;
            m_OnChangedAction = OnChangedAction;
        }

        public void Update(int NewSensorValue, bool ForceUpdate)
        {
            if (this.Value != NewSensorValue && ForceUpdate != true) //When variable is not equal to stored variable
            {
                this.Value = NewSensorValue;
                m_OnChangedAction(this.name, this.Value, this.LogString);
            }
            else if (ForceUpdate == true)
            {
                this.Value = NewSensorValue;
                m_OnChangedAction(this.name, this.Value, this.LogString);
            }
        }

    }

    /*#--------------------------------------------------------------------------#*/
    /*  Description: BActuator class interface 
     *               to set actuators and send command to target
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
    public abstract class BActuator
    {

        public delegate void StatusUpdate();
        public event StatusUpdate OnStatusUpdate = null;

        public void Attach(Actuator ActuatorUpdate)
        {
            OnStatusUpdate += new StatusUpdate(ActuatorUpdate.Update);
        }

        public void Detach(Actuator ActuatorUpdate)
        {
            OnStatusUpdate -= new StatusUpdate(ActuatorUpdate.Update);
        }

        public void Notify()
        {
            if (OnStatusUpdate != null)
            {
                OnStatusUpdate();
            }
        }
    }

    public class ActuatorUpdater : BActuator
    {
        public void UpdateActuator()
        {
            Notify();
        }
    }

    interface ActuatorUpdate
    {
        void Update();
    }

    public class Actuator : ActuatorUpdate
    {
        //Name of the Actuator
        string name;        
        //Cmd to be send
        string cmd;
        //When variable has changed generate an event
        Action<string, string> m_OnChangedAction;

        public Actuator(string name, string cmd, Action<string, string> OnChangedAction)
        {
            this.name = name;            
            this.cmd = cmd;
            m_OnChangedAction = OnChangedAction;
        }

        public void Update()
        {
            m_OnChangedAction(this.name, this.cmd);
        }
    }

    /*#--------------------------------------------------------------------------#*/
    /*  Description: Message class interface 
     *               to push messages
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
    public abstract class CMessage
    {

        public delegate void StatusUpdate();
        public event StatusUpdate OnStatusUpdate = null;

        public void Attach(Message MessageUpdate)
        {
            OnStatusUpdate += new StatusUpdate(MessageUpdate.Update);
        }

        public void Detach(Message MessageUpdate)
        {
            OnStatusUpdate -= new StatusUpdate(MessageUpdate.Update);
        }

        public void Notify()
        {
            if (OnStatusUpdate != null)
            {
                OnStatusUpdate();
            }
        }
    }

    public class MessageUpdater : CMessage
    {
        public void UpdateMessage()
        {
            Notify();
        }
    }

    interface MessageUpdate
    {
        void Update();
    }

    public class Message : MessageUpdate
    {
        //Name of Message
        string name;
        //String to be written when updated
        string logString;
        //When variable has changed generate an event
        Action<string, string> m_OnChangedAction;

        public Message(string name, string logString, Action<string, string> OnChangedAction)
        {
            this.name = name;
            this.logString = logString;
            m_OnChangedAction = OnChangedAction;
        }

        public void Update()
        {
            m_OnChangedAction(this.name, this.logString);
        }
    }

    /*#--------------------------------------------------------------------------#*/
    /*  Description: Command class interface 
     *               to push commands to Application
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
    public abstract class DCommand
    {

        public delegate void StatusUpdate();
        public event StatusUpdate OnStatusUpdate = null;

        public void Attach(Command CommandUpdate)
        {
            OnStatusUpdate += new StatusUpdate(CommandUpdate.Update);
        }

        public void Detach(Command CommandUpdate)
        {
            OnStatusUpdate -= new StatusUpdate(CommandUpdate.Update);
        }

        public void Notify()
        {
            if (OnStatusUpdate != null)
            {
                OnStatusUpdate();
            }
        }
    }

    public class CommandUpdater : DCommand
    {
        public void UpdateCommand()
        {
            Notify();
        }
    }

    interface CommandUpdate
    {
        void Update();
    }

    public class Command : CommandUpdate
    {
        //Command to application
        string Cmd;        
        //When variable has changed generate an event
        Action<string> m_OnChangedAction;

        public Command(string Cmd, Action<string> OnChangedAction)
        {
            this.Cmd = Cmd;            
            m_OnChangedAction = OnChangedAction;
        }

        public void Update()
        {
            m_OnChangedAction(this.Cmd);
        }
    }


    /*#--------------------------------------------------------------------------#*/
    /*  Description: Collor class interface 
     *               to push coller settings to Application
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
    public abstract class EColor
    {

        public delegate void StatusUpdate(Color NewColor);
        public event StatusUpdate OnStatusUpdate = null;


        public void Attach(Colorc ColorUpdate)
        {
            OnStatusUpdate += new StatusUpdate(ColorUpdate.Update);
        }

        public void Detach(Colorc ColorUpdate)
        {
            OnStatusUpdate -= new StatusUpdate(ColorUpdate.Update);

        }

        public void Notify(Color NewColor)
        {
            if (OnStatusUpdate != null)
            {
                OnStatusUpdate(NewColor);
            }
        }
    }

    public class ColorUpdater : EColor
    {
        public void UpdateColorValue(Color NewColor)
        {
            Notify(NewColor);
        }
    }

    interface ColorUpdate
    {
        void Update(Color NewColor);
    }

    public class Colorc : ColorUpdate
    {
        //Name of the Sensor
        Color m_color;
        //String to be written when updated
        string LogString;
        //When variable has changed generate an event
        Action<Color, string> m_OnChangedAction;

        public Colorc(Color NewColor, string LogString, Action<Color, string> OnChangedAction)
        {
            this.m_color = NewColor;
            this.LogString = LogString;
            m_OnChangedAction = OnChangedAction;
        }

        public void Update(Color NewColorValue)
        {
            if (this.m_color != NewColorValue) //When variable is not equal to stored variable
            {
                this.m_color = NewColorValue;
                m_OnChangedAction(this.m_color, this.LogString);
            }            
        }

    }

    /*#--------------------------------------------------------------------------#*/
    /*  Description: TrackAmplifier class interface 
     *               to set TrackAmplifier info received from target
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
    public abstract class FTrackAmplifier
    {

        public delegate void StatusUpdate(UInt16 MbHeader, UInt16 SlaveNumber, UInt16 SlaveDetected, UInt16[] HoldingReg,
            UInt16 MbReceiveCounter, UInt16 MbSentCounter, UInt32 MbCommError, UInt16 MbExceptionCode,
            UInt16 SpiCommErrorCounter, UInt16 MbFooter);
        public event StatusUpdate OnStatusUpdate = null;


        public void Attach(TrackAmplifier TrackAmplifierUpdate)
        {
            OnStatusUpdate += new StatusUpdate(TrackAmplifierUpdate.Update);
        }

        public void Detach(TrackAmplifier TrackAmplifierUpdate)
        {
            OnStatusUpdate -= new StatusUpdate(TrackAmplifierUpdate.Update);

        }

        public void Notify(UInt16 MbHeader, UInt16 SlaveNumber, UInt16 SlaveDetected, UInt16[] HoldingReg,
            UInt16 MbReceiveCounter, UInt16 MbSentCounter, UInt32 MbCommError, UInt16 MbExceptionCode,
            UInt16 SpiCommErrorCounter, UInt16 MbFooter)
        {
            if (OnStatusUpdate != null)
            {
                OnStatusUpdate(MbHeader, SlaveNumber, SlaveDetected, HoldingReg,
            MbReceiveCounter, MbSentCounter, MbCommError, MbExceptionCode,
            SpiCommErrorCounter, MbFooter);
            }
        }
    }

    public class TrackAmplifierUpdater : FTrackAmplifier
    {
        public void UpdateTrackAmplifier(UInt16 MbHeader, UInt16 SlaveNumber, UInt16 SlaveDetected, UInt16[] HoldingReg,
            UInt16 MbReceiveCounter, UInt16 MbSentCounter, UInt32 MbCommError, UInt16 MbExceptionCode,
            UInt16 SpiCommErrorCounter, UInt16 MbFooter)
        {
            Notify(MbHeader, SlaveNumber, SlaveDetected, HoldingReg,
            MbReceiveCounter, MbSentCounter, MbCommError, MbExceptionCode,
            SpiCommErrorCounter, MbFooter);
        }
    }

    interface TrackAmplifierUpdate
    {
        void Update(UInt16 MbHeader, UInt16 SlaveNumber, UInt16 SlaveDetected, UInt16[] HoldingReg,
            UInt16 MbReceiveCounter, UInt16 MbSentCounter, UInt32 MbCommError, UInt16 MbExceptionCode,
            UInt16 SpiCommErrorCounter, UInt16 MbFooter);
    }

    public class TrackAmplifier : TrackAmplifierUpdate
    {
        public UInt16 MbHeader { get; set; }
        public UInt16 SlaveNumber { get; set; }
        public UInt16 SlaveDetected { get; set; }
        public UInt16[] HoldingReg = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, };
        public UInt16 MbReceiveCounter { get; set; }
        public UInt16 MbSentCounter { get; set; }
        public UInt32 MbCommError { get; set; }
        public UInt16 MbExceptionCode { get; set; }
        public UInt16 SpiCommErrorCounter { get; set; }
        public UInt16 MbFooter { get; set; }

        //When variable has changed generate an event
        Action<UInt16, UInt16, UInt16, UInt16[], UInt16, UInt16, UInt32, UInt16, UInt16, UInt16> m_OnChangedAction;

        public TrackAmplifier(UInt16 MbHeader, UInt16 SlaveNumber, UInt16 SlaveDetected, UInt16[] HoldingReg, 
            UInt16 MbReceiveCounter, UInt16 MbSentCounter, UInt32 MbCommError, UInt16 MbExceptionCode, 
            UInt16 SpiCommErrorCounter, UInt16 MbFooter, 
            Action<UInt16, UInt16, UInt16, UInt16[], UInt16, UInt16, UInt32, UInt16, UInt16, UInt16> OnChangedAction)
        {
            this.MbHeader = MbHeader;
            this.SlaveNumber = SlaveNumber;
            this.SlaveDetected = SlaveDetected;
            Array.Copy(HoldingReg, 0, this.HoldingReg, 0, HoldingReg.Length);
            this.MbReceiveCounter = MbReceiveCounter;
            this.MbSentCounter = MbSentCounter;
            this.MbCommError = MbCommError;
            this.MbExceptionCode = MbExceptionCode;
            this.SpiCommErrorCounter = SpiCommErrorCounter;
            this.MbFooter = MbFooter;
            m_OnChangedAction = OnChangedAction;
        }

        public void Update(UInt16 MbHeader, UInt16 SlaveNumber, UInt16 SlaveDetected, UInt16[] HoldingReg,
            UInt16 MbReceiveCounter, UInt16 MbSentCounter, UInt32 MbCommError, UInt16 MbExceptionCode,
            UInt16 SpiCommErrorCounter, UInt16 MbFooter)
        {
            bool SentNotification = false;
            IStructuralEquatable se1 = this.HoldingReg;

            if (this.MbHeader != MbHeader)
            {
                this.MbHeader = MbHeader;
                SentNotification = true;
                
            }
            if (this.SlaveNumber != SlaveNumber)
            {
                this.SlaveNumber = SlaveNumber;
                SentNotification = true;
            }
            if (this.SlaveDetected != SlaveDetected)
            {
                this.SlaveDetected = SlaveDetected;
                SentNotification = true;
            }
            if (false == se1.Equals(HoldingReg, StructuralComparisons.StructuralEqualityComparer))
            {
                Array.Copy(HoldingReg, 0, this.HoldingReg, 0, HoldingReg.Length);
                SentNotification = true;
            }
            if (this.MbReceiveCounter != MbReceiveCounter)
            {
                this.MbReceiveCounter = MbReceiveCounter;
                SentNotification = true;
            }
            if (this.MbSentCounter != MbSentCounter)
            {
                this.MbSentCounter = MbSentCounter;
                SentNotification = true;
            }
            if (this.MbCommError != MbCommError)
            {
                this.MbCommError = MbCommError;
                SentNotification = true;
            }
            if (this.MbExceptionCode != MbExceptionCode)
            {
                this.MbExceptionCode = MbExceptionCode;
                SentNotification = true;
            }
            if (this.SpiCommErrorCounter != SpiCommErrorCounter)
            {
                this.SpiCommErrorCounter = SpiCommErrorCounter;
                SentNotification = true;
            }
            if (this.MbFooter != MbFooter)
            {
                this.MbFooter = MbFooter;
                SentNotification = true;
            }

            if(true == SentNotification)
            {
                m_OnChangedAction(this.MbHeader, this.SlaveNumber, this.SlaveDetected, this.HoldingReg,
                    this.MbReceiveCounter, this.MbSentCounter, this.MbCommError, this.MbExceptionCode,
                    this.SpiCommErrorCounter, this.MbFooter);
            }
        }        
    }

    /*#--------------------------------------------------------------------------#*/
    /*  Description: EthernetTarget class interface 
     *               to receive messages from Ethernet target
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
    public abstract class GEthernetTargetMessage
    {

        public delegate void StatusUpdate(UInt16 taskid, UInt16 taskcommand, UInt16 taskstate, UInt16 taskmessage);
        public event StatusUpdate OnStatusUpdate = null;


        public void Attach(EthernetTargetMessage EthernetTargetMessageUpdate)
        {
            OnStatusUpdate += new StatusUpdate(EthernetTargetMessageUpdate.Update);
        }

        public void Detach(EthernetTargetMessage EthernetTargetMessageUpdate)
        {
            OnStatusUpdate -= new StatusUpdate(EthernetTargetMessageUpdate.Update);

        }

        public void Notify(UInt16 taskid, UInt16 taskcommand, UInt16 taskstate, UInt16 taskmessage)
        {
            if (OnStatusUpdate != null)
            {
                OnStatusUpdate(taskid, taskcommand, taskstate, taskmessage);
            }
        }
    }

    public class EthernetTargetMessageUpdater : GEthernetTargetMessage
    {
        public void UpdateEthernetTargetMessage(UInt16 taskid, UInt16 taskcommand, UInt16 taskstate, UInt16 taskmessage)
        {
            Notify(taskid, taskcommand, taskstate, taskmessage);
        }
    }

    interface EthernetTargetMessageUpdate
    {
        void Update(UInt16 taskid, UInt16 taskcommand, UInt16 taskstate, UInt16 taskmessage);
    }

    public class EthernetTargetMessage : EthernetTargetMessageUpdate
    {
        UInt16 taskid;
        UInt16 taskcommand;
        UInt16 taskstate;
        UInt16 taskmessage;

        //When variable has changed generate an event
        Action<UInt16, UInt16, UInt16, UInt16> m_OnChangedAction;

        public EthernetTargetMessage(UInt16 taskid, UInt16 taskcommand, UInt16 taskstate, UInt16 taskmessage,
            Action<UInt16, UInt16, UInt16, UInt16> OnChangedAction)
        {
            this.taskid = taskid;
            this.taskcommand = taskcommand;
            this.taskstate = taskstate;
            this.taskmessage = taskmessage;            
            m_OnChangedAction = OnChangedAction;
        }

        public void Update(UInt16 taskid, UInt16 taskcommand, UInt16 taskstate, UInt16 taskmessage)
        {
            bool SentNotification = false;

            if (this.taskid != taskid)
            {
                this.taskid = taskid;
                SentNotification = true;

            }
            if (this.taskcommand != taskcommand)
            {
                this.taskcommand = taskcommand;
                SentNotification = true;
            }
            if (this.taskstate != taskstate)
            {
                this.taskstate = taskstate;
                SentNotification = true;
            }
            if (this.taskmessage != taskmessage)
            {
                this.taskmessage = taskmessage;
                SentNotification = true;
            }
            
            if (true == SentNotification)
            {
                m_OnChangedAction(this.taskid, this.taskcommand, this.taskstate, this.taskmessage);
            }
        }

    }
}
