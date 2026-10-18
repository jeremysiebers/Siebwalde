"""Modbus CRC-16 helpers for the Siebwalde 9-bit UART decoder.

The project uses a **bit-reversed Modbus CRC-16/MODBUS** on the wire (NOT the
standard byte order).  Verified against the firmware sources:

* ``TrackAmplifier4.X/modbus/PetitModbus.c`` (PIC18 slave)
* ``TrackController5/firmware/src/modbus/PetitModbus.c`` (PIC32 master)

Both build configurations emit the same on-wire value:

    on_wire_crc = bit_reverse16( crc16_modbus(payload) )

transmitted **low byte first, high byte second**.  ``payload`` is every byte of
the telegram except the two trailing CRC bytes (address, function, data...).

References in the firmware source:

* ``Petit_CRC16`` with ``CRC_CALC`` (poly ``0xA001``, init ``0xFFFF``) and
  ``CRC_LOOKUP`` (standard ``auchCRCHi``/``auchCRCLo`` tables) both compute the
  ordinary Modbus CRC-16/MODBUS.
* ``CRC_HW`` (Microchip hardware, reflected poly ``0x8005``) computes the
  bit-reversal of the ordinary CRC directly (no further reverse needed).
* ``CRC_HW_REVERSE`` applies ``CRC_ReverseValue`` (a full 16-bit bit-reversal)
  after ``CRC_LOOKUP``/``CRC_HW``, so every configuration lands on
  ``bit_reverse16(standard)``.
* ``Petit_TxRTU`` appends ``crc & 0x00FF`` then ``(crc & 0xFF00) >> 8``
  (low byte first).
"""

from __future__ import annotations


def crc16_modbus(data) -> int:
    """Standard Modbus CRC-16/MODBUS (poly 0xA001, init 0xFFFF, no final XOR).

    ``data`` may be bytes, bytearray, or any iterable of ints 0..255.
    Returns a 16-bit integer.
    """
    crc = 0xFFFF
    for b in data:
        crc ^= b & 0xFF
        for _ in range(8):
            if crc & 0x0001:
                crc = (crc >> 1) ^ 0xA001
            else:
                crc >>= 1
    return crc & 0xFFFF


def bit_reverse16(value: int) -> int:
    """Reverse all 16 bits of ``value`` (bit 0 <-> bit 15, ...)."""
    result = 0
    for _ in range(16):
        result = (result << 1) | (value & 1)
        value >>= 1
    return result & 0xFFFF


def crc_siebwalde_on_wire(payload) -> int:
    """Return the Siebwalde on-wire CRC word for ``payload``.

    The returned 16-bit word is ``bit_reverse16(crc16_modbus(payload))``.
    On the wire its low byte is transmitted first, then its high byte.
    """
    return bit_reverse16(crc16_modbus(payload))


def crc_siebwalde_bytes(payload):
    """Return the two on-wire CRC bytes ``(lo, hi)`` for ``payload``.

    ``lo`` is transmitted first (low byte of the bit-reversed word), ``hi``
    second (high byte of the bit-reversed word).
    """
    w = crc_siebwalde_on_wire(payload)
    return w & 0xFF, (w >> 8) & 0xFF


def verify_crc_siebwalde(payload, crc_lo: int, crc_hi: int) -> bool:
    """Check two received CRC bytes against the Siebwalde convention.

    ``crc_lo`` is the first (low) CRC byte on the wire, ``crc_hi`` the second.
    """
    exp_lo, exp_hi = crc_siebwalde_bytes(payload)
    return (crc_lo == exp_lo) and (crc_hi == exp_hi)


def verify_crc_standard(payload, crc_lo: int, crc_hi: int) -> bool:
    """Check two received CRC bytes against the standard Modbus convention
    (low byte first, no bit reversal).  Provided as a diagnostic to prove which
    convention the captured traffic actually uses."""
    std = crc16_modbus(payload)
    return (crc_lo == (std & 0xFF)) and (crc_hi == ((std >> 8) & 0xFF))
