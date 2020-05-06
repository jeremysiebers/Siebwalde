using SiebwaldeApp;
using System;
using System.IO;
using System.Windows;

namespace SiebwaldeApp
{
    public interface IMain
    {
        void SiebwaldeAppLogging(string text);
        void FYLinkActivityUpdate();
    }

    /// <summary
    /// 
    /// <summary
    public class SiebwaldeInitPageViewModel : BaseViewModel
    {
        #region Private members

        private FiddleYardController FYcontroller;
        private FiddleYardSettingsForm FYSettingsForm;
        //private TrackController MTcontroller;
        private FiddleYardController YDcontroller;
        private MAC_IP_Conditioner MACIPConditioner = new MAC_IP_Conditioner { };

        public ILogger SiebwaldeApplicationMainLogging;

        static ILogger GetLogger(string file)
        {
            return new FileLogger(file);
        }

        #endregion

        #region Public properties

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor
        /// <summary>
        public SiebwaldeInitPageViewModel()
        {
            SiebwaldeApplicationMainLogging = GetLogger("SiebwaldeApplicationMain.txt");
            try
            {
                if (!Directory.Exists(Enums.HOMEPATH + Enums.LOGGING))
                {
                    Directory.CreateDirectory(Enums.HOMEPATH + Enums.LOGGING);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            SiebwaldeAppLogging("Siebwalde Application started.");
            SiebwaldeAppLogging("Main: PC MAC adress is: " + MACIPConditioner.MACstring());
            SiebwaldeAppLogging("Main: PC IP adress is: " + MACIPConditioner.IPstring());

            int FYSendingport = 28671;
            int FYReceivingport = 0x7000; // Port on which the PC will receive data from the FiddleYard   
            FYcontroller = new FiddleYardController(this, MACIPConditioner.MAC(), MACIPConditioner.IP(), FYReceivingport, FYSendingport);
            SiebwaldeAppLogging("Main: FiddleYard Controller starting...");
            FYcontroller.Start();

            FYcontroller.FYTOPShow(false, 1010, 1948, 0, 0, true);
            FYcontroller.FYBOTShow(false, 1010, 1948, 0, 0, true);
            //FiddleYardFormTop.Visible = true;
            //FYLinkActivity.Visible = true;
            //LFYLinkActivity.Visible = true;
            SiebwaldeAppLogging("Main: FiddleYard Controller started.");

        }

        public void SiebwaldeAppLogging(string text)
        {
            SiebwaldeApplicationMainLogging.Log(GetType().Name, text);
            string fmt = "000";
            int m_Millisecond = DateTime.Now.Millisecond;
            string m_text = DateTime.Now + ":" + m_Millisecond.ToString(fmt) + " " + text + " " + Environment.NewLine;
            //SiebwaldeAppLog.AppendText(m_text);
        }

        public void FYLinkActivityUpdate()
        {
            //if (FYLinkActivity.InvokeRequired)
            //{
            //    ToggleCommLinkCallback d = new ToggleCommLinkCallback(FYLinkActivityUpdate);
            //    FYLinkActivity.Invoke(d, new object[] { });  // invoking itself
            //}
            //else
            //{
            //    if (FYLinkActivity.Value >= LINKACTMAX)
            //    {
            //        FYLinkActivity.Value = 0;
            //    }
            //    FYLinkActivity.Value++;
            //}
        }

        #endregion
    }
}
