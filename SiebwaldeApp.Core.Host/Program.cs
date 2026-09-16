using SiebwaldeApp.Core;
using SiebwaldeApp.Core.TrackApplication.Comm;
using System.IO;

namespace SiebwaldeApp.Core.Host
{
    internal class Program
    {
        /// <summary>
        /// Entry point for the console host that drives TrackCommClientAsync.
        /// This host can run either with a fake in-process transport or a real
        /// UDP transport to the Ethernet target.
        /// 
        /// Usage:
        ///   - Fake mode (default):   SiebwaldeApp.Core.Host
        ///   - Real hardware mode:    SiebwaldeApp.Core.Host --real
        /// </summary>
        static async Task Main(string[] args)
        {
            // -----------------------------------------------------------------
            // 1) Configure IoC for logging
            // -----------------------------------------------------------------
            IoC.ConfigureLogger(new BaseLogFactory
            {
                // Set the log output level to Debug so you see everything.
                LogOutputLevel = LogOutputLevel.Debug
            });

            var cts = new CancellationTokenSource();

            Console.CancelKeyPress += (sender, e) =>
            {
                // NOTE: This allows Ctrl+C to cancel the initialization pipeline.
                e.Cancel = true;
                Console.WriteLine("Cancellation requested, shutting down...");
                cts.Cancel();
            };

            // -----------------------------------------------------------------
            // 2) Core data model
            // -----------------------------------------------------------------
            var variables = new TrackApplicationVariables();

            // -----------------------------------------------------------------
            // 3) Select transport: fake or real UDP
            // -----------------------------------------------------------------
            bool useRealTransport = true;

            ITrackTransport transport;

            if (useRealTransport)
            {
                Console.WriteLine("[MAIN] Using REAL UDP transport.");

                // TODO: Adjust these to match your real Ethernet target.
                var targetIpAddress = CoreConfiguration.TrackControllerIpAddress;
                var targetPort = CoreConfiguration.TrackControllerSendingPort;
                var localPort = CoreConfiguration.TrackControllerReceivingPort;

                // Raw UDP client (simple wrapper around UdpClient).
                var rawUdp = new RawUdpTransport(targetIpAddress, targetPort, localPort);

                // Adapter that exposes IRawUdpTransport as ITrackTransport.
                transport = new RawUdpTrackTransport(rawUdp);
            }
            else
            {
                Console.WriteLine("[MAIN] Using FAKE in-process transport.");

                // In-process fake PIC32 emulator for offline testing.
                transport = new FakeTrackTransport();
            }

            // -----------------------------------------------------------------
            // 4) Communication client on top of the selected transport
            // -----------------------------------------------------------------
            var commClient = new TrackCommClientAsync(transport, variables);

            var bootloaderHelpers = new TrackAmplifierBootloaderHelpers(CoreConfiguration.TrackAmplifierFirmwarePath, "fake");
            var sendNextFwDataPacket = new SendNextFwDataPacket(commClient, bootloaderHelpers);

            // -----------------------------------------------------------------
            // 5) Initialization steps
            // -----------------------------------------------------------------
            // NOTE:
            //  - You can add more steps here later (RecoverSlavesStep, 
            //    FlashFwTrackamplifiersStep, InitTrackamplifiersStep, 
            //    EnableTrackamplifiersStep), but for now we keep the same
            //    minimal pipeline you already used while bringing up the host.
            var steps = new IInitializationStep[]
            {
                new ConnectToEthernetTargetStep(commClient, variables),
                new ResetAllSlavesStep(commClient, variables),
                new DataUploadStep(commClient, variables),
                new DetectSlavesStep(commClient, variables),
                new RecoverSlavesStep(commClient, variables, sendNextFwDataPacket, bootloaderHelpers),
                new FlashFwTrackamplifiersStep(commClient, variables, sendNextFwDataPacket, bootloaderHelpers),
                new InitTrackamplifiersStep(commClient),
                new EnableTrackamplifiersStep(commClient),
            };

            var initService = new TrackAmplifierInitializationServiceAsync(
                commClient,
                variables,
                steps);

            // -----------------------------------------------------------------
            // 6) Run TrackCommClientAsync + initialization pipeline
            // -----------------------------------------------------------------
            try
            {
                Console.WriteLine("[MAIN] Start TrackCommClientAsync...");
                // The boolean realHardwareMode is passed through; currently the
                // client does not use it internally, but this keeps the API ready.
                await commClient.StartAsync(realHardwareMode: useRealTransport, cancellationToken: cts.Token);

                Console.WriteLine("[MAIN] Start initialization pipeline...");
                await initService.InitializeAsync(cts.Token);

                Console.WriteLine("[MAIN] Initialization completed.");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("[MAIN] Initialization cancelled.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MAIN] Initialization failed: {ex}");
            }
            finally
            {
                Console.WriteLine("[MAIN] Stop TrackCommClientAsync...");
                try
                {
                    await commClient.StopAsync(cts.Token);
                }
                catch
                {
                    // Ignore errors while shutting down.
                }

                try
                {
                    await transport.CloseAsync(cts.Token);
                }
                catch
                {
                    // Ignore close errors.
                }

                await transport.DisposeAsync();
            }

            Console.WriteLine();
            Console.WriteLine("Done. Press ENTER to exit.");
            Console.ReadLine();
        }
    }
}
