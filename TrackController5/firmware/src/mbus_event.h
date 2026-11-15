#ifndef MBUS_EVENT_H
#define MBUS_EVENT_H

#include <stdint.h>

/* P2-C fault-B diagnostic event capture.
 *
 * Lightweight bounded ring: MBUS_EVENT_PUSH is safe in the Modbus
 * communication path (no printf, no vsnprintf, ~O(1) struct copy with a
 * few bytes of stack). MBUS_EVENT_REPORT is deferred to a shallow call
 * point and formats the events via LOG_Printf there.
 *
 * The prior instrumentation used LOG_Printf/vsnprintf directly in the deep
 * MBUS call chain (SYS_Tasks -> MBUS_Tasks -> PROCESSxNEXTxSLAVE ->
 * SendNextMessage -> SLAVExCOMMUNICATIONxHANDLER), which overflowed the 1 KiB
 * PIC32 stack (LOG_Printf carries a 250-byte buffer plus vsnprintf internals
 * and LOG_Push a further 250-byte buffer). */

typedef enum {
    MBUS_EV_ENQUEUE   = 1,   /* command-108 accepted into mailbox            */
    MBUS_EV_GATE_DROP = 2,   /* write rejected by the compatibility gate      */
    MBUS_EV_FC16_SENT = 3,   /* FC16 write dispatched (rv = SEND result)      */
    MBUS_EV_PURGE     = 4,   /* mailbox purged by a global-stop latch         */
    MBUS_EV_TX_DONE   = 5,   /* FC16 bytes physically sent (TX -> WAIT_ANSWER) */
    MBUS_EV_ACK       = 6,   /* valid FC16 echo received (MbCommError=OK)     */
    MBUS_EV_TIMEOUT   = 7,   /* no echo (MbCommError=TIMEOUT)                 */
    MBUS_EV_EXCEPTION = 8    /* correlated Modbus exception (MbCommError=EXCEPTION) */
} MBUS_EVENT_TYPE;

/* MBUS_EV_EXCEPTION payload encoding (PV07-C):
 *
 * A Modbus exception response is [slave][respFn|0x80][exceptionCode][crc lo][crc hi].
 * MBUS_EVENT_PUSH(type, slave, reg, val, rv) carries, for MBUS_EV_EXCEPTION:
 *   slave = response slave address (must equal the outstanding request address)
 *   reg   = ORIGINAL request function code, normalized from the exception fn
 *           (e.g. 0x10 = FC16), i.e. respFn & 0x7F
 *   rv    = the exception RESPONSE function code (e.g. 0x90 = FC16|0x80)
 *   val   = the Modbus exception code (e.g. 0x03 = illegal data value, which the
 *           PIC18 uses for AMP_WRITE_RELATIONSHIP / GROUP_READY rejection)
 *
 * This event is reported by MBUS_EVENT_REPORT() in slavecommhandler.c through the
 * LOG_Printf path, which reaches the PIC32 TCP diagnostic log (port 50000).
 * It is NOT carried on the UDP host protocol (a known, accepted limitation for
 * this increment; not changed here). */

void MBUS_EVENT_PUSH(uint8_t type, uint8_t slave, uint8_t reg, uint16_t val, uint8_t rv);
void MBUS_EVENT_REPORT(void);

#endif /* MBUS_EVENT_H */
