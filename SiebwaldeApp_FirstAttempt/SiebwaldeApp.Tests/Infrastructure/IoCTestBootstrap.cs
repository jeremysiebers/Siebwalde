// File: SiebwaldeApp.Core.Tests/Infrastructure/IoCTestBootstrap.cs
using Ninject;
using Xunit;

namespace SiebwaldeApp.Core.Tests
{
    [CollectionDefinition("IoC bootstrap")]
    public class IoCCollection : ICollectionFixture<IoCBootstrap> { }

    /// <summary>
    /// Runs once per test collection. Creates a clean kernel and binds a single TestLogFactory.
    /// </summary>
    public sealed class IoCBootstrap
    {
        public IoCBootstrap()
        {
            // 1) Maak kernel schoon (verwijder alle bestaande bindings)
            //    Als je IoC geen Reset heeft, unbind expliciet types die je bindt.
            try
            {
                IoC.Kernel.Unbind<ILogFactory>();
            }
            catch { /* kan al unbound zijn */ }

            // 2) Bind exact één testlogger
            IoC.Kernel.Bind<ILogFactory>().ToConstant(new TestLogFactory());

            // (optioneel) als andere services nodig zijn, bind hier ook:
            // IoC.Kernel.Unbind<IFileManager>();
            // IoC.Kernel.Bind<IFileManager>().ToConstant(new TestFileManager());
        }
    }
}
