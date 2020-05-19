using System;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.ComponentModel;

namespace SiebwaldeApp
{
    public interface ILogger
    {
        /// <summary>
        /// Logs the specified message
        /// </summary>
        /// <param name="sender">The sender application part of the message</param>
        /// <param name="text">The message to be logged</param>
        /// <param name="logLevel">The log level of the messgae</param
        void Log(string sender, string text, LogLevel logLevel = LogLevel.Verbose);

        /// <summary>
        /// Defines the log level
        /// </summary>
        LogLevel LogLevel { get; set; }

        /// <summary>
        /// The event that is fired when any child property changes it value
        /// </summary>
        event PropertyChangedEventHandler PropertyChanged;

    }

    #region LogLevel

    public enum LogLevel
    {
        Verbose = 0,

        Debug = 1,

        Critical = 2
    }

    #endregion

    #region Console logger

    /// <summary>
    /// A logger that logss to the console
    /// </summary>
    public class ConsoleLogger : ILogger, INotifyPropertyChanged
    {
        /// <summary>
        /// The event that is fired when any child property changes it value
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged = (Sender, e) => { };

        #region Public  Members
        
        /// <summary>
        /// String that get's filled with the last log entry
        /// </summary>
        public string Logs { get; private set; }

        #endregion

        #region Private Members

        private readonly string m_file = "null";

        #endregion

        #region Public properties

        /// <summary>
        /// Defines the log output level
        /// </summary>
        public LogLevel LogLevel { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Specifiy and test the path to a file
        /// </summary>
        /// <param name="path">The path to log to</param>
        public ConsoleLogger(string file)
        {
            // Set the log path
            m_file = file;
        }

        #endregion

        #region Log a message

        public void Log(string sender, string text, LogLevel level)
        {
            // If this message is important enough...
            if (level >= LogLevel)
            {
                string fmt = "000";
                string mText = (m_file != null) ? (m_file):("") + "[" + sender + "]";
                string EmptyString = new string(' ', Enums.SPACELENGTH - mText.Length);
                mText = mText + EmptyString + " " + text;
                int m_Millisecond = DateTime.Now.Millisecond;
                mText = mText + DateTime.Now + ":" + m_Millisecond.ToString(fmt) + " " + text + " ";
                Console.WriteLine(mText);
                
                // Let listeners know that there is a new logstring
                Logs = mText;
            }            
        }

        #endregion
    }

    #endregion

    #region LogList



    #endregion

    #region FileLogger

    /// <summary>
    /// A logger that logs to a file
    /// </summary>
    public class FileLogger : ILogger, INotifyPropertyChanged
    {
        /// <summary>
        /// The event that is fired when any child property changes it value
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged = (Sender, e) => { };

        #region Private Members

        private readonly string m_path = "null";
        private object writelock = new object();
        private string fmt = "000";

        #endregion

        #region Public  Members

        public string Logs { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Specifiy and test the path to a file
        /// </summary>
        /// <param name="path">The path to log to</param>
        public FileLogger(string file)
        {
            // Set the log path
            m_path = Enums.HOMEPATH + Enums.LOGGING + DateTime.Now.Day + "-" + DateTime.Now.Month + "-" + DateTime.Now.Year + "_" + file;

            // Get the folder of the log path
            var directory = Path.GetDirectoryName(m_path);

            // Ensure folder exists
            Directory.CreateDirectory(directory);
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Defines the log output level
        /// </summary>
        public LogLevel LogLevel { get; set; }

        #endregion

        #region Store Text to File

        /// <summary>
        /// Store the enriched text to a file and lock the method
        /// </summary>
        /// <param name="text"></param>
        private void StoreText(string text)
        {

            lock (writelock)
            {
                try
                {
                    int m_Millisecond = DateTime.Now.Millisecond;
                    string m_text = DateTime.Now + ":" + m_Millisecond.ToString(fmt) + " " + text + " " + Environment.NewLine;

                    // Let listeners know that there is a new logstring
                    Logs = m_text;

               
                    using (var fs = new FileStream(m_path, FileMode.Append))
                    {
                        Byte[] info = new UTF8Encoding(true).GetBytes(m_text);
                        fs.Write(info, 0, info.Length);
                        fs.Close();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        #endregion

        #region Log the string and enrich it

        /// <summary>
        /// Provide name of the class calling the logging function to prevent additional typing of the callers name
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="text"></param>
        public void Log(string sender, string text, LogLevel level)
        {
            // If this message is important enough...
            if (level >= LogLevel)
            {
                string mText = "[" + sender + "]"; // + "\t\t\t" + " " + text;
                string EmptyString = new string(' ', Enums.SPACELENGTH - mText.Length);
                mText = mText + EmptyString + " " + text;
                StoreText(mText);
            }            
        }

        #endregion
    }

    #endregion
}
