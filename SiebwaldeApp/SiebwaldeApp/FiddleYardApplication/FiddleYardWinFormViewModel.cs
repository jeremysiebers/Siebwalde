using SiebwaldeApp.Core;
using System;
using System.Windows.Forms;

namespace SiebwaldeApp 
{
    public class FiddleYardWinFormViewModel
    {
        public FiddleYardSettingsForm FYSettingsForm;
        public FiddleYardMip50SettingsForm FYMip50SettingsFormTop, FYMip50SettingsFormBot;
        public FiddleYardForm FyFormTop, FyFormBot;

        /// <summary>
        /// Constructor
        /// </summary>
        public FiddleYardWinFormViewModel()
        {
            IoC.siebwaldeApplicationModel.InstantiateFiddleYardWinForms += FiddleYardPageStart;
            IoC.siebwaldeApplicationModel.FiddleYardShowWinForms += ShowFiddleYardWinForm;
            IoC.siebwaldeApplicationModel.FiddleYardShowSettingsWinForms += ShowFiddleYardSettingsWinForm;
        }

        #region Private methods

        private void ShowFiddleYardSettingsWinForm(object sender, EventArgs e)
        {
            if (FYSettingsForm == null)
            {
                FYSettingsForm = new FiddleYardSettingsForm();
            }
            else
            {
                FYSettingsForm.Show();
            }
                
        }

        private void ShowFiddleYardWinForm(object sender, EventArgs e)
        {
            if (IoC.siebwaldeApplicationModel.FYcontroller == null)
            {
                return;
            }
            FYFORMShow(FyFormTop, false, 1010, 1948, 0, 0, true);
            FYFORMShow(FyFormBot, false, 1010, 1948, 0, 0, true);
        }

        private void FiddleYardPageStart(object sender, EventArgs e)
        {
            if (IoC.siebwaldeApplicationModel.FYcontroller == null)
            {
                return;
            }
            else
            {
                if (FyFormTop == null)
                {
                    FYMip50SettingsFormTop = new FiddleYardMip50SettingsForm("TOP", 
                        IoC.siebwaldeApplicationModel.FYcontroller.FYIOHandleTOP.FYApp.FYMIP50,
                        IoC.siebwaldeApplicationModel.FYcontroller.FYIOHandleTOP.FYApp.FYAppVar);

                    FyFormTop = new FiddleYardForm(IoC.siebwaldeApplicationModel.FYcontroller.FYIOHandleTOP.FYApp, FYMip50SettingsFormTop) { Name = "FiddleYardTOP" };
                    FyFormTop.Show();
                    FyFormTop.Hide();
                    // connect the Form to the FYIOHandle interface
                    FyFormTop.Connect(IoC.siebwaldeApplicationModel.FYcontroller.FYIOHandleTOP.FYIOHandleVar,
                        IoC.siebwaldeApplicationModel.FYcontroller.FYIOHandleTOP.FYApp.FYAppVar);
                }
                if (FyFormBot == null)
                {
                    FYMip50SettingsFormBot = new FiddleYardMip50SettingsForm("BOT",
                       IoC.siebwaldeApplicationModel.FYcontroller.FYIOHandleTOP.FYApp.FYMIP50,
                       IoC.siebwaldeApplicationModel.FYcontroller.FYIOHandleTOP.FYApp.FYAppVar);

                    FyFormBot = new FiddleYardForm(IoC.siebwaldeApplicationModel.FYcontroller.FYIOHandleBOT.FYApp, FYMip50SettingsFormBot) { Name = "FiddleYardBOT" };
                    FyFormBot.Show();
                    FyFormBot.Hide();
                    // connect the Form to the FYIOHandle interface 
                    FyFormBot.Connect(IoC.siebwaldeApplicationModel.FYcontroller.FYIOHandleBOT.FYIOHandleVar,
                        IoC.siebwaldeApplicationModel.FYcontroller.FYIOHandleBOT.FYApp.FYAppVar);
                }
            }
        }

        private void HideFiddleYardWinForm()
        {
            FyFormTop.Hide();
            FyFormBot.Hide();
        }

        private void FYFORMShow(FiddleYardForm FYFORM, bool autoscroll, int height, int width, int LocX, int LocY, bool View)
        {
            FYFORM.StartPosition = FormStartPosition.Manual;

            FYFORM.Height = height;// - 60 - 20;// 27;
            if (autoscroll == true)
            {
                FYFORM.Width = width / 2;// - 6;
            }
            else
            {
                FYFORM.Width = width / 2;
            }
            if (FYFORM.Name == "FiddleYardTOP")
                FYFORM.Location = new System.Drawing.Point(LocX + 0, LocY + 30); //(LocX + 6, LocY + 80);
            else if (FYFORM.Name == "FiddleYardBOT")
                FYFORM.Location = new System.Drawing.Point(LocX + 960, LocY + 30);//(LocX + width / 2, LocY + 80);  //960
            FYFORM.AutoScroll = autoscroll;

            if (View && FYFORM.WindowState != FormWindowState.Minimized)
            {
                FYFORM.FYFORMShow(View);
            }
            else if (!View && FYFORM.WindowState != FormWindowState.Minimized)
            {
                FYFORM.FYFORMShow(View);
            }
            else
            {
                FYFORM.WindowState = FormWindowState.Normal;
            }
        }

        #endregion
    }


}
