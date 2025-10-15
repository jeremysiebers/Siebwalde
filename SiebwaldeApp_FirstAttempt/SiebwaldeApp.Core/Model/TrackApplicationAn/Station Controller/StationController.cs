using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Coordinates sides (Top/Bottom), detects incoming, assigns tracks,
    /// provides exit availability, and forwards departure requests from SW.
    /// </summary>
    public class StationController
    {
        #region private properties
        // Logger instance
        // logging local variables
        private ILogger _stationcontrolloging;

        private CancellationTokenSource _cts;
        // Basic string for file name
        private NewLogFileBasics _logfilebasics;

        // Logger instance
        private static string? LoggerInstance { get; set; }

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
        /// Initializes a new instance of the <see cref="StationController"/> class with the specified logger instance.
        /// </summary>
        /// <remarks>This constructor initializes the logging system for the station control and creates
        /// two station sides: "Top" and "Bottom". Each station side is associated with a specific set of identifiers
        /// and is logged using the provided logger instance.</remarks>
        /// <param name="LoggerInstance">The name of the logger instance to be used for logging operations. This value is used to identify log
        /// entries associated with this instance of <see cref="StationController"/>.</param>
        public StationController(string LoggerInstance)
        {
            // Set logger instance
            LoggerInstance = "Station";

            _logfilebasics = new NewLogFileBasics();

            _stationcontrolloging = GetLogger(_logfilebasics.getLogFile("StationControl.txt"), LoggerInstance);
            // Add the logger to the logging factory
            IoC.Logger.AddLogger(_stationcontrolloging);

            TopStation = new StationSide("Top", new int[] { 10, 11, 12 }, LoggerInstance);
            IoC.Logger.Log($"Instantiate Top Station...", LoggerInstance);

            BottomStation = new StationSide("Bottom", new int[] { 1, 2, 3 }, LoggerInstance);
            IoC.Logger.Log($"Instantiate Bottom Station...", LoggerInstance);

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

        // Called by the outside world (SW) when a departure is requested.
        public void RequestDeparture(bool preferPassenger, bool isTopSide)
        {
            var side = isTopSide ? TopStation : BottomStation;
            side.RequestPreferredDeparture(preferPassenger ? TrainType.Passenger : TrainType.Freight);
        }

        // Called by your layout/sensors when the exit block becomes free/busy.
        public void SetExitAvailability(bool isTopSide, bool isFree)
        {
            var side = isTopSide ? TopStation : BottomStation;
            side.SetExitAvailability(isFree);
        }

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
                        //StartIncomingTrain(isTop);
                        //freeTrack.Reserve();
                        //side.HandleIncomingTrain(isFreight);

                        freeTrack.Reserve();
                        // Later, when the train hits the entry sensor for this track:
                        // side.ConfirmArrivalAndStop(freeTrack, isFreight ? TrainType.Freight : TrainType.Passenger);
                        side.HandleIncomingTrain(freeTrack, isFreight ? TrainType.Freight : TrainType.Passenger);

                        IoC.Logger.Log($"Handle incoming train on {side.Name}, train is {(isFreight ? "Freight" : "Passenger")}", LoggerInstance);
                    }
                    
                }

                await Task.Delay(200, token);
            }
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
            /// TODO: command to stop before station
            IoC.Logger.Log($"Incoming train stopped before {(isTop ? "Top" : "Bottom")} station — no free track.", LoggerInstance);
        }

        #endregion
    }
}
