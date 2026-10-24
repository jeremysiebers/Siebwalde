"""K6-B: exactly one physical command-113 OBSERVE transaction.

This script is the ONLY sanctioned live entry point for K6-B. It performs a
single bounded, read-only diagnostic transaction against the allowlisted PIC32
endpoint:

    1. construct an :class:`ObserveSession` and consume the single-use
       authorization exactly once (before any I/O);
    2. send exactly one ``GET_CONTROLLER_VERSION`` (113) request through the
       allowlisted :class:`Command113Transport` and receive one bounded reply;
    3. write durable evidence (request/response bytes, hashes, timestamps,
       classification) to a git-ignored evidence directory.

There is no retry, no recovery, no flash/reset, and no other command. A timeout
or socket error is recorded as OBSERVE_UNKNOWN/OBSERVE_FAILED and the process
exits without any second attempt.
"""

from __future__ import annotations

import json
import sys
from datetime import datetime, timezone
from pathlib import Path

from tools.diagnostics.live_transport import Command113Transport
from tools.diagnostics.observe_guard import ObserveSession

ENDPOINT = "192.168.1.193"
TIMEOUT = 5.0
EVIDENCE_DIR = Path("Logging/K6B")
# Fixed safety-authorization identifier for this single K6-B session.
SAFETY_AUTHORIZATION_ID = "K6B-SINGLE-USE-113-192.168.1.193"


def main() -> int:
    run_id = datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ")
    EVIDENCE_DIR.mkdir(parents=True, exist_ok=True)

    # 1. Single-use authorization, consumed exactly once BEFORE the send.
    #    A second consume() would raise AuthorizationBlocked; there is no loop.
    session = ObserveSession(command_id=113, endpoint=ENDPOINT, timeout=TIMEOUT)
    auth = session.consume()  # AUTHORIZED_COMMAND_COUNT_REMAINING 1 -> 0

    # 2. Exactly one command-113 request + one bounded receive (no retry).
    transport = Command113Transport(endpoint=ENDPOINT, timeout=TIMEOUT)
    result = transport.send_get_controller_version()

    status = result.get("status")
    if status == "PASS":
        classification = "OBSERVE_PASS"
    elif status == "FAILED":
        classification = "OBSERVE_FAILED"
    else:
        classification = "OBSERVE_UNKNOWN"

    record = {
        "run_id": run_id,
        "safety_authorization_id": SAFETY_AUTHORIZATION_ID,
        "request_count": 1,
        "endpoint": ENDPOINT,
        "command_id": 113,
        "authorization": auth,
        "result": result,
        "classification": classification,
        "recorded_at": datetime.now(timezone.utc).isoformat(),
    }

    evidence_path = EVIDENCE_DIR / f"k6b-observe-{run_id}.json"
    evidence_path.write_text(json.dumps(record, indent=2), encoding="utf-8")

    print(json.dumps(record, indent=2))
    print(f"EVIDENCE: {evidence_path}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
