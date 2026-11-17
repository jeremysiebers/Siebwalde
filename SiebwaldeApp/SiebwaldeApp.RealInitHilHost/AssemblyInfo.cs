using System.Runtime.CompilerServices;

// The focused hil-d --continue-on-fail tests live in the existing test project so the campaign
// loop's fail-closed default, count-and-continue recovery, and unexpected-global-stop safety are
// covered by the normal test run. The host remains a standalone console tool and is not added to
// SiebwaldeApp.sln.
[assembly: InternalsVisibleTo("SiebwaldeApp.Core.Tests")]
