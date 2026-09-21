using System.Runtime.CompilerServices;

// The focused harness tests live in the existing test project so the harness command's
// production-path seam and no-seeding property are covered by the normal test run. The harness
// remains a standalone console tool and is not added to SiebwaldeApp.sln.
[assembly: InternalsVisibleTo("SiebwaldeApp.Core.Tests")]
