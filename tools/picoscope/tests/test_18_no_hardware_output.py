"""Category 18: absence of any hardware-output / AWG / flash / motion surface.

This is a static source-surface test: it asserts that neither the public API nor
the vendor adapter exposes, or even references, signal-generation / AWG / flash /
reset / motion / backplane operations. The picoscope tooling is measurement-only.
"""

import inspect
import os

from tools.picoscope.backend.adapters import picosdk as _picosdk
from tools.picoscope.backend.adapters import simulator as _simulator
from tools.picoscope.backend.api import PicoScope

#: Forbidden operation substrings in public method/property names.
FORBIDDEN_METHOD_TOKENS = (
    "awg",
    "sig_gen",
    "signal_generator",
    "output",
    "flash",
    "reset",
    "motion",
    "backplane",
    "write",
    "set_waveform",
)

#: Forbidden vendor API identifiers that must never appear in the ps3000a adapter.
FORBIDDEN_VENDOR_TOKENS = (
    "SetSigGen",
    "SetSignalGenerator",
    "SetAWG",
    "ArbitraryWaveform",
    "SignalGenerator",
    "FlashProgram",
    "ResetDevice",
)


def _public_names(obj) -> list:
    return [n for n in dir(obj) if not n.startswith("_")]


def test_public_api_has_no_forbidden_operation_names():
    for name in _public_names(PicoScope):
        for token in FORBIDDEN_METHOD_TOKENS:
            assert token not in name.lower(), f"PicoScope exposes forbidden {name!r}"


def test_adapter_classes_have_no_forbidden_operation_names():
    classes = [
        _simulator.SimulatedDevice,
        _simulator.SimulatedAdapter,
        _picosdk.Ps3000aDevice,
        _picosdk.Ps3000aAdapter,
    ]
    for cls in classes:
        for name in _public_names(cls):
            for token in FORBIDDEN_METHOD_TOKENS:
                assert token not in name.lower(), f"{cls.__name__} exposes forbidden {name!r}"


def test_picosdk_source_references_no_vendor_awg_flash_identifiers():
    src = inspect.getsource(_picosdk)
    for token in FORBIDDEN_VENDOR_TOKENS:
        assert token not in src, f"ps3000a adapter references forbidden vendor API {token!r}"


def test_ps3000a_adapter_only_binds_measurement_functions():
    # The adapter may bind only measurement/config/teardown ps3000a entry points.
    # Parse the AST so docstring/comment prose is not mistaken for a binding.
    import ast

    allowed = {
        "ps3000aOpenUnit",
        "ps3000aCloseUnit",
        "ps3000aGetUnitInfo",
        "ps3000aSetChannel",
        "ps3000aSetDigitalPort",
        "ps3000aSetSimpleTrigger",
        "ps3000aGetTimebase2",
        "ps3000aSetDataBuffers",
        "ps3000aRunBlock",
        "ps3000aIsReady",
        "ps3000aGetValues",
        "ps3000aStop",
    }
    with open(_picosdk.__file__, "r", encoding="utf-8") as fh:
        tree = ast.parse(fh.read())
    referenced = {
        node.attr
        for node in ast.walk(tree)
        if isinstance(node, ast.Attribute) and node.attr.startswith("ps3000a")
    }
    assert referenced, "expected to find ps3000a entry points in the adapter"
    unexpected = referenced - allowed
    assert not unexpected, f"unexpected ps3000a entry points: {sorted(unexpected)}"


def test_measurement_only_docs_present():
    # The safety contract is documented (SAFETY.md) next to the package.
    safety_path = os.path.join(
        os.path.dirname(_picosdk.__file__), "..", "..", "SAFETY.md"
    )
    assert os.path.exists(safety_path)
