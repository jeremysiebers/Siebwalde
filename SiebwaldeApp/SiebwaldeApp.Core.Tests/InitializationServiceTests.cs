using System;
using System.Threading;
using System.Threading.Tasks;
using SiebwaldeApp;
using SiebwaldeApp.Core;
using Xunit;

namespace SiebwaldeApp.Core.Tests
{
    public class InitializationServiceTests
    {
#pragma warning disable CS0067 // the events are required by the interface but not raised by this fake
        private sealed class FakeTrackCommClient : ITrackCommClient
        {
            public event EventHandler<AmplifierDataEventArgs>? AmplifierDataReceived;
            public event EventHandler<ControlMessageEventArgs>? ControlMessageReceived;

            public Task StartAsync(bool realHardwareMode, CancellationToken cancellationToken = default)
                => Task.CompletedTask;

            public Task StopAsync(CancellationToken cancellationToken = default)
                => Task.CompletedTask;

            public Task SendAsync(SendMessage message, CancellationToken cancellationToken = default)
                => Task.CompletedTask;

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
#pragma warning restore CS0067

        private sealed class FakeStep : IInitializationStep
        {
            private readonly Func<InitStepResult> _result;

            public FakeStep(string name, Func<InitStepResult> result)
            {
                Name = name;
                _result = result;
            }

            public string Name { get; }

            public Task<InitStepResult> ExecuteAsync(ReceivedMessage? lastMessage, CancellationToken cancellationToken)
                => Task.FromResult(_result());
        }

        private static async Task<InitializationStatus> RunAsync(params IInitializationStep[] steps)
        {
            var client = new FakeTrackCommClient();
            var variables = new TrackApplicationVariables();
            var service = new TrackAmplifierInitializationServiceAsync(client, variables, steps);

            var last = InitializationStatus.Idle;
            service.StatusChanged += (_, status) => last = status;

            await service.InitializeAsync(CancellationToken.None);
            return last;
        }

        [Fact]
        public async Task ChainOfSteps_Completes()
        {
            var status = await RunAsync(
                new FakeStep("ConnectToEthernetTarget", () => InitStepResult.Next("B")),
                new FakeStep("B", () => InitStepResult.Completed()));

            Assert.Equal(InitializationStatus.Completed, status);
        }

        [Fact]
        public async Task MissingInitialStep_Fails()
        {
            var status = await RunAsync(
                new FakeStep("SomethingElse", () => InitStepResult.Completed()));

            Assert.Equal(InitializationStatus.Failed, status);
        }

        [Fact]
        public async Task NextToUnknownStep_Fails()
        {
            var status = await RunAsync(
                new FakeStep("ConnectToEthernetTarget", () => InitStepResult.Next("DoesNotExist")));

            Assert.Equal(InitializationStatus.Failed, status);
        }

        [Fact]
        public async Task StepError_Fails()
        {
            var status = await RunAsync(
                new FakeStep("ConnectToEthernetTarget", () => InitStepResult.Error("boom")));

            Assert.Equal(InitializationStatus.Failed, status);
        }

        [Fact]
        public async Task ContinueThenComplete_Completes()
        {
            var calls = 0;

            var status = await RunAsync(
                new FakeStep("ConnectToEthernetTarget", () =>
                {
                    calls++;
                    return calls < 2 ? InitStepResult.Continue() : InitStepResult.Completed();
                }));

            Assert.Equal(InitializationStatus.Completed, status);
            Assert.Equal(2, calls);
        }
    }
}
