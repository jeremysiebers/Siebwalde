using SiebwaldeApp.Core;  // TrackCommClientAsync, IInitializationStep, steps, init service

namespace SiebwaldeApp.Core.Host
{
    internal class Program
    {
        private const string FwPath = "C:\\Localdata\\Siebwalde\\TrackAmplifier4.X\\dist\\Offset\\production\\TrackAmplifier4.X.production.hex";

        static async Task Main(string[] args)
        {
            // ---- IoC configureren voor logging + file access ----
            IoC.Kernel.Bind<ILogFactory>()
                .ToConstant(new BaseLogFactory
                {
                    // optioneel:
                    LogOutputLevel = LogOutputLevel.Debug
                });

            IoC.Kernel.Bind<IFileManager>()
                .To<FileManager>()
                .InSingletonScope();
            // ------------------------------------------------------

            Console.Title = "Siebwalde Track Init TestHost";
            Console.WriteLine("=== Siebwalde Track Init TestHost ===");
            Console.WriteLine("CTRL+C om te stoppen.\n");

            using var cts = new CancellationTokenSource();

            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                Console.WriteLine("Cancel gevraagd, afsluiten...");
                cts.Cancel();
            };

            // 1) Core data model
            var variables = new TrackApplicationVariables();

            // 2) Fake transport + comm client
            ITrackTransport transport = new FakeTrackTransport();
            var commClient = new TrackCommClientAsync(transport, variables);

            var bootloaderHelpers = new TrackAmplifierBootloaderHelpers(FwPath, "fake");
            var sendNextFwDataPacket = new SendNextFwDataPacket(commClient, bootloaderHelpers);


            // 3) Init-steps registreren
            var steps = new IInitializationStep[]
            {
                new ConnectToEthernetTargetStep(commClient, variables),
                new ResetAllSlavesStep(commClient, variables),
                new DataUploadStep(commClient, variables),
                new DetectSlavesStep(commClient, variables),
                new FlashFwTrackamplifiersStep(commClient, variables, sendNextFwDataPacket, bootloaderHelpers),
                new InitTrackamplifiersStep(commClient),
                new EnableTrackamplifiersStep(commClient),
            };

            var initService = new TrackAmplifierInitializationServiceAsync(
                commClient,
                variables,
                steps);

            // 4) Logging van init-status
            initService.StatusChanged += (sender, status) =>
            {
                Console.WriteLine($"[INIT] Status = {status}");
            };

            initService.ProgressChanged += (sender, progress) =>
            {
                var pct = progress.Percent.HasValue
                    ? $"{progress.Percent:P0}"
                    : "";
                Console.WriteLine($"[INIT] [{progress.StepName}] {progress.Message} {pct}");
            };

            // 5) Start communicatie + init-pipeline
            try
            {
                Console.WriteLine("[MAIN] Start TrackCommClientAsync...");
                await commClient.StartAsync(realHardwareMode: false, cancellationToken: cts.Token);

                Console.WriteLine("[MAIN] Start initialization pipeline...");
                await initService.InitializeAsync(cts.Token);

                Console.WriteLine("[MAIN] Initialization done (Completed).");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("[MAIN] Initialization geannuleerd.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("[MAIN] FOUT tijdens initialization:");
                Console.WriteLine(ex);
            }
            finally
            {
                Console.WriteLine("[MAIN] Stop TrackCommClientAsync...");
                try
                {
                    await commClient.StopAsync(cts.Token);
                }
                catch { /* ignore */ }
            }

            Console.WriteLine("\nKlaar. Druk op ENTER om af te sluiten.");
             Console.ReadLine();
        }
    }
}
