using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
        public int GetValue()
        {
            return this.Value;
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
}
