using SiebwaldeApp.Core;
using SiebwaldeApp.Core.Properties;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Input;

namespace SiebwaldeApp
{
    /// <summary>
    /// View model for the application settings page. It edits the core configuration
    /// values, persists them, offers a default per entity, and supports undo (Ctrl-Z).
    /// </summary>
    public class SiebwaldeSettingsPageViewModel : BaseViewModel
    {
        private sealed class Snapshot
        {
            public string TrackControllerIpAddress = "";
            public string TrackControllerSendingPort = "";
            public string TrackControllerReceivingPort = "";
            public string TrackAmplifierFirmwarePath = "";
            public string LogDirectory = "";
            public string FiddleYardSendingPort = "";
            public string FiddleYardReceivingPort = "";
            public string BlockTopologyConfig = "";
            public string KoploperBlockMapConfig = "";
        }

        private readonly Stack<Snapshot> _undo = new();

        #region Public properties

        public string TrackControllerIpAddress { get; set; } = "";
        public string TrackControllerSendingPort { get; set; } = "";
        public string TrackControllerReceivingPort { get; set; } = "";
        public string TrackAmplifierFirmwarePath { get; set; } = "";
        public string LogDirectory { get; set; } = "";
        public string FiddleYardSendingPort { get; set; } = "";
        public string FiddleYardReceivingPort { get; set; } = "";
        public string BlockTopologyConfig { get; set; } = "";
        public string KoploperBlockMapConfig { get; set; } = "";
        public string Status { get; set; } = "";
        public bool CanUndo { get; set; }

        #endregion

        #region Public commands

        public ICommand Save { get; set; }
        public ICommand Reload { get; set; }
        public ICommand ResetTrack { get; set; }
        public ICommand ResetFiddleYard { get; set; }
        public ICommand ResetLogging { get; set; }
        public ICommand ResetMapping { get; set; }
        public ICommand Undo { get; set; }

        #endregion

        public SiebwaldeSettingsPageViewModel()
        {
            Save = new RelayCommand(SaveSettings);
            Reload = new RelayCommand(ReloadSettings);
            ResetTrack = new RelayCommand(ResetTrackSettings);
            ResetFiddleYard = new RelayCommand(ResetFiddleYardSettings);
            ResetLogging = new RelayCommand(ResetLoggingSettings);
            ResetMapping = new RelayCommand(ResetMappingSettings);
            Undo = new RelayCommand(UndoLast);

            ReloadSettings();
            Status = "Settings loaded. Edit a value and press Save. Use Ctrl-Z to undo.";
        }

        #region Load and save

        private void ReloadSettings()
        {
            var settings = CoreSettings.Default;

            TrackControllerIpAddress = settings.TrckIpAddress;
            TrackControllerSendingPort = settings.TrckSendingPort.ToString(CultureInfo.InvariantCulture);
            TrackControllerReceivingPort = settings.TrckReceivingPort.ToString(CultureInfo.InvariantCulture);
            TrackAmplifierFirmwarePath = settings.TrackAmplifierFwPath;
            LogDirectory = settings.LogDirectory;
            FiddleYardSendingPort = settings.FYSendingport.ToString(CultureInfo.InvariantCulture);
            FiddleYardReceivingPort = settings.FYReceivingport.ToString(CultureInfo.InvariantCulture);
            // Read the mapping through CoreConfiguration so the page shows the value that is
            // actually used: a blank persisted value falls back to the configured default
            // instead of leaving the operator with a silently empty mapping.
            BlockTopologyConfig = CoreConfiguration.BlockTopologyConfig;
            KoploperBlockMapConfig = CoreConfiguration.KoploperBlockMapConfig;
        }

