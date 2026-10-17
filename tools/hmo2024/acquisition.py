"""Bounded single-acquisition state machine (EXPLICIT ACTION MODE).

A capture is a side-effecting action: it issues ``SINGle`` and therefore MUST
be requested explicitly. The machine:

1. requires an explicit confirmation (never runs by default);
2. reads the pre-acquisition state and REFUSES to arm if the scope is already
   running continuously (we never silently STOP an operator's run);
3. issues a single ``SINGle`` (no STOP, no RUN);
4. polls a documented completion indicator (``ACQ:STAT?`` -> ``COMPlete``);
5. fails explicitly on no-trigger / timeout / non-completion / interruption;
6. records pre/post state and a "hold token" (header + point count + state)
   used later to detect a rearm before export;
7. deliberately does NOT resume (no RUN after completion), and records what was
   (not) restored.

State transitions:

  IDLE -> RUNNING (SINGle sent) -> COMPLETE (HELD)
                                  -> FAIL_NO_TRIGGER / FAIL_TIMEOUT
                                  -> FAIL_INTERRUPTED (BREAK)
  IDLE -> FAIL_NOT_IDLE (scope already running; refused)
"""

from __future__ import annotations

import time
from typing import Dict, List, Optional

import scpi
from records import RecordManager, capture_id
from transport import HmoTransport, TransportError

# Canonical acquisition states (normalized).
ST_RUN = "RUN"
ST_STOP = "STOP"
ST_COMP = "COMP"
ST_BREAK = "BREAK"

# States we accept as an idle starting point for a bounded single acquisition.
IDLE_STATES = (ST_COMP, ST_STOP, ST_BREAK)

DEFAULT_CAPTURE_TIMEOUT = 20.0
DEFAULT_POLL_INTERVAL = 0.2


class CaptureError(Exception):
    pass


class CaptureNotConfirmed(CaptureError):
    pass


class ScopeNotIdle(CaptureError):
    pass


class NoTrigger(CaptureError):
    pass


class CaptureTimeout(CaptureError):
    pass


class CaptureInterrupted(CaptureError):
    pass


class CaptureSession:
    def __init__(
        self,
        transport: HmoTransport,
        records: RecordManager,
        port: str,
        baud: int,
        tool: Dict[str, str],
        confirm: bool = False,
        timeout: float = DEFAULT_CAPTURE_TIMEOUT,
        poll_interval: float = DEFAULT_POLL_INTERVAL,
        channel: int = 1,
        clock=time.monotonic,
    ):
        self.transport = transport
        self.records = records
        self.port = port
        self.baud = baud
        self.tool = tool
        self.confirm = confirm
        self.timeout = timeout
        self.poll_interval = poll_interval
        self.channel = channel
        self._clock = clock
        self.cap_id: Optional[str] = None
        self.cap_dir: Optional[str] = None
        self.manifest: Optional[Dict] = None

    # -- helpers ----------------------------------------------------------
    def _acq_state(self) -> str:
        raw = self.transport.query("ACQ:STAT?")
        return scpi.normalize_acq_state(raw)

    def _hold_token(self) -> Dict:
        """Header + point count + acquisition state that later export checks."""
        token = {
            "channel": self.channel,
            "acq_state": self._acq_state(),
            "header_raw": None,
            "header": None,
            "point_count_raw": None,
            "point_count": None,
        }
        for key, cmd in (
            ("header_raw", f"CHAN{self.channel}:DATA:HEAD?"),
            ("point_count_raw", f"CHAN{self.channel}:DATA:POIN?"),
        ):
            try:
                token[key] = self.transport.query(cmd)
            except TransportError as exc:
                token[key + "_error"] = f"{type(exc).__name__}: {exc}"
        if token.get("header_raw"):
            try:
                token["header"] = scpi.parse_header(token["header_raw"]).__dict__
            except scpi.ScpiParseError as exc:
                token["header_error"] = str(exc)
        if token.get("point_count_raw"):
            try:
                token["point_count"] = int(token["point_count_raw"].strip())
            except ValueError:
                token["point_count"] = None
        return token

    # -- the action -------------------------------------------------------
    def arm_and_wait(self) -> Dict:
        """Run one bounded single acquisition and hold the result.

        Returns the manifest dict. Raises CaptureError subclasses on failure.
        """
        if not self.confirm:
            raise CaptureNotConfirmed(
                "capture is an explicit action; pass confirm=True (CLI: --confirm)"
            )

        pre_state = self._acq_state()
        flags: List[str] = []
        if pre_state == ST_RUN:
            # Fail-closed: do NOT silently stop an operator's running scope.
            raise ScopeNotIdle(
                "scope acquisition is RUNNING; refusing to arm without an "
                "explicit operator stop (we do not silently STOP)"
            )
        if pre_state not in IDLE_STATES:
            flags.append(f"pre_state_unexpected:{pre_state}")

        # Record the pre-capture inventory-relevant state (state + trigger source).
        pre_state_rec = {
            "acq_state": pre_state,
            "trigger_mode": self._safe_query("TRIG:A:MODE?"),
            "trigger_source": self._safe_query("TRIG:A:SOUR?"),
        }

        self.cap_id = capture_id()
        self.cap_dir = self.records.new_capture_dir(self.cap_id)
        self.manifest = self.records.base_manifest(
            self.cap_id, self.port, self.baud, self.tool
        )
        self.manifest["instrument"]["id"] = self._safe_query("*IDN?")
        self.manifest["acquisition"] = {
            "command": "SINGle",
            "pre_state": pre_state_rec,
            "started_at_monotonic": self._clock(),
            "result": None,
            "post_state": None,
            "duration_s": None,
        }

        # Issue the single acquisition (no STOP, no RUN).
        self.transport.write("SINGle")

        start = self._clock()
        final_state: Optional[str] = None
        last_state: Optional[str] = pre_state
        while True:
            elapsed = self._clock() - start
            if elapsed > self.timeout:
                if last_state == ST_RUN:
                    raise NoTrigger(
                        f"no trigger: acquisition still RUNNING after "
                        f"{self.timeout}s"
                    )
                raise CaptureTimeout(
                    f"acquisition did not complete within {self.timeout}s "
                    f"(last state {last_state})"
                )
            try:
                state = self._acq_state()
            except TransportError:
                state = None
            if state is not None:
                last_state = state
            if state == ST_COMP:
                final_state = ST_COMP
                break
            if state == ST_BREAK:
                final_state = ST_BREAK
                raise CaptureInterrupted(
                    "acquisition finished but interrupted (BREAK)"
                )
            # ST_RUN / ST_STOP / unknown -> keep polling.
            time.sleep(self.poll_interval)

        duration = self._clock() - start
        hold = self._hold_token()
        self.manifest["acquisition"]["result"] = "COMPLETE"
        self.manifest["acquisition"]["post_state"] = final_state
        self.manifest["acquisition"]["duration_s"] = round(duration, 4)
        self.manifest["hold"] = hold
        self.manifest["flags"] = flags
        self.manifest["restore"] = {
            "run_state_restored": False,
            "note": (
                "acquisition left in COMPlete (stopped); we deliberately do NOT "
                "resume (no RUN) after an operator or tool stop"
            ),
        }
        self.records.write_manifest(self.cap_dir, self.manifest)
        return self.manifest

    def _safe_query(self, cmd: str) -> Optional[str]:
        try:
            return self.transport.query(cmd)
        except TransportError as exc:
            return f"ERROR:{type(exc).__name__}"
