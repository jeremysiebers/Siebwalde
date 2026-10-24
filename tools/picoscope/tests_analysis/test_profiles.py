"""Capture profile config tests (config-only; no hardware execution)."""

from __future__ import annotations

import pytest

from tools.picoscope.profiles import (
    PROFILES,
    all_profiles,
    get_profile,
)


def test_all_three_profiles_present():
    assert set(PROFILES.keys()) == {"A", "B", "C"}


def test_profile_a_fields():
    p = get_profile("A").to_dict()
    assert p["key"] == "A"
    assert p["uart_mode"] == "9bit"
    assert p["decode"]["request"]["mode"] == "9bit"
    assert p["decode"]["reply"]["mode"] == "8bit"


def test_profile_b_fields():
    p = get_profile("B").to_dict()
    assert p["decode"]["application"]["mode"] == "9bit"
    assert p["decode"]["bootloader"]["mode"] == "8bit"


def test_profile_c_fields():
    p = get_profile("C").to_dict()
    assert p["decode"]["domain"] == "analog"


def test_profiles_are_proposals_not_proof():
    for p in all_profiles().values():
        assert "PROPOSAL" in p["proposal_note"].upper() or "PROPOSAL" in p["channels"][0]["note"].upper()


def test_unknown_profile_raises():
    with pytest.raises(KeyError):
        get_profile("Z")


def test_all_profiles_json_serializable():
    import json
    json.dumps(all_profiles())  # must not raise