        private void SaveSettings()
        {
            if (!TryParsePort(TrackControllerSendingPort, "TrackController sending port", out var trckSendingPort) ||
                !TryParsePort(TrackControllerReceivingPort, "TrackController receiving port", out var trckReceivingPort) ||
                !TryParsePort(FiddleYardSendingPort, "Fiddle Yard sending port", out var fySendingPort) ||
                !TryParsePort(FiddleYardReceivingPort, "Fiddle Yard receiving port", out var fyReceivingPort))
            {
                return;
            }

            PushUndo();

            var settings = CoreSettings.Default;
            settings.TrckIpAddress = TrackControllerIpAddress;
            settings.TrckSendingPort = trckSendingPort;
            settings.TrckReceivingPort = trckReceivingPort;
            settings.TrackAmplifierFwPath = TrackAmplifierFirmwarePath;
            settings.LogDirectory = LogDirectory;
            settings.FYSendingport = fySendingPort;
            settings.FYReceivingport = fyReceivingPort;
            settings.BlockTopologyConfig = BlockTopologyConfig;
            settings.KoploperBlockMapConfig = KoploperBlockMapConfig;
            settings.Save();

            Status = $"Settings saved at {DateTime.Now:HH:mm:ss}.";
        }

        #endregion

        #region Reset per entity

        private void ResetTrackSettings()
        {
            PushUndo();
            TrackControllerIpAddress = GetDefault("TrckIpAddress");
            TrackControllerSendingPort = GetDefault("TrckSendingPort");
            TrackControllerReceivingPort = GetDefault("TrckReceivingPort");
            TrackAmplifierFirmwarePath = GetDefault("TrackAmplifierFwPath");
            Status = "Track settings reset to defaults (press Save to persist).";
        }

        private void ResetFiddleYardSettings()
        {
            PushUndo();
            FiddleYardSendingPort = GetDefault("FYSendingport");
            FiddleYardReceivingPort = GetDefault("FYReceivingport");
            Status = "Fiddle Yard settings reset to defaults (press Save to persist).";
        }

        private void ResetLoggingSettings()
        {
            PushUndo();
            LogDirectory = GetDefault("LogDirectory");
            Status = "Logging settings reset to defaults (press Save to persist).";
        }

        private void ResetMappingSettings()
        {
            PushUndo();
            BlockTopologyConfig = GetDefault("BlockTopologyConfig");
            KoploperBlockMapConfig = GetDefault("KoploperBlockMapConfig");
            Status = "Block mapping reset to defaults (press Save to persist).";
        }

        private static string GetDefault(string name)
        {
            var property = CoreSettings.Default.Properties[name];
            return property?.DefaultValue?.ToString() ?? "";
        }

        #endregion

        #region Undo

        private void PushUndo()
        {
            _undo.Push(new Snapshot
            {
                TrackControllerIpAddress = TrackControllerIpAddress,
                TrackControllerSendingPort = TrackControllerSendingPort,
                TrackControllerReceivingPort = TrackControllerReceivingPort,
                TrackAmplifierFirmwarePath = TrackAmplifierFirmwarePath,
                LogDirectory = LogDirectory,
                FiddleYardSendingPort = FiddleYardSendingPort,
                FiddleYardReceivingPort = FiddleYardReceivingPort,
                BlockTopologyConfig = BlockTopologyConfig,
                KoploperBlockMapConfig = KoploperBlockMapConfig
            });

            CanUndo = true;
        }

        private void UndoLast()
        {
            if (_undo.Count == 0)
            {
                Status = "Nothing to undo.";
                return;
            }

            var snapshot = _undo.Pop();
            TrackControllerIpAddress = snapshot.TrackControllerIpAddress;
            TrackControllerSendingPort = snapshot.TrackControllerSendingPort;
            TrackControllerReceivingPort = snapshot.TrackControllerReceivingPort;
            TrackAmplifierFirmwarePath = snapshot.TrackAmplifierFirmwarePath;
            LogDirectory = snapshot.LogDirectory;
            FiddleYardSendingPort = snapshot.FiddleYardSendingPort;
            FiddleYardReceivingPort = snapshot.FiddleYardReceivingPort;
            BlockTopologyConfig = snapshot.BlockTopologyConfig;
            KoploperBlockMapConfig = snapshot.KoploperBlockMapConfig;

            CanUndo = _undo.Count > 0;
            Status = "Last change undone.";
        }

        #endregion

        private bool TryParsePort(string value, string name, out ushort port)
        {
            if (ushort.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out port))
            {
                return true;
            }

            Status = $"{name} is not a valid port number.";
            return false;
        }
    }
}
