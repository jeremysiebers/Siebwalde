// SiebwaldeApp.Core/Model/TrackApplicationAn/Ports/SimulatedTrackBus.cs
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Scriptbare bus: kan ITrackIn-events genereren en ITrackOut-commando's loggen.
    /// Gebruik Add(...) om scenario-steps toe te voegen en StartAsync(...) om af te spelen.
    /// </summary>
    public sealed class SimulatedTrackBus : ITrackIn, ITrackOut, IDisposable
    {
        // --- ITrackIn events ---
        public event Action<IncomingDetectedEvent> IncomingDetected;
        public event Action<EntrySensorEvent> EntrySensorTriggered;
        public event Action<ExitFreeChangedEvent> ExitBlockFreeChanged;
        public event Action<AmplifierFeedbackEvent> AmplifierOccupiedChanged;
        public event Action<TrainClearedEvent> TrainClearedFromBlock;
        public event Action<HardwareAliveEvent> HardwareAliveChanged;

        private readonly List<ISimStep> _steps = new();
        private CancellationTokenSource _cts;
        private Task _runner;

        // Log van uitgaande commando's (handig voor asserts/debug)
        public readonly List<string> OutboundLog = new();

        public SimulatedTrackBus Add(params ISimStep[] steps)
        {
            _steps.AddRange(steps);
            _steps.Sort((a, b) => a.WhenUtc.CompareTo(b.WhenUtc));
            return this;
        }

        public Task StartAsync(CancellationToken token = default)
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            _runner = Task.Run(() => RunAsync(_cts.Token), _cts.Token);
            return Task.CompletedTask;
        }

        private async Task RunAsync(CancellationToken token)
        {
            foreach (var step in _steps)
            {
                var delay = step.WhenUtc - DateTime.UtcNow;
                if (delay > TimeSpan.Zero)
                {
                    try { await Task.Delay(delay, token); }
                    catch (TaskCanceledException) { break; }
                }
                if (token.IsCancellationRequested) break;
                step.Execute(this);
            }
        }

        public void Stop()
        {
            try { _cts?.Cancel(); _runner?.Wait(50); } catch { /* ignore */ }
        }

        public void Dispose() => Stop();

        // --- ITrackOut implementatie: loggen ---
        public void SetAmplifierStop(int trackNumber, bool stop)
        {
            var msg = $"SetAmplifierStop track={trackNumber} stop={stop} @ {DateTime.UtcNow:O}";
            OutboundLog.Add(msg);
            IoC.Logger.Log("[SimBus OUT] " + msg, "");
        }

        public void SetSignalEntry(bool isTopSide, bool green)
        {
            var msg = $"SetSignalEntry top={isTopSide} green={green} @ {DateTime.UtcNow:O}";
            OutboundLog.Add(msg);
            IoC.Logger.Log("[SimBus OUT] " + msg, "");
        }

        public void SetSignalExit(bool isTopSide, bool green)
        {
            var msg = $"SetSignalExit top={isTopSide} green={green} @ {DateTime.UtcNow:O}";
            OutboundLog.Add(msg);
            IoC.Logger.Log("[SimBus OUT] " + msg, "");
        }

        public void SetSwitch(int switchId, SwitchPosition position)
        {
            var msg = $"SetSwitch id={switchId} pos={position} @ {DateTime.UtcNow:O}";
            OutboundLog.Add(msg);
            IoC.Logger.Log("[SimBus OUT] " + msg, "");
        }

        public void StopBeforeStation(bool isTopSide)
        {
            var msg = $"StopBeforeStation top={isTopSide} @ {DateTime.UtcNow:O}";
            OutboundLog.Add(msg);
            IoC.Logger.Log("[SimBus OUT] " + msg, "");
        }


        // --- Helpers om events te raisen ---
        public void RaiseIncoming(bool isTop, bool isFreight)
        {
            IoC.Logger.Log($"[SimBus] RaiseIncoming (isTop={isTop}, isFreight={isFreight})", "");
            IncomingDetected?.Invoke(new IncomingDetectedEvent(isTop, isFreight, DateTime.UtcNow));
        }

        public void RaiseEntrySensor(bool isTop, int track)
        {
            IoC.Logger.Log($"[SimBus] RaiseEntrySensor (isTop={isTop}, track={track})", "");
            EntrySensorTriggered?.Invoke(new EntrySensorEvent(isTop, track, DateTime.UtcNow));
        }

        public void RaiseExitFree(bool isTop, bool isFree)
        {
            IoC.Logger.Log($"[SimBus] RaiseExitFree (isTop={isTop}, isFree={isFree})", "");
            ExitBlockFreeChanged?.Invoke(new ExitFreeChangedEvent(isTop, isFree, DateTime.UtcNow));
        }

        public void RaiseAmplifierOccupied(int track, bool occ)
        {
            IoC.Logger.Log($"[SimBus] RaiseAmplifierOccupied (track={track}, occ={ occ})", "");
            AmplifierOccupiedChanged?.Invoke(new AmplifierFeedbackEvent(track, occ, DateTime.UtcNow));
        }

        public void RaiseTrainCleared(bool isTop, int track)
        {
            IoC.Logger.Log($"[SimBus] RaiseTrainCleared (isTop={isTop}, track={track})", "");
            TrainClearedFromBlock?.Invoke(new TrainClearedEvent(isTop, track, DateTime.UtcNow));
        }

        public void RaiseHardwareAlive(bool isAlive)
        {
            IoC.Logger.Log($"[SimBus] RaiseAmplifierOccupied (isAlive={isAlive})", "");
            HardwareAliveChanged?.Invoke(new HardwareAliveEvent(isAlive, DateTime.UtcNow));
        }            
    }

    // --- Scenario DSL ---
    public interface ISimStep { DateTime WhenUtc { get; } void Execute(SimulatedTrackBus bus); }
    public abstract record SimStep(DateTime WhenUtc) : ISimStep
    {
        public abstract void Execute(SimulatedTrackBus bus);
    }

    public sealed record IncomingStep(DateTime WhenUtc, bool IsTop, bool IsFreight) : SimStep(WhenUtc)
    { public override void Execute(SimulatedTrackBus bus) => bus.RaiseIncoming(IsTop, IsFreight); }

    public sealed record EntrySensorStep(DateTime WhenUtc, bool IsTop, int Track) : SimStep(WhenUtc)
    { public override void Execute(SimulatedTrackBus bus) => bus.RaiseEntrySensor(IsTop, Track); }

    public sealed record ExitFreeStep(DateTime WhenUtc, bool IsTop, bool IsFree) : SimStep(WhenUtc)
    { public override void Execute(SimulatedTrackBus bus) => bus.RaiseExitFree(IsTop, IsFree); }

    public sealed record AmplifierOccStep(DateTime WhenUtc, int Track, bool Occupied) : SimStep(WhenUtc)
    { public override void Execute(SimulatedTrackBus bus) => bus.RaiseAmplifierOccupied(Track, Occupied); }

    public sealed record TrainClearedStep(DateTime WhenUtc, bool IsTop, int Track) : SimStep(WhenUtc)
    { public override void Execute(SimulatedTrackBus bus) => bus.RaiseTrainCleared(IsTop, Track); }

    public sealed record AliveStep(DateTime WhenUtc, bool IsAlive) : SimStep(WhenUtc)
    { public override void Execute(SimulatedTrackBus bus) => bus.RaiseHardwareAlive(IsAlive); }
}
