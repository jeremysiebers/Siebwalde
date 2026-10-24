# K8 Feasibility — Passive SLAVEINFO Observation of PIC18 Amplifiers 1/3

**Status:** offline source investigation, 2026-10-24. Verdict: **BLOCKED** for a *genuinely passive* observation. This documents the delivery mechanism, the state-changing prerequisites, and the alternative access methods for a future PO decision. No hardware was touched; no command was issued.

## Conclusion

A **passive read-only** SLAVEINFO observation is **not feasible**. SLAVEINFO is delivered **unicast to a single connected client**, is **request-driven** (command 106), and the meaningful `SERVICE_TASKS` stream additionally requires command **105 ENABLE** (state-changing: HR1=0x8000, outputs on, plus a periodic motion-permission broadcast). Calling this "passive" would be inaccurate.

## Delivery mechanism (source-verified)

| Question | Finding |
| --- | --- |
| How SLAVEINFO reaches the host | **Unicast** to the IP of the single connected client, UDP `10001` (`ethernet.c:81-82,289-292,372-373`; `enums.h:70` SLAVEINFO=0xFF; frame built in `mbus.c:423-426`). Server listens on `10000` (`ethernet.c:265`). |
| Unsolicited or request-driven | **Request-driven**: `upload=UPLOAD_STATE_ALL` is set only by command 106 START_DATA_UPLOAD (`mbus.c:355-359`; `controller.c:358-362`); once started it is a continuous round-robin stream. No separate "subscription" — 106 IS the enable. |
| Multicast/broadcast | **None** — multicast disabled (`Production.mhc:4637`); no UDP broadcast. |
| Passive observer without competing with C# | **No** — one transocket, one destination; a second observer receives nothing unless it becomes the client (250 handshake) or L2-sniffs an already-running stream. In-repo: "a passive observer has no PIC32 client" (`KoploperSessionCommandAllowlist.cs:149-150`). |
| Connection/subscription/state-change needed | **All three** — CONNECT (250), the request (106), and `SERVICE_TASKS` via **105 ENABLE** (`mbus.c:347`; 105 writes HR1=0x8000, `slavestartup.c:646-671`); 101/104 also write (backplane slot-select, amplifier address assignment). |
| Freshness / stale detection | No firmware timestamp/seq. Live-vs-cached via `MbReceiveCounter`/`MbSentCounter` deltas (`PetitModbus.h:73-74`) + host-side `LastDataReceivedUtc` + `ProtocolReadbackObserved` (`TrackCommClientAsync.cs:220-221`; `TrackAmplifierItem.cs:54-78`). |
| Address mapping | `SlaveNumber` = Modbus slave address (`mbus.c:180`); amplifier N → backplane `51+((N-1)/10)`, slot `((N-1)%10)+1` (`slavestartup.c:230-239,405-457,543-548`). Amplifier 1 = `SlaveNumber 1`; amplifier 3 = `SlaveNumber 3`. |

## Actual access methods (for a future PO decision)

1. **State-changing "read-only-enablement" session** — reuse the KIS-09 `ReadOnlyTelemetryValidationHost` + `KoploperSessionCommandGuard`/allowlist with the exact sequence `250→100→101→104→106→105`; "read-only" refers to the write surface (no 107/108/HR0/PWM), **not** passivity — 105 turns outputs on and the master then broadcasts motion-permission `0x4000` every 100 ms (`slavehandler.c:274-284`).
2. **L2 packet capture** of an already-running SLAVEINFO stream (the stream itself earlier required 106+105).
3. **Warm-reconnect (250 only)** against an already-enabled master (`ReadOnlyTelemetryValidationHost.cs:141-152`) — least invasive, but still takes over the single connection and requires a pre-proven `SERVICE_TASKS` state.

None of these is a passive, state-free UDP listen.

## Ownership (future K8 session)

The PIC32 has **one** transocket/destination. A future K8 session would **compete with any active C#/Koploper host** for that connection; there is no side-by-side reception. The write-allowlist/`KoploperSessionCommandGuard` is **process-local** (no named mutex/lockfile — no cross-process arbitration). A future K8 session must: (1) establish no active C#/Koploper host owns the connection, (2) take the connection over explicitly, (3) never terminate processes or silently start C#.

## Proposed LIVE_HARDWARE scope + GO/NO-GO (future PO grant)

**Honest scope:** the observation is **not passive**. The PO grant must authorize one of: state-changing read-only-enablement (250→100→101→104→106→105), L2-capture of an already-running stream, or warm-reconnect against an already-enabled master.

**GO:** PO explicitly authorizes a non-passive method; single-client ownership established (no active C#/Koploper, explicit takeover); cold-start reuses the KIS-09 guard + allowlist (107/108/123/124 structurally impossible); 105 ENABLE + the subsequent motion-permission broadcast are **reported as state-changing and PO-accepted**; no HR0/PWM/108/RESET/110-heartbeat; freshness via counter-delta + `LastDataReceivedUtc` + `ProtocolReadbackObserved`.

**NO-GO:** any claim that the observation is "passive/read-only" while 105/106 are required; an active C#/Koploper session without an explicit handover agreement; any outbound command outside the allowed set; unproven `SERVICE_TASKS` on warm-reconnect; hardware not confirmed or lacking a fresh PO LIVE_HARDWARE grant.

## Register-map interpretation note

HR11 (application checksum `0x251F`/`0xE906`/`0xFFFF`), HR7 (identity/map), HR21 (fw-version word), HR22 (patch/proto word) must be read per the **verified register map**; Phase-1 HR7/HR21/HR22 semantics must not be assumed on a legacy amplifier (legacy reads 0). HR11 alone cannot exclude an arbitrary legacy build.

## Remaining UNKNOWNs

- Exact Harmony UDP re-bind semantics of the transocket on a second connection attempt (`TransSocketReady` not reset by `DISCONNECTxCLIENT`, `ethernet.c:563-571`) — "first-client-wins" is a code-level inference, not hardware-verified.
- Live HR11 of amps 1/3 (still UNKNOWN); physical flash contents beyond the reported version string.
