using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiebwaldeApp
{
    class LogFileBasics
    {
        public string getLogFile(string filename)
        {
            string file = SiebwaldeApp.Properties.Settings.Default.LogDirectory + DateTime.Now.Day + "-" + DateTime.Now.Month + "-" + DateTime.Now.Year + "_" + filename;
            return file;
        }
    }
}
