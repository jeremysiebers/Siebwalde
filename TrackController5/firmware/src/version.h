#ifndef CTRL_VERSION_H
#define CTRL_VERSION_H
/* Single authoritative committed firmware version for TrackController5.
 * No generated source: tools/firmware/build-firmware.ps1 records these values
 * into the build manifest. */
#define CTRL_FW_MAJOR 1u
#define CTRL_FW_MINOR 0u
#define CTRL_FW_PATCH 1u
#define CTRL_PROTOCOL_VERSION 0x01u
/* Deterministic release timestamp string (null-terminated). Two clean builds of
 * identical source must produce identical images, so this MUST NOT use
 * __DATE__/__TIME__. Bump this value when the release timestamp should change. */
#define CTRL_RELEASE_TIMESTAMP "2026-10-26"
#endif
