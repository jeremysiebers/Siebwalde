using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace SiebwaldeApp
{
    public interface IMain
    {
        void SiebwaldeAppLogging(string text);
        void FYLinkActivityUpdate();
    }

    public class SiebwaldeApplicationModel
    {
        #region Public properties

        //public ObservableCollection<LogList> Log { get; set; }
        //public LogList Logging { get; private set; }

        public List<string> Logging { get; private set; }

        #endregion

        #region Private members

        public FiddleYardController FYcontroller;
        public FiddleYardSettingsForm FYSettingsForm;
        //private TrackController MTcontroller;
        public FiddleYardController YDcontroller;
        public MAC_IP_Conditioner MACIPConditioner = new MAC_IP_Conditioner { };

        public ILogger SiebwaldeApplicationMainLogging;

        static ILogger GetLogger(string file)
        {
            return new FileLogger(file);
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor
        /// <summary>
        public SiebwaldeApplicationModel()
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

            SiebwaldeApplicationMainLogging.Log(GetType().Name, "Siebwalde Application started.");
            SiebwaldeApplicationMainLogging.Log(GetType().Name, "Main: PC MAC adress is: " + MACIPConditioner.MACstring());
            SiebwaldeApplicationMainLogging.Log(GetType().Name, "Main: PC IP adress is: " + MACIPConditioner.IPstring());
        }
        #endregion

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

        public void StartFYController()
        {
            
            if (FYcontroller != null)
            {
                //FYcontroller.FYTOPShow(false, 1010, 1948, 0, 0, true);
                //FYcontroller.FYBOTShow(false, 1010, 1948, 0, 0, true);
                return;
            }
            
            int FYSendingport = 28671;
            int FYReceivingport = 0x7000; // Port on which the PC will receive data from the FiddleYard   
            FYcontroller = new FiddleYardController(this, MACIPConditioner.MAC(), MACIPConditioner.IP(), FYReceivingport, FYSendingport);
            IoC.siebwaldeApplicationModel.SiebwaldeApplicationMainLogging.Log(GetType().Name, "Main: FiddleYard Controller starting...");
            FYcontroller.Start();
;
            //FYcontroller.FYTOPShow(false, 1010, 1948, 0, 0, true);
            //FYcontroller.FYBOTShow(false, 1010, 1948, 0, 0, true);
            //FiddleYardFormTop.Visible = true;
            //FYLinkActivity.Visible = true;
            //LFYLinkActivity.Visible = true;
            IoC.siebwaldeApplicationModel.SiebwaldeApplicationMainLogging.Log(GetType().Name, "Main: FiddleYard Controller started.");
        }

    }
}
