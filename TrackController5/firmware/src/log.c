#include "log.h"

#include <string.h>
#include <stdarg.h>
#include <stdio.h>

#include "system_config.h"
#include "system_definitions.h"

/*** Internal structures / buffers ***/

typedef struct
{
    uint16_t len;                       // length without '\0', may include "\r\n"
    char     msg[LOG_MSG_MAX_LEN];      // always '\0'-terminated
} LOG_Message_t;

// Ring buffer indices (head = write, tail = read)
static volatile uint8_t   logHead = 0;
static volatile uint8_t   logTail = 0;
static LOG_Message_t      logQueue[LOG_QUEUE_LENGTH];

// TCP server socket used for logging
static TCP_SOCKET         logSocket = INVALID_SOCKET;

/*** Internal helpers ***/

// Push a message into the ring buffer.
// 'str' must be a valid pointer, 'len' is the number of bytes to copy
// (not including any '\0'). This function is safe to use from interrupts.
static void LOG_QueuePush(const char *str, size_t len)
{
    uint8_t nextHead;
    LOG_Message_t *slot;
    size_t i;
    bool intState;

    // Compute next head index
    nextHead = (uint8_t)((logHead + 1U) % LOG_QUEUE_LENGTH);
    if (nextHead == logTail)
    {
        // Queue is full: drop this message
        return;
    }

    slot = &logQueue[logHead];

    // Limit length to fit into the buffer (reserve 1 byte for '\0')
    if (len >= (LOG_MSG_MAX_LEN - 1U))
    {
        len = LOG_MSG_MAX_LEN - 1U;
    }

    // Copy into RAM buffer (source may be const / in flash)
    for (i = 0U; i < len; i++)
    {
        slot->msg[i] = str[i];
    }

    // Add '\0' terminator
    slot->msg[len] = '\0';
    slot->len      = (uint16_t)len;

    // Atomically update head (protect against concurrent interrupt access)
    intState = SYS_INT_Disable();
    logHead = nextHead;
    if (intState)
    {
        SYS_INT_Enable();
    }
}

// Pop a message from the ring buffer.
// Returns true if a message was available, false if queue is empty.
// 'out' receives a copy of the message.
static bool LOG_QueuePop(LOG_Message_t *out)
{
    bool intState;

    if (logTail == logHead)
    {
        // Queue is empty
        return false;
    }

    // Copy element to output
    *out = logQueue[logTail];

    // Atomically update tail
    intState = SYS_INT_Disable();
    logTail = (uint8_t)((logTail + 1U) % LOG_QUEUE_LENGTH);
    if (intState)
    {
        SYS_INT_Enable();
    }

    return true;
}

/*** Public functions ***/

void LOG_Init(void)
{
    // Open a TCP server on LOG_TCP_PORT, accept connections from any remote.
    logSocket = TCPIP_TCP_ServerOpen(IP_ADDRESS_TYPE_IPV4, LOG_TCP_PORT, 0);

    // If this returns INVALID_SOCKET, the stack had no free TCP sockets.
    // You may want to handle that case separately if needed.
//    if (logSocket == INVALID_SOCKET)
//    {
//        SYS_MESSAGE("LOG: failed to open TCP server socket");
//    }
//    else
//    {
//        SYS_MESSAGE("LOG: TCP server socket opened");
//    }
}

bool LOG_IsClientConnected(void)
{
    if (logSocket == INVALID_SOCKET)
    {
        return false;
    }

    if (TCPIP_TCP_IsConnected(logSocket))
    {
        return true;
    }

    return false;
}

void LOG_Task(void)
{
    LOG_Message_t msg;
    uint16_t space;

    if (logSocket == INVALID_SOCKET)
    {
        // Logger not initialized or no free socket
        return;
    }

    if (!TCPIP_TCP_IsConnected(logSocket))
    {
        // No client connected; do not send anything
        return;
    }

    // Send all queued messages while there is space in the TCP TX buffer
    while (LOG_QueuePop(&msg))
    {
        space = TCPIP_TCP_PutIsReady(logSocket);
        if (space < msg.len)
        {
            // Not enough space in TCP TX buffer:
            // push the message back and try again later
            LOG_QueuePush(msg.msg, msg.len);
            break;
        }

        TCPIP_TCP_ArrayPut(logSocket, (uint8_t *)msg.msg, msg.len);
    }

    // Actually flush the data out on the network
    TCPIP_TCP_Flush(logSocket);
}

void LOG_Push(const char *str)
{
    size_t len;
    char tmp[LOG_MSG_MAX_LEN];
    size_t i;

    if (str == NULL)
    {
        return;
    }

    // Determine length up to maximum (without extra "\r\n")
    len = 0U;
    while (str[len] != '\0' && len < (LOG_MSG_MAX_LEN - 3U))  // -3 for "\r\n" + '\0'
    {
        len++;
    }

    // Copy original string into a local buffer
    for (i = 0U; i < len; i++)
    {
        tmp[i] = str[i];
    }

    // Append "\r\n" for nice line endings in PuTTY
    tmp[i++] = '\r';
    tmp[i++] = '\n';

    tmp[i] = '\0';

    // Queue the final message
    LOG_QueuePush(tmp, i);
}

void LOG_Printf(const char *fmt, ...)
{
    char buffer[LOG_MSG_MAX_LEN];
    va_list args;

    if (fmt == NULL)
    {
        return;
    }

    va_start(args, fmt);
    (void)vsnprintf(buffer, sizeof(buffer), fmt, args);
    va_end(args);

    LOG_Push(buffer);
}
