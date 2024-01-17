using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiebwaldeApp.Core
{ 
    public class StationControl
    {
        // Logger instance
        private string mLoggerInstance { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="LoggerInstance"></param>
        public StationControl(string LoggerInstance)
        {
            // couple and hold local variables                    
            mLoggerInstance = LoggerInstance;

            IoC.TrackVariables.PropertyChanged += TrackVar_PropertyChanged;
        }

        private void TrackVar_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            IoC.Logger.Log(e.PropertyName + " = " + sender.GetType().GetProperty(e.PropertyName).GetValue(sender).ToString(), mLoggerInstance);
        }
    }
}
