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
    MBUS_EV_PURGE     = 4    /* mailbox purged by a global-stop latch         */
} MBUS_EVENT_TYPE;

void MBUS_EVENT_PUSH(uint8_t type, uint8_t slave, uint8_t reg, uint16_t val, uint8_t rv);
void MBUS_EVENT_REPORT(void);

#endif /* MBUS_EVENT_H */
