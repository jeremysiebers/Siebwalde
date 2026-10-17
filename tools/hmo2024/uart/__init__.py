"""Siebwalde offline 9-bit UART decoder (software/tooling only).

Decodes the captured POD digital channels (D6 = master request direction,
D7 = slave response direction) into 9-bit UART frames and Modbus-RTU telegrams,
and verifies the project's bit-reversed Modbus CRC-16/MODBUS on-wire convention.

No production code, no firmware, no flash, no live hardware, no train commands.
"""

from . import crc  # noqa: F401
from . import decoder  # noqa: F401
