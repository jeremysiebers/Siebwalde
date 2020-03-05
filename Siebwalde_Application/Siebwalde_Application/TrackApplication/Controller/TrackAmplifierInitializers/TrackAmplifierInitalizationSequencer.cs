using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Siebwalde_Application
{
    /// <summary>
    /// This TrackAmplifierInitalizationSequencer initializes all the Track amplifiers and checks if firmware download is required
    /// </summary>
    public class TrackAmplifierInitalizationSequencer
    {
        #region Local variables

        private TrackApplicationVariables mTrackApplicationVariables;
        private Log2LoggingFile mTrackApplicationLogging;

        /// <summary>
        /// This enum holds all the possible states of the TrackAmplifierInitalizationSequencer statemachine
        /// </summary>
        private enum State { Idle, Reset, ConnectToEthernetTarget };
        private State State_Machine;
        private State SubMethodState;

        #endregion

        #region Constructor

        public TrackAmplifierInitalizationSequencer(Log2LoggingFile trackApplicationLogging, TrackApplicationVariables trackApplicationVariables)
        {
            mTrackApplicationVariables = trackApplicationVariables;
            mTrackApplicationLogging = trackApplicationLogging;

            State_Machine = State.Idle;
            SubMethodState = State.Idle;
        }

        #endregion

        #region Internal statemachine

        internal uint InitSequence()
        {
            uint returnval = Enums.Busy;

            switch (State_Machine)
            {
                case State.Reset:
                    // Here all sub classes reset methods are called in case of a forced reset
                    returnval = Enums.Finished;
                    break;

                case State.Idle:
                    // Here all manual commands are handled from the user
                    returnval = Enums.Finished;
                    break;

                case State.ConnectToEthernetTarget:
                    {
                        switch (ConnectToEthernetTarget())
                        {
                            case Enums.Busy:
                                {
                                    break;
                                }
                            case Enums.Finished:
                                {
                                    State_Machine = State.Idle;
                                    returnval = Enums.Finished;
                                    break;
                                }
                            case Enums.Error:
                                {
                                    State_Machine = State.Idle;
                                    returnval = Enums.Error;
                                    break;
                                }
                            default:
                                {
                                    State_Machine = State.Idle;
                                    break;
                                }
                        }
                        break;
                    }

                default:
                    break;
            }

            return returnval;
        }

        #endregion

        #region internal methods

        /// <summary>
        /// Method for setting up connection with EthernetTarget
        /// </summary>
        /// <returns></returns>
        private uint ConnectToEthernetTarget()
        {
            return Enums.Finished;
        }

        #endregion

    }
}
