using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiebwaldeApp
{
    public class EnableTrackamplifiers : IAmplifierInitializersBaseClass
    {
        #region Local variables
        // Hold the Track IO Handle instance
        private TrackIOHandle mTrackIOHandle;
        // Switch-case variable
        private uint SubMethodState { get; set; }
        // Message conatiner for sending messages
        private SendMessage mSendMessage;
        // Logger instance
        private string mLoggerInstance { get; set; }
        // The name of the class
        public string Name { get; set; }

        #endregion

        #region Constructor
        /// <summary>
        /// Setup the Connect to Ethernet target
        /// </summary>
        public EnableTrackamplifiers(string LoggerInstance, TrackIOHandle trackIOHandle)
        {
            // Hold the Track IO Handle instance
            mTrackIOHandle = trackIOHandle;

            // Hold the logger instance
            mLoggerInstance = LoggerInstance;

            // init the switch-case
            SubMethodState = 0;

            // Create dummy data container
            byte[] DummyData = new byte[80];

            // Create Sendmessage container
            mSendMessage = new SendMessage(0, DummyData);

            //Set the name of the class for referencing during execution from list
            Name = this.GetType().Name;
        }
        #endregion

        #region public Method

        /// <summary>
        /// Call until finished returns
        /// </summary>
        /// <param name="receivedMessage"></param>
        /// <returns></returns>
        public (uint, string) Execute(ReceivedMessage receivedMessage)
        {
            uint returnval = Enums.Busy;
            string CallNext = "";

            switch (SubMethodState)
            {
                case 0:
                    {
                        mSendMessage.Command = TrackCommand.EXEC_MBUS_STATE_SLAVE_ENABLE;
                        mTrackIOHandle.ActuatorCmd(mSendMessage);
                        SubMethodState += 1;
                        IoC.Logger.Log("State.EnableTrackamplifiers => EXEC_MBUS_STATE_SLAVE_ENABLE.", mLoggerInstance);
                        break;
                    }
                case 1:
                    {
                        if (receivedMessage.TaskId == TaskId.MBUS &&
                            receivedMessage.Taskcommand == EnumMbusStatus.MBUS_STATE_SLAVE_ENABLE &&
                            receivedMessage.Taskstate == TaskStates.DONE)
                        {
                            IoC.Logger.Log("State.EnableTrackamplifiers => MBUS_STATE_SLAVE_ENABLE DONE.", mLoggerInstance);
                            SubMethodState = 0;
                            returnval = Enums.Finished;
                            CallNext = "DONE";
                        }
                        break;
                    }
                default:
                    {
                        SubMethodState = 0;
                        returnval = Enums.Error;
                        break;
                    }
            }

            return (returnval, CallNext);
        }

        #endregion
    }
}
