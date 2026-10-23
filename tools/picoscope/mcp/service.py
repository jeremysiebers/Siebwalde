"""Allowlisted, measurement-only scope service behind the MCP tools.

This is the single place where the MCP layer touches the backend. It calls a
*strict allowlist* of Layer A measurement functions and nothing else:

    discover_devices, open_device, PicoScope.get_device_info,
    PicoScope.configure_analog_channel, PicoScope.configure_digital_port,
    PicoScope.configure_trigger, PicoScope.capture_block, save_capture,
    load_capture, PicoScope.close_device

No AWG / signal-generation, no arbitrary PicoSDK command, no flash / reset /
motion / Siebwalde state, no shell command. This allowlist is enforced by a
static test (``tests_mcp/test_19_no_unsafe_output_op.py``).
"""

from __future__ import annotations

import os
import re
import threading

from tools.picoscope.backend import api as _backend_api
from tools.picoscope.backend.errors import (
    DeviceBusyError,
    InvalidConfigError,
    PicoScopeError,
)
from tools.picoscope.backend.model import Capture

from .config import ServerConfig
from .evidence import EvidenceError, EvidenceStore
from .lock import CrossProcessLock
from .schema import AcquisitionProfile, AnalysisRequest

#: Validate a digital line name (D0..D15) for analyzer settings.
_DIGITAL_LINE_RE = re.compile(r"^D([0-9]|1[0-5])$")

#: Validate an analog channel name (A..D) for analyzer settings.
_ANALOG_CHANNEL_RE = re.compile(r"^[A-D]$")

#: Canonical backend name for each accepted alias (mirrors backend/api.py).
_BACKEND_ALIASES = {
    "simulator": "simulator",
    "ps3000a": "ps3000a",
    "picosdk": "ps3000a",
    "real": "ps3000a",
}

#: 3406D MSO capability surface (measurement-only; no AWG exposed).
_CAPABILITIES = {
    "analog_channels": 4,
    "digital_ports": 2,
    "digital_lines": 16,
    "block_capture": True,
    "measurement_only": True,
}


class ServiceError(PicoScopeError):
    """An MCP-layer rejection (validation, gate, or evidence) surfaced to the client."""


