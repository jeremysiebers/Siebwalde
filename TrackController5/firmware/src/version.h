#ifndef CTRL_VERSION_H
#define CTRL_VERSION_H
/* Single authoritative committed firmware version for TrackController5 (L1).
 * L1 = legacy Baseline-0 source + a read-only command-113 semantic version query.
 * No Phase-1 global-stop/heartbeat/motion-lease protocol is implemented, so
 * CTRL_PROTOCOL_VERSION is 0x00 (legacy/unknown), NOT Phase-1's 0x01.
 *
 * CTRL_RELEASE_TIMESTAMP is a DETERMINISTIC RELEASE TAG identifying the L1
 * source/release. It is NOT a runtime compile date: it MUST NOT use
 * __DATE__/__TIME__, so that two clean builds of identical source produce
 * identical images. Bump this value only when the L1 release tag should change. */
#define CTRL_FW_MAJOR 0u
#define CTRL_FW_MINOR 1u
#define CTRL_FW_PATCH 0u
#define CTRL_PROTOCOL_VERSION 0x00u
#define CTRL_RELEASE_TIMESTAMP "2026-10-20"
#endif
