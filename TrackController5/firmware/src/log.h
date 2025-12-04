#ifndef LOG_H
#define LOG_H

#include <stdint.h>
#include <stdbool.h>
#include <stddef.h>

/*
 * Simple TCP logger for PIC32 + Harmony 2 (C89 compatible).
 *
 * Usage:
 *  - Call LOG_Init() once, after the TCP/IP stack is ready
 *      (TCPIP_STACK_Status(...) == SYS_STATUS_READY).
 *  - Call LOG_Task() regularly from your main application task.
 *  - Use LOG_Push("text") or LOG_Printf("x=%d\r\n", x) to queue log messages.
 *
 * Optional:
 *  - You can redirect SYS_MESSAGE to this logger in a central header:
 *
 *      #include "system/debug/sys_debug.h"
 *      #include "log.h"
 *      #undef  SYS_MESSAGE
 *      #define SYS_MESSAGE(x)   LOG_Push(x)
 *
 *  - LOG_Push is short and safe to call from interrupt context.
 *  - LOG_Task does the TCP work and must run in normal task context.
 */

/*** Configuration ***/

// Maximum length of a single log message (including terminating '\0',
// excluding TCP/IP overhead).
#ifndef LOG_MSG_MAX_LEN
#define LOG_MSG_MAX_LEN        250
#endif

// Number of log messages in the ring buffer.
#ifndef LOG_QUEUE_LENGTH
#define LOG_QUEUE_LENGTH       64
#endif

// TCP port for the logger (must not conflict with your application protocol).
#ifndef LOG_TCP_PORT
#define LOG_TCP_PORT           50000
#endif

#ifdef __cplusplus
extern "C" {
#endif

// Initialize the logger and open the TCP server socket.
// Call after TCP/IP stack status is SYS_STATUS_READY.
void LOG_Init(void);

// Task function: sends queued log messages to a connected TCP client.
// Call regularly from your main application task.
void LOG_Task(void);

// Simple API: push a C-string into the log queue.
// Automatically appends "\r\n" to the message.
void LOG_Push(const char *str);

// Formatted API, similar to printf.
// Automatically appends "\r\n" to the formatted message.
void LOG_Printf(const char *fmt, ...);

// Optional: check if a TCP client is currently connected.
bool LOG_IsClientConnected(void);

#ifdef __cplusplus
}
#endif

#endif // LOG_H
