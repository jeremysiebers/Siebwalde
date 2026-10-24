# K8 Corrective — Windows Firewall Root-Cause Reassessment

**Status:** offline investigation, 2026-10-25. This CORRECTS the K8 final report's conclusion.

## Corrected root cause

The K8 command-250 timeout was **NOT proven to be a PIC32 protocol incompatibility**. The PO reported that 2–3 Windows Firewall permission dialogs were pending during K8 execution, which means the inbound UDP path of the test executable was likely **blocked by the firewall** at the time.

**Evidence:**
- Command 113 (GET_CONTROLLER_VERSION) **succeeded** — it was sent by `python.exe` (Python 3.13, `C:\users\jerem\appdata\local\programs\python\python313\python.exe`), which has an established inbound allow rule.
- Command 250 (CLIENT_CONNECTION_REQUEST) **timed out** — it was sent by `SiebwaldeApp.Koploper.Validation.Host.exe` (the Release build under `C:\Localdata\Siebwalde_temp\K8-amplifier-identity\...\bin\Release\net8.0-windows7.0\`), whose inbound UDP was the subject of the pending firewall dialogs.
- The two executables share the **same** inbound UDP port 10001 and destination 192.168.1.193:10000; the only material difference is which executable's inbound path was firewalled.

## Firewall state

- Active network profile: **Ethernet = Public** (all three profiles Domain/Private/Public enabled).
- Existing inbound rules (Allow) for `siebwaldeapp.koploper.validation.host.exe`: **UDP + TCP, `Any` local port, `Public` profile**.
- Existing inbound rules for `python313.exe`: present (this is why 113 succeeded).
- **Conclusion:** the Validation.Host UDP rule exists (Public profile), but a fresh build path or a not-yet-approved dialog can still leave a specific executable path blocked until the dialog is approved. The 250 timeout is consistent with the firewall blocking inbound UDP while the approval dialog was pending.

## Narrowly scoped corrective firewall permission (prepared, not yet applied)

Required: an inbound UDP **Allow** rule for the exact test executable path, Private+Public profile, restricted to the diagnostic UDP ports.

```powershell
New-NetFirewallRule -DisplayName "Siebwalde K8 Diagnostic UDP (Validation.Host)" `
  -Direction Inbound -Action Allow -Protocol UDP -LocalPort 10001 `
  -Program "C:\Localdata\Siebwalde_temp\K8-amplifier-identity\SiebwaldeApp\SiebwaldeApp.Koploper.Validation.Host\bin\Release\net8.0-windows7.0\SiebwaldeApp.Koploper.Validation.Host.exe" `
  -Profile Private,Public
```

This is executable- and port-scoped (not an unrestricted exception). The PO/operator should verify the rule and approve any remaining dialog before the next live test.

## Validation performed (offline)

- Socket bind / packet send / receive of command 113 (python) re-verified working — the controller is reachable and responds to raw 113.
- Firewall profile + rule inventory inspected (read-only).

## Updated K8 verdict

- **PIC32 0.1.0 / protocol 0 / build 2026-10-20 remains PHYSICALLY_CONFIRMED** (command 113, byte-identical to K6).
- **The command-250 timeout is reclassified as LIKELY FIREWALL-INDUCED, NOT proven protocol incompatibility.**
- The PIC18 HR7/HR11/HR21/HR22 readback remains **UNKNOWN** (no fresh readback yet).

## Minimal corrective live test (after firewall readiness + a new scoped grant)

1. Confirm the firewall rule is applied (or the dialog approved) for the Release `SiebwaldeApp.Koploper.Validation.Host.exe`.
2. Re-run `readonly-validate --live --read-registers --wait-ms 15000`.
3. If 250 now receives CONTROLLER/CONNECTED/DONE, proceed with the normal SLAVEINFO read (250→100→101→104→106→105) and read HR7/HR11/HR21/HR22.
4. If 250 still times out after firewall readiness, THEN re-classify as a genuine protocol mismatch and investigate the deployed 0.1.0 CONNECT protocol.

No PIC32 reset/flash is required. No active command is repeated until a new scoped test authorization is granted.
