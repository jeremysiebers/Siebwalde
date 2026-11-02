# release/current — current validated track-amplifier firmware

This directory holds the **current intended PIC18 track-amplifier firmware** for the normal
Siebwalde application (protocol 0x02, the track-power-capable build).

| File | Identity |
|---|---|
| `TrackAmplifier4.X.production.hex` | SHA-256 `935A04E5F3BD112E6DB715F95234C1C25C5158298E74B7290EE21747B5A5F093` |
| | PIC18 application checksum `0x0E39` (embedded word at `0x7FFE`/`0x7FFF`) |
| | wire PROTOCOL_VERSION `0x02` (HR22) — supports `HR_TRACK_POWER` (register 23) |

This is the artifact the `CoreSettings` default `TrackAmplifierFwPath` points at, so the normal
application identifies the intended current firmware. It is NOT the rollback artifact: the previous
`0x5784` build is preserved unchanged at
`TrackAmplifier4.X/dist/Offset/production/TrackAmplifier4.X.production.hex`.

Safety contract:

- A firmware checksum mismatch is **detected and reported**, never auto-flashed. The initialization
  state machine transitions to a `Blocked` (authorization-required) stage, not a silent flash.
- Only an explicit operator action (`TrackApplicationRuntimeHost.AuthorizeFirmwareDeploymentAsync`)
  authorizes ONE bounded deployment, which reuses the proven per-amplifier command-131 path (never
  command 122) and additionally requires the controller-session lease.
