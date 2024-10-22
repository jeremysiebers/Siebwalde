using System;

namespace SiebwaldeApp.Core
{
    public class StationState
    {
        private State _state;

        /// <summary>
        /// constructor
        /// </summary>
        public StationState()
        {
            _state = new State();
        }

        public State CurrentState
        {
            get { return _state; }
            set { _state = value; }
        }

        public void ChangeState(State newState)
        {
            if (newState == null)
                throw new ArgumentNullException(nameof(newState));

            _state = newState;
        }

        public bool IsInState(State state)
        {
            return _state.Equals(state);
        }

        public void ResetState()
        {
            _state = new State();
        }
    }

    public class State
    {
        // State properties and methods go here
    }
}
