#ifndef CTRL_VERSION_H
#define CTRL_VERSION_H
/* Single authoritative committed firmware version for TrackController5.
 * No generated source: tools/firmware/build-firmware.ps1 records these values
 * into the build manifest. */
#define CTRL_FW_MAJOR 1u
#define CTRL_FW_MINOR 0u
#define CTRL_FW_PATCH 0u
#define CTRL_PROTOCOL_VERSION 0x01u
/* Compile-time build timestamp string (null-terminated). */
#define CTRL_BUILD_DATE __DATE__ " " __TIME__
#endif
