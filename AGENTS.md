# Siebwalde AI Contributor Instructions

## Reading Order

1. `AGENTS.md` for shared rules and current project map.
2. `docs/README.md` for the documentation index.
3. `docs/product.md` for confirmed project purpose, scope, and open questions.
4. `docs/handoff.md` for current progress and resume steps.
5. `docs/analysis-coverage.md` for investigation status.
6. `docs/inventory.md`, `docs/architecture.md`, `docs/implementation.md`, and `docs/build-test.md` for verified project knowledge.

## Project Map

The workspace root is a Git repository (`https://github.com/jeremysiebers/Siebwalde.git`) at `C:\Localdata\Siebwalde`. It is broader than the managed .NET application: it also contains microcontroller firmware, PCB hardware sources, Python tooling, runtime logs, and backup projects.

Managed .NET application (analysis focus):

- `SiebwaldeApp/` is the main solution root. `SiebwaldeApp/SiebwaldeApp.sln` includes the desktop UI, core library, and active ECoS emulator library.
- `SiebwaldeApp.Core.Host/` is a separate console host solution that references `SiebwaldeApp/SiebwaldeApp.Core`.
- `SiebwaldeApp.EcosEmu/` is a separate emulator host solution that references `SiebwaldeApp/SiebwaldeApp.EcosEmu` and also contains an unreferenced `SiebwaldeApp.EcosEmu_old` copy.

Embedded firmware and hardware (inventoried, not deeply analyzed):

- `TrackAmplifier4.X/` track amplifier firmware; contains its own agent guidance (`AGENT_TRACK_AMPLIFIER.md`, `AGENT_TRACK_AMPLIFIER_COMPACT.md`, `MODBUS_TRACK_AMPLIFIER_MAPPING.md`, `TRACK_AMPLIFIER_STATE_MACHINE.md`).
- `TrackAmplifierBootLoader.X/`, `TrackBackplane2.X/`, `TrackController5/`, `ServoController.X/`, `FiddleYard/`, `Faller_Car_ucontroller2.X/`, `Faller_Car_uControllerBootLoader.X/`, `YardController.X/` microcontroller firmware and tooling.
- `KiCad/` PCB designs; `Common_Files/` ModBus slave/master mappings; `Ecos ESU info/` vendor reference material; `Backup projects/` archived/backup firmware projects.

Supporting areas:

- `Logging/` runtime logs and `locos.json` persistence; `docs/` analysis and handoff documentation; `.opencode/` agent definitions and tooling.

See `docs/inventory.md` for the full inventory and `docs/analysis-coverage.md` for investigation status.

## Commands And Prerequisites

- Verified environment command: `dotnet --info` reported SDK `9.0.318`, .NET 8 runtime, and .NET 8 Windows Desktop runtime on Windows. This was verified in the pre-migration workspace; re-run it in `C:\Localdata\Siebwalde` to reconfirm.
- Proposed build commands are documented in `docs/build-test.md`; full build/test execution was not performed because it can update `bin/` and `obj/`.
- `dotnet test "SiebwaldeApp\SiebwaldeApp.Tests\SiebwaldeApp.Tests.csproj" --no-build --no-restore -c Debug` previously failed because the test DLL was not present. That check was run in the pre-migration workspace and requires revalidation here.
- Firmware projects use Microchip MPLAB X / XC compilers and Python tooling. Do not build, flash, or connect to hardware without explicit authorization.

## Constraints

- Communicate with the user in Dutch. Write documentation and agent instructions in English.
- Do not modify application source code, dependency files, or runtime configuration unless the user explicitly authorizes implementation work.
- Keep proposed improvements in `docs/backlog.md` until approved.
- Support durable facts with repository-relative paths and exact symbol names.
- Distinguish verified facts, assumptions, proposals, and open questions.
- Do not document generated output or vendored packages line by line.

## Knowledge Maintenance

- Update `docs/analysis-coverage.md` whenever investigation status changes.
- Update `docs/handoff.md` before ending a session or when a material uncertainty remains.
- Keep `AGENTS.md` concise and link to detailed docs instead of duplicating the knowledge base.
- The project-specific OpenCode agents live in `.opencode/agents/`. Restart OpenCode before relying on newly created or changed agent definitions.
