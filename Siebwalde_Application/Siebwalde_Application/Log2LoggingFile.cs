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
        List<string> TextToFile = new List<string>();
        List<string> TextToFile2 = new List<string>();
        private bool WritingTextToFileReady = true;

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
            string Spaces = " ";
            int m_Millisecond = DateTime.Now.Millisecond;
            if (m_Millisecond < 10)
            {
                Spaces = "   ";
            }
            else if (m_Millisecond < 100)
            {
                Spaces = "  ";
            }
            string m_text = DateTime.Now + ":" + Convert.ToString(m_Millisecond) + Spaces + text + " " + Environment.NewLine;

            TextToFile.Add(m_text);

            if (WritingTextToFileReady == true)
            {
                WritingTextToFileReady = false;     // Make sure this can be started only once

                foreach (string text2 in TextToFile)
                {
                    TextToFile2.Add(text2);          // deepcopy content of text to be written into mailbox TextToFile2
                }

                TextToFile.Clear();                 // clear this local buffer of text to be written
                StoreText2();                       // start writing mailbox to files
            }

        }

        private void StoreText2()
        {
            try
            {

                using (var fs = new FileStream(m_path, FileMode.Append))
                {
                    foreach (string text in TextToFile2)
                    {
                        Byte[] info = new UTF8Encoding(true).GetBytes(text);
                        fs.Write(info, 0, info.Length);
                    }
                    fs.Close();
                    TextToFile2.Clear();                                        // clear mailbox
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            WritingTextToFileReady = true;
        }
    }
}
