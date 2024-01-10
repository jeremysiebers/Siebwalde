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


        }
    }
}
