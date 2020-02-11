using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Timers;
using System.Net.Sockets;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Management;
using System.Net.NetworkInformation;
using System.Globalization;

namespace Siebwalde_Application
{
    public class Log2LoggingFile
    {
        string m_path = "null";        
        private object writelock = new object();
        private string fmt = "000";

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
    }
}
