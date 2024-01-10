
using System;

namespace SiebwaldeApp.Core
{
    class NewLogFileBasics
    {
        public string getLogFile(string filename)
        {
            string file = "C:\\Localdata\\Siebwalde\\Logging\\" + DateTime.Now.Day + "-" + DateTime.Now.Month + "-" + DateTime.Now.Year + "_" + filename;
            return file;
        }
    }
}