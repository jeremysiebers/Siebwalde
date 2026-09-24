using System.Runtime.CompilerServices;
using SiebwaldeApp.Core.Properties;

namespace SiebwaldeApp.Core.Tests
{
    /// <summary>
    /// Forces the shared <see cref="CoreSettings.Default"/> settings metadata to materialize once,
    /// single-threaded, before any test body runs.
    /// </summary>
    /// <remarks>
    /// <see cref="System.Configuration.ApplicationSettingsBase.Properties"/> lazily builds its
    /// collection with a non-volatile double-check that assigns an empty
    /// <see cref="System.Configuration.SettingsPropertyCollection"/> BEFORE populating it via
    /// <c>EnsureInitialized()</c>. Under xUnit's cross-class parallelism, a reader (for example
    /// <see cref="CoreConfigurationTests"/>) can observe that empty/partially-populated collection
    /// while another test is still materializing it, making <c>Properties[key]</c> return null and
    /// throwing <see cref="System.NullReferenceException"/> at the subsequent <c>.DefaultValue</c>
    /// access. Touching the collection here, at assembly load, completes the single-threaded
    /// materialization and removes the race window entirely. This is a test-isolation concern only:
    /// production <c>CoreSettings</c> is not modified.
    /// </remarks>
    internal static class CoreSettingsInitialization
    {
        [ModuleInitializer]
        internal static void InitializeCoreSettingsMetadata()
        {
            // Accessing Properties triggers EnsureInitialized(), which also populates Context and
            // Providers, so all three lazily-built collections are fully materialized here.
            _ = CoreSettings.Default.Properties;
        }
    }
}
