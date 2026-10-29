#ifndef AMP_GENERAL_H
#define AMP_GENERAL_H
#ifndef AMP_HOST_TEST
#include <xc.h>
#include "Init.h"
#include "Interrupts.h"
#include "PetitModbus.h"
#endif
#include "../version.h"
/* Frozen R8 map. Pure wire definitions, usable by the host-shim tests. */
#define HR_COMMAND 0u
#define HR_CONTROL 1u
#define HR_ADDRESS 2u
#define HR_CFG_RAMP 3u
#define HR_CFG_FAST_STOP 4u
#define HR_CFG_WATCHDOG 5u
#define HR_CFG_TAG 6u
#define HR_IDENTITY 7u
#define HR_STATE 8u
#define HR_FAULT 9u
#define HR_VALIDITY 10u
#define HR_SW_CHECKSUM 11u
#define HR_REQUESTED_PWM 12u
#define HR_TARGET_PWM 13u
#define HR_APPLIED_PWM 14u
#define HR_WRITE_RESULT 15u
#define HR_MEAS_VOLTAGE 16u
#define HR_MEAS_TEMPERATURE 17u
#define HR_MEAS_CURRENT 18u
#define HR_MSGS_RX 19u
#define HR_MSGS_TX 20u
#define HR_FW_VERSION 21u
#define HR_FW_VERSION_PATCH_PROTO 22u
#define HR_TRACK_POWER 23u
#define TRACK_POWER_ENABLE 0x0001u
#define HR_PWM_SETPOINT_MASK 0x03FFu
#define HR_PWM_DIR_BIT 0x0400u
#define HR_PWM_BRAKE_BIT 0x0800u
#define HR_PWM_STOP_BIT 0x1000u
#define HR_PWM_EMO_BIT 0x8000u
#define HR_CONTROL_ENABLE 0x8000u
#define HR_CONTROL_LEASE 0x4000u
#define HR_CONTROL_APPLY 0x2000u
#define HR_CONTROL_CLEAR 0x1000u
#define HR_CONTROL_RESET 0x0800u
#define HR_CONTROL_DISABLE 0x0400u
extern volatile unsigned int Timer1_Tick_Counter;
#endif
