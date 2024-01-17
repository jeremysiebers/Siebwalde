using System;
using System.Threading.Tasks;

namespace SiebwaldeApp.Core
{
    public class SiebwaldeApplicationModel
    {
        #region Public properties

        public event EventHandler InstantiateFiddleYardWinForms;
        public event EventHandler FiddleYardShowWinForms;
        public event EventHandler FiddleYardShowSettingsWinForms;

        #endregion

        #region Private members

        public FiddleYardController FYcontroller;
        //public FiddleYardSettingsForm FYSettingsForm;
        
        public FiddleYardController YDcontroller;
        public NewMAC_IP_Conditioner MACIPConditioner = new NewMAC_IP_Conditioner { };
                               
        public TrackController TrackController;

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor
        /// <summary>
        public SiebwaldeApplicationModel()
        {
            IoC.Logger.Log("Siebwalde Application started.", "");
            IoC.Logger.Log("Main: PC MAC adress is: " + MACIPConditioner.MACstring(), "");
            IoC.Logger.Log("Main: PC IP adress is: " + MACIPConditioner.IPstring(), "");
        }
        #endregion

        protected virtual void OnLaunchWinFormsFormRequested(EventArgs e)
        {
            InstantiateFiddleYardWinForms?.Invoke(this, e);
        }

        public void OnFiddleYardShowWinForm(EventArgs e) {
            FiddleYardShowWinForms?.Invoke(this, e);
        }

        public void OnFiddleYardSettingsWinForm(EventArgs e)
        {
            FiddleYardShowSettingsWinForms?.Invoke(this, e);
        }

        /// <summary>
        /// Fiddle Yard
        /// </summary>
        public async Task StartFYController()
        {
            if (FYcontroller != null)
            {                
                return;
            }

            // Tried to make Task and start FiddleYard with it, however the winform stuff needs to run on "main" task as well hence
            // it will give error when starting it on a different task. Therefore first continue with the whole track, later re-build 
            // the fiddle yard part....
            FYcontroller = new FiddleYardController(
                MACIPConditioner.MAC(),
                MACIPConditioner.IP(),
                Properties.CoreSettings.Default.FYReceivingport,
                Properties.CoreSettings.Default.FYSendingport);

            // Launch winform here before controller is started.
            OnLaunchWinFormsFormRequested(EventArgs.Empty);


            IoC.Logger.Log("FiddleYard Controller starting...", "");

            await FYcontroller.StartFiddleYardControllerAsync();
            
            ////FYcontroller.FYTOPShow(false, 1010, 1948, 0, 0, true);
            //FYcontroller.FYBOTShow(false, 1010, 1948, 0, 0, true);
            //FiddleYardFormTop.Visible = true;
            //FYLinkActivity.Visible = true;
            //LFYLinkActivity.Visible = true;
            IoC.Logger.Log("FiddleYard Controller started.", "");

        }



        /// <summary>
        /// Track Controller
        /// </summary>
        /// <returns></returns>
        public async Task StartTrackController()
        {
            if (TrackController == null)
            {
                IoC.Logger.Log("Track Controller starting...", "");

                TrackController = initTrackcontroller();

                await TrackController.StartTrackControllerAsync();

                IoC.Logger.Log("Track Controller started.", "");
            }

        }

        private static TrackController initTrackcontroller()
        {
            return new TrackController(60000, 60000);
        }


    }
}