class ScopeService:
    """The measurement service. One instance per server process."""

    def __init__(self, config: ServerConfig | None = None):
        self.config = config or ServerConfig.from_env()
        self.evidence = EvidenceStore(self.config.evidence_dir)
        # In-process guard: reject a second acquisition while one is in flight.
        self._in_flight = threading.Lock()
        # One-shot simulator fault injection (test-only; consumed after first use).
        self._fault_pending = self.config.test_fault

    # ------------------------------------------------------------------ #
    # scope_info
    # ------------------------------------------------------------------ #
    def scope_info(self, backend: str = "simulator") -> dict:
        """Compact device identity + ownership status. Non-owning discovery."""
        canonical = _canonical_backend(backend)

        if canonical == "ps3000a" and not self.config.allow_real_backend:
            result = {
                "model": "3406D MSO",
                "backend": "ps3000a",
                "serial": None,
                "variant": None,
                "sdk_driver": None,
                "capabilities": _CAPABILITIES,
                "ownership": "gated",
                "simulated": False,
                "real_backend_gate": "closed",
                "limits": self.config.limit_doc(),
            }
            self._bound_response(result)
            return result

        devices = _backend_api.discover_devices(canonical)
        first = devices[0] if devices else None
        serial = first["serial"] if first else None
        ownership = self._ownership_status(canonical, serial) if serial else "none"

        result = {
            "model": (first["model"] if first else "3406D MSO"),
            "backend": canonical,
            "serial": serial,
            "variant": first["variant"] if first else None,
            "sdk_driver": first["driver_version"] if first else None,
            "capabilities": _CAPABILITIES,
            "ownership": ownership,
            "simulated": canonical == "simulator",
            "real_backend_gate": "open" if self.config.allow_real_backend else "closed",
            "limits": self.config.limit_doc(),
        }
        self._bound_response(result)
        return result

    # ------------------------------------------------------------------ #
    # scope_capture_block
    # ------------------------------------------------------------------ #
    def capture_block(self, profile: AcquisitionProfile) -> dict:
        """Validate a profile, run one bounded capture, save evidence.

        Ordering guarantees:

        1. interval-independent limits are enforced (sample count, channels,
           trigger wait, memory, file budget);
        2. the simulator's sample interval is resolved and duration / total-timeout
           limits are enforced — all *before* any backend/SDK call;
        3. the backend's own domain validation runs on the mapped config;
        4. only then is the cross-process lock taken and the device opened.
        """
        if not self._in_flight.acquire(blocking=False):
            raise DeviceBusyError("Another acquisition is already in progress")

        try:
            return self._capture_block(profile)
        finally:
            self._in_flight.release()

    def _capture_block(self, profile: AcquisitionProfile) -> dict:
        canonical = _canonical_backend(profile.backend)
        if canonical == "ps3000a" and not self.config.allow_real_backend:
            raise ServiceError(
                "Real-device acquisition requires the operator gate "
                "(PICOSCOPE_MCP_ALLOW_REAL=1). No simulator fallback."
            )

        # Normalize the EFFECTIVE acquisition config first. When a trigger is
        # supplied, the capture uses the trigger's pre/post split (see
        # ``_configure_and_capture``), not the top-level pre/post. The
        # interval-independent budget (sample count / memory / file size) must
        # therefore be computed from the effective split, so an out-of-budget
        # trigger split is rejected before any SDK call.
        pre, post = self._effective_pre_post(profile)
        sample_count = pre + post
        self._validate_interval_independent(profile, sample_count)
        self._pre_validate_domain(profile)

        # Resolve the sample interval up front where possible (simulator), so
        # duration/total-timeout limits are enforced before any SDK call.
        interval_ns = self._resolve_interval(canonical, profile.timebase)
        if interval_ns is not None:
            self._validate_duration(profile, sample_count, interval_ns)

        device_id = self._resolve_device_id(canonical, profile.device_id)
        lock = CrossProcessLock(self.evidence.root, device_id)

        try:
            lock.acquire()  # raises DeviceBusyError if another process owns it
        except DeviceBusyError:
            raise
        try:
            scope = _backend_api.open_device(device_id, canonical)
            try:
                capture = self._configure_and_capture(scope, profile, canonical)
                capture = self._annotate(capture, profile, canonical)
                capture_id = capture.capture_id
                base = self.evidence.base_path(capture_id)
                _backend_api.save_capture(capture, base)
                return self._capture_result(capture, capture_id, canonical)
            finally:
                scope.close_device()
        finally:
            lock.release()

    def _configure_and_capture(self, scope, profile: AcquisitionProfile, canonical: str) -> Capture:
        for ch in profile.analog_channels:
            scope.configure_analog_channel(ch.channel, ch.enabled, ch.range_v, ch.coupling)
        for port in profile.digital_ports:
            scope.configure_digital_port(port.port, port.enabled, port.threshold_mv)
        if profile.trigger is not None:
            t = profile.trigger
            scope.configure_trigger(t.source, t.threshold, t.edge, t.pre_samples, t.post_samples)
            pre, post = t.pre_samples, t.post_samples
        else:
            pre, post = profile.pre_samples, profile.post_samples

        self._inject_test_fault(scope, canonical)

        capture = scope.capture_block(
            pre_samples=pre,
            post_samples=post,
            timebase=profile.timebase,
            trigger_wait_ms=profile.trigger_wait_ms,
        )

        # Post-capture duration check for backends whose interval is only known
        # after acquisition (the real ps3000a path resolves it via GetTimebase2).
        if canonical != "simulator":
            self._validate_duration(profile, capture.sample_count, capture.sample_interval_ns)
        return capture

    # ------------------------------------------------------------------ #
    # scope_get_capture_metadata / scope_export_capture
    # ------------------------------------------------------------------ #
    def get_capture_metadata(self, capture_id: str) -> dict:
        """Read the metadata of an existing capture (no device, no array load)."""
        base = self.evidence.base_path(capture_id)  # validates id + containment
        meta = self._read_sidecar(base)
        compact = self._compact_metadata(meta)
        compact["artifact_paths"] = self.evidence.artifact_paths(capture_id)
        compact["simulated"] = meta.get("provenance", {}).get("simulated", False)
        compact["backend"] = meta.get("device_identity", {}).get("backend")
        self._bound_response(compact)
        return compact

    def export_capture(self, capture_id: str) -> dict:
        """Return validated artifact references + metadata (no raw arrays)."""
        base = self.evidence.base_path(capture_id)
        meta = self._read_sidecar(base)
        result = {
            "capture_id": capture_id,
            "artifact_paths": self.evidence.artifact_paths(capture_id),
            "metadata": self._compact_metadata(meta),
            "simulated": meta.get("provenance", {}).get("simulated", False),
            "backend": meta.get("device_identity", {}).get("backend"),
            "result": "ok",
        }
        self._bound_response(result)
        return result

    # ------------------------------------------------------------------ #
    # scope_analyze_capture (read-only offline analysis)
    # ------------------------------------------------------------------ #
    def analyze_capture(self, request: AnalysisRequest) -> dict:
        """Run one allowlisted analyzer over an existing capture (read-only).

        Loads the capture's ``.npz`` + sidecar JSON from the evidence dir, runs
        the allowlisted analyzer with validated settings, saves the detailed
        analysis as ``<capture_id>.analysis.<analyzer>.json`` inside the evidence
        dir, and returns a compact summary. No device, no hardware, no arbitrary
        path/script/SDK call.
        """
        base = self.evidence.base_path(request.capture_id)  # validates id + containment

        from tools.picoscope.analysis import io as _analysis_io

        capture = _backend_api.load_capture(base)

        if request.analyzer == "uart":
            result = self._analyze_uart(capture, request.settings or {})
            summary = result.get("summary", {})
        elif request.analyzer == "pwm_timing":
            result = self._analyze_pwm(capture, request.settings or {})
            summary = result.get("summary", {})
        else:  # pragma: no cover - guarded by the schema Literal
            raise ServiceError(f"Unknown analyzer {request.analyzer!r}")

        out_path = base + f".analysis.{request.analyzer}.json"
        _analysis_io.export_analysis_json(result, out_path)

        payload = {
            "capture_id": request.capture_id,
            "analyzer": request.analyzer,
            "summary": summary,
            "analysis_path": out_path,
            "result": "ok",
            "simulated": capture.provenance.get("simulated", False),
            "backend": capture.device_identity.backend,
        }
        self._bound_response(payload)
        return payload

    # -- analyzer settings validation ------------------------------------- #
    @staticmethod
    def _require_settings_keys(settings: dict, allowed: set, analyzer: str) -> dict:
        unknown = set(settings) - allowed
        if unknown:
            raise InvalidConfigError(
                f"Unknown {analyzer} setting(s): {sorted(unknown)}; allowed: {sorted(allowed)}"
            )
        return dict(settings)

    def _analyze_uart(self, capture, settings: dict) -> dict:
        from tools.picoscope.analysis import uart as _uart
        from tools.picoscope.analysis.io import extract_digital_line

        allowed = {"line", "mode", "baud", "polarity", "stop_bits", "mode_source", "idle_gap_bits"}
        s = self._require_settings_keys(settings, allowed, "uart")
        line = s.get("line")
        mode = s.get("mode")
        baud = s.get("baud")
        if not isinstance(line, str) or not _DIGITAL_LINE_RE.match(line):
            raise InvalidConfigError("uart setting 'line' must be a digital line D0..D15")
        if mode not in ("8bit", "9bit"):
            raise InvalidConfigError("uart setting 'mode' must be '8bit' or '9bit'")
        if not isinstance(baud, int) or baud <= 0:
            raise InvalidConfigError("uart setting 'baud' must be a positive int")

        samples = extract_digital_line(capture, line)
        cfg = _uart.UartConfig(
            mode=mode,
            baud=baud,
            sample_interval_ns=capture.sample_interval_ns,
            polarity=s.get("polarity", "idle_high"),
            stop_bits=s.get("stop_bits", 1),
            mode_source=s.get("mode_source", "explicit"),
            idle_gap_bits=s.get("idle_gap_bits"),
        )
        return _uart.decode_uart(samples, cfg)

    def _analyze_pwm(self, capture, settings: dict) -> dict:
        from tools.picoscope.analysis import timing as _timing
        from tools.picoscope.analysis.io import extract_analog_channel, extract_digital_line

        allowed = {"source", "domain", "threshold"}
        s = self._require_settings_keys(settings, allowed, "pwm_timing")
        source = s.get("source")
        if not isinstance(source, str):
            raise InvalidConfigError("pwm_timing setting 'source' is required")
        if _ANALOG_CHANNEL_RE.match(source):
            domain = "analog"
            samples = extract_analog_channel(capture, source)
        elif _DIGITAL_LINE_RE.match(source):
            domain = "digital"
            samples = extract_digital_line(capture, source)
        else:
            raise InvalidConfigError("pwm_timing setting 'source' must be A..D or D0..D15")

        domain_override = s.get("domain")
        if domain_override is not None and domain_override != domain:
            raise InvalidConfigError(
                f"pwm_timing 'domain' {domain_override!r} is inconsistent with source {source!r}"
            )
        threshold = s.get("threshold")
        if threshold is not None and domain != "analog":
            raise InvalidConfigError("pwm_timing 'threshold' is only valid for analog sources")

        m = _timing.analyze_pwm(
            samples,
            capture.sample_interval_ns,
            domain=domain,
            threshold=threshold,
        )
        d = m.to_dict()
        d["summary"] = {
            "frequency_hz": d["frequency_hz"],
            "period_ns": d["period_ns"],
            "duty": d["duty"],
            "complete_cycles": d["complete_cycles"],
            "rising_edge_count": len(d["rising_edges_ns"]),
            "falling_edge_count": len(d["falling_edges_ns"]),
            "domain": d["domain"],
        }
        return d

    # ------------------------------------------------------------------ #
    # validation
    # ------------------------------------------------------------------ #
    @staticmethod
    def _effective_pre_post(profile: AcquisitionProfile) -> tuple[int, int]:
        """Return the (pre, post) split the capture will actually use.

        A supplied trigger carries its own pre/post split and wins over the
        top-level ``pre_samples``/``post_samples`` (see
        :meth:`_configure_and_capture`). This is the single normalization point
        for interval-independent budgeting.
        """
        if profile.trigger is not None:
            return profile.trigger.pre_samples, profile.trigger.post_samples
        return profile.pre_samples, profile.post_samples

    def _validate_interval_independent(self, profile: AcquisitionProfile, sample_count: int) -> None:
        cfg = self.config
        if sample_count > cfg.max_sample_count:
            raise InvalidConfigError(
                f"sample_count {sample_count} exceeds max_sample_count {cfg.max_sample_count}"
            )
        analog_enabled = sum(1 for c in profile.analog_channels if c.enabled)
        if analog_enabled > cfg.max_analog_channels:
            raise InvalidConfigError(
                f"{analog_enabled} analog channels exceed max_analog_channels "
                f"{cfg.max_analog_channels}"
            )
        digital_enabled = sum(1 for p in profile.digital_ports if p.enabled)
        if digital_enabled > cfg.max_digital_ports:
            raise InvalidConfigError(
                f"{digital_enabled} digital ports exceed max_digital_ports {cfg.max_digital_ports}"
            )
        if profile.trigger_wait_ms > cfg.max_trigger_wait_ms:
            raise InvalidConfigError(
                f"trigger_wait_ms {profile.trigger_wait_ms} exceeds max_trigger_wait_ms "
                f"{cfg.max_trigger_wait_ms}"
            )
        digital_lines = 8 * digital_enabled
        est_bytes = sample_count * (8 * analog_enabled + 1 * digital_lines)
        if est_bytes > cfg.max_memory_bytes:
            raise InvalidConfigError(
                f"estimated raw data {est_bytes} bytes exceeds max_memory_bytes "
                f"{cfg.max_memory_bytes}"
            )
        if est_bytes > cfg.max_capture_file_bytes:
            raise InvalidConfigError(
                f"estimated capture size {est_bytes} bytes exceeds max_capture_file_bytes "
                f"{cfg.max_capture_file_bytes}"
            )

    def _pre_validate_domain(self, profile: AcquisitionProfile) -> None:
        """Re-validate the mapped profile against the backend's domain rules.

        Builds the same :class:`AcquisitionConfig` the backend will use and calls
        its ``validate()`` so "at least one channel enabled" and "trigger source
        must be enabled" are rejected *before* the cross-process lock is taken or
        any device is opened.
        """
        from tools.picoscope.backend.config import AcquisitionConfig
        from tools.picoscope.backend.model import (
            AnalogChannelConfig,
            DigitalPortConfig,
            TriggerConfig,
        )

        cfg = AcquisitionConfig(
            analog_channels=[
                AnalogChannelConfig(channel=c.channel, enabled=c.enabled, range_v=c.range_v, coupling=c.coupling)
                for c in profile.analog_channels
            ],
            digital_ports=[
                DigitalPortConfig(port=p.port, enabled=p.enabled, threshold_mv=p.threshold_mv)
                for p in profile.digital_ports
            ],
            trigger=None
            if profile.trigger is None
            else TriggerConfig(source=profile.trigger.source, threshold=profile.trigger.threshold, edge=profile.trigger.edge),
            pre_samples=profile.trigger.pre_samples if profile.trigger else profile.pre_samples,
            post_samples=profile.trigger.post_samples if profile.trigger else profile.post_samples,
            timebase=profile.timebase,
        )
        cfg.validate()

    def _validate_duration(self, profile: AcquisitionProfile, sample_count: int, interval_ns: int) -> None:
        cfg = self.config
        duration_s = sample_count * interval_ns / 1e9
        if duration_s > cfg.max_duration_s:
            raise InvalidConfigError(
                f"capture duration {duration_s:.6f}s exceeds max_duration_s {cfg.max_duration_s}s"
            )
        total_s = duration_s + profile.trigger_wait_ms / 1000.0
        if total_s > cfg.total_acquisition_timeout_s:
            raise InvalidConfigError(
                f"estimated total acquisition time {total_s:.6f}s exceeds "
                f"total_acquisition_timeout_s {cfg.total_acquisition_timeout_s}s"
            )

    # ------------------------------------------------------------------ #
    # helpers
    # ------------------------------------------------------------------ #
    def _resolve_interval(self, canonical: str, timebase: int) -> int | None:
        """Resolve the sample interval without opening a device (simulator only)."""
        if canonical != "simulator":
            return None
        from tools.picoscope.backend.adapters import simulator as _sim

        return _sim.default_adapter.resolve_timebase(timebase)

    def _resolve_device_id(self, canonical: str, device_id: str | None) -> str:
        if device_id is not None:
            return device_id
        devices = _backend_api.discover_devices(canonical)
        if not devices:
            from tools.picoscope.backend.errors import DeviceNotFoundError

            raise DeviceNotFoundError(f"No {canonical} device found")
        return devices[0]["serial"]

    def _ownership_status(self, canonical: str, serial: str) -> str:
        from tools.picoscope.backend import ownership as _ownership

        if _ownership.is_owned(serial):
            return "busy"
        probe = CrossProcessLock(self.evidence.root, serial)
        try:
            probe.acquire()
        except DeviceBusyError:
            return "busy"
        finally:
            probe.release()
        return "available"

    def _inject_test_fault(self, scope, canonical: str) -> None:
        """Test-only, one-shot simulator fault injection (env-gated, disabled by default)."""
        fault = self._fault_pending
        if not fault or canonical != "simulator":
            return
        raw = getattr(scope, "device", None)
        if raw is None:
            return
        if fault == "inject_timeout" and hasattr(raw, "inject_timeout"):
            raw.inject_timeout()
            self._fault_pending = None
        elif fault.startswith("inject_error") and hasattr(raw, "inject_error"):
            raw.inject_error(fault[len("inject_error:"):] or "injected device error")
            self._fault_pending = None

    def _annotate(self, capture: Capture, profile: AcquisitionProfile, canonical: str) -> Capture:
        capture.provenance = dict(capture.provenance)
        capture.provenance["mcp_layer"] = "pico-3"
        capture.provenance["evidence_kind"] = "simulated" if canonical == "simulator" else "real"
        if profile.notes:
            capture.provenance["mcp_notes"] = profile.notes
        return capture

    def _capture_result(self, capture: Capture, capture_id: str, canonical: str) -> dict:
        trigger_status = capture.trigger_status
        if trigger_status is None:
            # Backward-compatible inference for captures without trigger_status.
            if capture.trigger is None:
                trigger_status = "unused"
            elif capture.acquisition_status == "OK":
                trigger_status = "triggered"
            else:
                trigger_status = "unknown"
        result = {
            "capture_id": capture_id,
            "sample_interval_ns": capture.sample_interval_ns,
            "sample_count": capture.sample_count,
            "trigger_status": trigger_status,
            "trigger_position": capture.trigger_position,
            "trigger_index": capture.trigger_index,
            "channel_mapping": capture.channel_mapping,
            "artifact_paths": self.evidence.artifact_paths(capture_id),
            "warnings": list(capture.warnings),
            "result": "ok",
            "simulated": canonical == "simulator",
            "backend": canonical,
        }
        self._bound_response(result)
        return result

    def _read_sidecar(self, base: str) -> dict:
        """Read only the JSON sidecar of a capture (never the raw ``.npz``)."""
        import json

        json_path = base + ".json"
        try:
            with open(json_path, "r", encoding="utf-8") as fh:
                return json.load(fh)
        except FileNotFoundError:
            raise EvidenceError(f"No capture found at {base}") from None
        except Exception as exc:
            raise EvidenceError(f"Failed to read capture metadata: {exc}") from exc

    @staticmethod
    def _compact_metadata(meta: dict) -> dict:
        """Project a sidecar dict onto compact metadata (no raw sample data)."""
        device = meta.get("device_identity", {})
        return {
            "capture_id": meta.get("capture_id"),
            "timestamp": meta.get("timestamp"),
            "device": {
                "model": device.get("model"),
                "serial": device.get("serial"),
                "backend": device.get("backend"),
                "variant": device.get("variant"),
                "driver_version": device.get("driver_version"),
            },
            "sample_interval_ns": meta.get("sample_interval_ns"),
            "sample_count": meta.get("sample_count"),
            "analog_channels": meta.get("analog_channels", []),
            "digital_ports": meta.get("digital_ports", []),
            "trigger": meta.get("trigger"),
            "trigger_position": meta.get("trigger_position"),
            "trigger_status": meta.get("trigger_status"),
            "trigger_index": meta.get("trigger_index"),
            "channel_mapping": meta.get("channel_mapping"),
            "acquisition_status": meta.get("acquisition_status"),
            "errors": meta.get("errors", []),
            "warnings": meta.get("warnings", []),
            # dtype/shape descriptor only — the raw values live in the .npz.
            "samples": meta.get("samples", {}),
            "provenance": meta.get("provenance", {}),
        }

    def _bound_response(self, payload: dict) -> None:
        import json

        size = len(json.dumps(payload, sort_keys=True).encode("utf-8"))
        if size > self.config.max_metadata_response_bytes:
            raise ServiceError(
                f"Response metadata too large ({size} bytes > "
                f"{self.config.max_metadata_response_bytes} bytes)"
            )


def _canonical_backend(backend: str) -> str:
    canonical = _BACKEND_ALIASES.get(backend)
    if canonical is None:
        raise ServiceError(
            f"Unknown backend {backend!r}; expected one of {sorted(set(_BACKEND_ALIASES))}"
        )
    return canonical
