# PICOSCOPE PICO-4 — Future Hardware Test Procedure (DOCUMENT ONLY)

Date: 2026-10-22. This is a **plan for a controlled first PIC32 UART
measurement**. It has **NOT been executed**, and it requires separate
`LIVE_HARDWARE` (+ any applicable `FIRMWARE_FLASH`) Workflow v1 authority before
it may be run. No observation here is claimed.

## 1. Objective

Capture and decode one clean PIC32 master request (9-bit address-marked) and the
matching PIC18 reply (8-bit) on the RS-422 bus, to confirm the framing model
recorded in `docs/PICOSCOPE-PICO-4-SIGNAL-ANALYSIS.md` §2 against real silicon.
PWM (Profile C) is a later session; do not mix it into the first UART run.

## 2. Safety / electrical

- Power the target (PIC32 master + one amplifier/backplane) from its normal
  bench supply; confirm the RS-422 driver common-mode is within the PicoScope
  digital pod's ±5 V input rating before connecting.
- Use the digital MSO pod (logic threshold, TTL-ish) or 10× passive probes on
  the RS-422 differential pair; **never** connect an analog channel directly to
  an unknown/energized rail. Confirm ground reference (scope ground to target
  ground) first.
- Measurement only: no flashing, no reset toggling beyond what is already
  authorized, no train/motion. Manual stop = disconnect the pod / stop the
  acquisition (process PID recorded).

## 3. Channel mapping (PROPOSAL until verified)

| Signal | Intended channel | Verify |
|---|---|---|
| PIC32 master TX (request) | D6 | confirm with a known address byte |
| PIC18 slave TX (reply) | D7 | confirm with the response |
| (optional) RESET | D0 | only if a reset is to be observed |
| (optional) AMP_ID/slot | D1 | only if backplane select is observed |

Absolute bit position (which wire = D0) is UNVERIFIED; capture a known pattern
first to confirm LSB mapping (see PICO-2 §13).

## 4. Acquisition settings (Profile A)

- Sample interval: **16 ns (tb4)**; fall back to 8 ns (tb3) only if both digital
  ports are used and the combination is confirmed valid by `ps3000aGetTimebase2`.
- Digital ports: port 0 (D0–D7), threshold ~1500 mV (confirm against signal
  swing).
- Trigger: D6, **falling** edge (start bit of a request), pre-samples ~1000,
  post-samples ~60000 (≈1 ms at 16 ns).
- Bounded duration: total capture ≤ 10 s; trigger wait ≤ 5 s.

## 5. Expected observation

- A request telegram: address byte `0x04` (9th bit = 1) followed by function/data
  bytes (9th bit = 0), e.g. `04 03 00 08 00 02` (FC03 read, reg 8, qty 2).
- A reply telegram: 8-bit, e.g. `04 03 04 <hi> <lo> 00 00` (no ninth bit).
- Decoder output (explicit mode): request decoded in 9-bit mode with
  `ninth_bit=[1,0,0,0,0,0,0,0]`; reply decoded in 8-bit mode with
  `ninth_bit=None` and a valid stop bit.

## 6. Expected decoder output / acceptance

- Request frames: 8 frames, first frame `ninth_bit=1`, remaining `ninth_bit=0`.
- Reply frames: 8-bit frames, `stop_ok=True`, `frame_error=False`.
- CRC (bit-reversed convention) validated if the CRC layer is used
  (`tools/hmo2024/uart/crc.py`), but the PICO-4 decoder itself does not verify
  CRC — that is a separate check.

## 7. Stop conditions

- Any clipping / out-of-range voltage, any unexpected bus contention, any
  physical anomaly → stop acquisition, disconnect pod, record state.
- Decoder reports unexpected `frame_error`/`truncated` on a clean capture →
  stop, preserve raw evidence, investigate (do not re-run repeatedly as a
  substitute for understanding the cause).

## 8. Raw-evidence preservation

- Save the raw `.npz` + sidecar JSON via `scope_capture_block`/`scope_export_capture`
  to the dedicated evidence dir; record capture id, revision, device serial,
  threshold, interval, and trigger. Run `scope_analyze_capture` (read-only) to
  produce the decoder evidence without mutating the raw capture.
- Do not edit or regenerate the raw evidence; keep the original sidecar for
  provenance.

## 9. Not executed

This procedure is a plan. No capture, no wiring, and no hardware action has been
performed as part of PICO-4.
