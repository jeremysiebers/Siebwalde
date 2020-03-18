using System;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace Siebwalde_Application
{
    public class Log2LoggingFile
    {
        string m_path = "null";
        private object writelock = new object();
        private string fmt = "000";
        private int SpaceLength = 40;

        /*#--------------------------------------------------------------------------#*/
        /*  Description: StoreText
         * 
         *  Input(s)   : Store diagnostic text
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

        public Log2LoggingFile(string path)
        {
            m_path = path;
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: StoreText
         * 
         *  Input(s)   : Store diagnostic text
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
        public void StoreText(string text)
        {

            lock (writelock)
            {
                int m_Millisecond = DateTime.Now.Millisecond;
                string m_text = DateTime.Now + ":" + m_Millisecond.ToString(fmt) + " " + text + " " + Environment.NewLine;

                try
                {
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

        /// <summary>
        /// Provide name of the class calling the logging function to prevent additional typing of the callers name
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="text"></param>
        public void Log(string sender, string text)
        {
            string mText = "[" + sender + "]"; // + "\t\t\t" + " " + text;
            string EmptyString = new string(' ', SpaceLength - mText.Length);
            mText = mText + EmptyString + " " + text;
            StoreText(mText);
        }
    }
}
