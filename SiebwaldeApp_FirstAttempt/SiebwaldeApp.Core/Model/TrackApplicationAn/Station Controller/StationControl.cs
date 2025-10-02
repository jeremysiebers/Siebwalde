using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Initializes all Station related properties and data
    /// </summary>
    public class StationControl
    {
        #region private properties
        // Logger instance
        // logging local variables
        private ILogger mStationControlLogging;

        private CancellationTokenSource _cts;
        // Basic string for file name
        private NewLogFileBasics m_LogFileBasics;

        // Logger instance
        private static string LoggerInstance { get; set; }

        // Get a new log factory
        static ILogger GetLogger(string file, string loggerinstance)
        {
            return new FileLogger(file, loggerinstance);
        }

        #endregion

        #region public properties

        public StationSide TopStation { get; }
        public StationSide BottomStation { get; }

        #endregion

        #region constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="LoggerInstance"></param>
        public StationControl(string LoggerInstance)
        {
            // Set logger instance
            LoggerInstance = "Station";

            m_LogFileBasics = new NewLogFileBasics();

            mStationControlLogging = GetLogger(m_LogFileBasics.getLogFile("StationControl.txt"), LoggerInstance);
            // Add the logger to the logging factory
            IoC.Logger.AddLogger(mStationControlLogging);

            TopStation = new StationSide("Top", new int[] { 1, 2, 3 }, LoggerInstance);
            IoC.Logger.Log($"Instantiate Top Station...", LoggerInstance);

            BottomStation = new StationSide("Bottom", new int[] { 10, 11, 12 }, LoggerInstance);
            IoC.Logger.Log($"Instantiate Bottom Station...", LoggerInstance);

        }

        /// <summary>
        /// Catching event from track internal vars and sets the correct instance data to be send to Ethernet controller
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        //private void InternalTrackVar_PropertyChanged(object sender, PropertyChangedEventArgs e)
        //{
        //    if (mStations == null) { return; }

        //    StationTrack stnTrack = sender as StationTrack;

        //    switch (stnTrack.stnTrackName)
        //    {
        //        case "Track1":
        //            switch(e.PropertyName)
        //            {
        //                case "setOccStn":
        //                    IoC.TrackVariables.OccToStn1 = (bool)sender.GetType().GetProperty(e.PropertyName).GetValue(sender);
        //                    break;
        //                default: break;
        //            }
        //            break;

        //            default: break;
        //    }
        //}

        #endregion

        #region private methods

        private async Task GlobalLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (DetectIncomingTrain(out bool isFreight, out bool isTop))
                {
                    var side = isTop ? TopStation : BottomStation;

                    var freeTrack = side.GetFreeTrack(isFreight);

                    if (freeTrack == null)
                    {
                        // ❌ No track free → stop the train before entering the station
                        StopIncomingTrain(isTop);
                    }
                    else
                    {
                        // ✅ Track free → reserve and let StationSide handle the rest
                        StartIncomingTrain(isTop);
                        freeTrack.Reserve();
                        side.HandleIncomingTrain(isFreight);
                        IoC.Logger.Log($"Handle incoming train on {side.Name}, train is {(isFreight ? "Freight" : "Passenger")}", LoggerInstance);
                    }
                    
                }

                await Task.Delay(200, token);
            }
        }

        private void StartIncomingTrain(bool isTop)
        {
            // TODO: When a train is detected, this method can be used to set signals or amplifiers to allow the train to enter the station
            throw new NotImplementedException();
        }

        private bool DetectIncomingTrain(out bool isFreight, out bool isTop)
        {
            // TODO: hook up sensors
            isFreight = false;
            isTop = true;
            return false;
        }

        private void StopIncomingTrain(bool isTop)
        {
            // TODO: hier zet je bijvoorbeeld een stopsein of stop-amplifier aan
            IoC.Logger.Log($"Incoming train stopped before {(isTop ? "Top" : "Bottom")} station — no free track.", LoggerInstance);
        }

        #endregion

        #region public methods

        public void Start()
        {
            IoC.Logger.Log($"Start StationControl GlobalLoop.", LoggerInstance);
            _cts = new CancellationTokenSource();
            TopStation.Start(_cts.Token);
            BottomStation.Start(_cts.Token);
            Task.Run(() => GlobalLoop(_cts.Token));
        }

        public void Stop() => _cts?.Cancel();

        /// <summary>
        /// Catching event from track io and sets the correct instance variables of station and tracks
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //private void ExternalTrackVar_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        //{
        //    if (mStations == null) { return; }

        //    switch (e.PropertyName)
        //    {
        //        case "OccFromStn1":
        //            _mStations["BOT"].StnTracks["Track1"].getOccStn = (bool)sender.GetType().GetProperty(e.PropertyName).GetValue(sender);
        //            break;
        //        case "OccFromStn2":
        //            _mStations["BOT"].StnTracks["Track2"].getOccStn = (bool)sender.GetType().GetProperty(e.PropertyName).GetValue(sender);
        //            break;
        //        case "OccFromStn3":
        //            _mStations["BOT"].StnTracks["Track3"].getOccStn = (bool)sender.GetType().GetProperty(e.PropertyName).GetValue(sender);
        //            break;
        //        case "OccFromStn10":
        //            _mStations["TOP"].StnTracks["Track10"].getOccStn = (bool)sender.GetType().GetProperty(e.PropertyName).GetValue(sender);
        //            break;
        //        case "OccFromStn11":
        //            _mStations["TOP"].StnTracks["Track11"].getOccStn = (bool)sender.GetType().GetProperty(e.PropertyName).GetValue(sender);
        //            break;
        //        case "OccFromStn12":
        //            _mStations["TOP"].StnTracks["Track12"].getOccStn = (bool)sender.GetType().GetProperty(e.PropertyName).GetValue(sender);
        //            break;

        //        default:
        //            break;
        //    }

        //}

        //internal void Start()
        //{
        //    // simulate data
        //    IoC.TrackVariables.OccFromStn1 = true;

        //    _mStations["BOT"].StnTracks["Track1"].setOccStn = true;
        //}

        #endregion
    }
}
