#ifndef AMP_VERSION_H
#define AMP_VERSION_H
/* Single authoritative committed firmware version for TrackAmplifier4.X.
 * No generated source: tools/firmware/build-firmware.ps1 records these values
 * into the build manifest. MAP_VERSION stays 0x01 (21->23 is additive). */
#define AMP_FW_MAJOR 1u
#define AMP_FW_MINOR 0u
#define AMP_FW_PATCH 2u
#define AMP_PROTOCOL_VERSION 0x01u
#define AMP_MAP_VERSION 0x01u
#define AMP_DEVICE_CLASS 1u
#define AMP_CAPABILITY 0xFu
/* HR7 identity: 12..15 capability, 8..11 device class, 0..7 map version. */
#define AMP_IDENTITY ((AMP_CAPABILITY << 12) | (AMP_DEVICE_CLASS << 8) | AMP_MAP_VERSION)
#endif
