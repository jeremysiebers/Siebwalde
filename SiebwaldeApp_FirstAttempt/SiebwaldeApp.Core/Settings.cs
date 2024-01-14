using System.Drawing;
using System;

namespace SiebwaldeApp.Core.Properties {
    
    
    // This class allows you to handle specific events on the settings class:
    //  The SettingChanging event is raised before a setting's value is changed.
    //  The PropertyChanged event is raised after a setting's value is changed.
    //  The SettingsLoaded event is raised after the setting values are loaded.
    //  The SettingsSaving event is raised before the setting values are saved.
    internal sealed partial class Settings {

        public SensorUpdater FYSimSpeedSetting;
        public ColorUpdater SWSetColorTrainOccupied;
        public ColorUpdater SWSetColorTrackNotInitialized;
        public ColorUpdater SWSetColorTrackNotActive;
        public ColorUpdater SWSetColorTrackDisabled;
        public ColorUpdater SWSetColorTrackDisabledNotOccupied;

        public Settings() {
            // // To add event handlers for saving and changing settings, uncomment the lines below:
            //
            this.SettingChanging += this.SettingChangingEventHandler;
            //
            this.SettingsSaving += this.SettingsSavingEventHandler;
            //
            FYSimSpeedSetting = new SensorUpdater();
            SWSetColorTrainOccupied = new ColorUpdater();
            SWSetColorTrackNotInitialized = new ColorUpdater();
            SWSetColorTrackNotActive = new ColorUpdater();
            SWSetColorTrackDisabled = new ColorUpdater();
            SWSetColorTrackDisabledNotOccupied = new ColorUpdater();
        }

        private void SettingChangingEventHandler(object sender, System.Configuration.SettingChangingEventArgs e)
        {
            // Add code to handle the SettingChangingEvent event here.
            switch (e.SettingName)
            {
                case "FIDDLExYARDxSIMxSPEEDxSETTING":
                    FYSimSpeedSetting.UpdateSensorValue(Convert.ToInt16(e.NewValue), false);
                    break;
                case "SETxCOLORxTRACKxOCCUPIED":
                    SWSetColorTrainOccupied.UpdateColorValue((Color)e.NewValue);
                    break;
                case "SETxCOLORxTRACKxNOTxINITIALIZED":
                    SWSetColorTrackNotInitialized.UpdateColorValue((Color)e.NewValue);
                    break;
                case "SETxCOLORxTRACKxNOTxACTIVE":
                    SWSetColorTrackNotActive.UpdateColorValue((Color)e.NewValue);
                    break;
                case "SETxCOLORxTRACKxDISABLED":
                    SWSetColorTrackDisabled.UpdateColorValue((Color)e.NewValue);
                    break;
                case "SETxCOLORxTRACKxDISABLEDxNOTxOCCUPIED":
                    SWSetColorTrackDisabledNotOccupied.UpdateColorValue((Color)e.NewValue);
                    break;
                default: break;
            }
        }

        private void SettingsSavingEventHandler(object sender, System.ComponentModel.CancelEventArgs e) {
            // Add code to handle the SettingsSaving event here.
        }
    }
}
