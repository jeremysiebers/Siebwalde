#ifndef _GENERAL_H
#define _GENERAL_H

#include <xc.h>
#include "Init.h"
#include "Interrupts.h"
#include "PetitModbus.h"

/*
 * Global timer tick counter (used by various timing routines).
 */
extern volatile unsigned int Timer1_Tick_Counter;

/*
 * Modbus Track Slave Holding Register mapping
 * (see ModBusSlaveMapping.txt, "Modbus Track Slave Data Register mapping")
 *
 * NOTE:
 *  - The indices below are zero-based and map directly to PetitHoldingRegisters[x].
 *  - HoldingRegN in the documentation corresponds to PetitHoldingRegisters[HR_xxx]
 *    where HR_xxx == N.
 */

/* ----------------------------- RUNNING PARAMETERS -------------------------- */

/* HoldingReg0: PWM command word */
#define HR_PWM_COMMAND                  0u  /* HoldingReg0: PWM setpoint, direction, brake, EMO stop */

/* HoldingReg1: BEMF speed / control word */
#define HR_BEMF_CTRL                    1u  /* HoldingReg1: BEMF speed setpoint and control flags */

/* HoldingReg2: Status / feedback */
#define HR_STATUS                       2u  /* HoldingReg2: Back-EMF feedback and status bits */

/* HoldingReg3: Amplifier status */
#define HR_AMP_STATUS                   3u  /* HoldingReg3: Amplifier status word */

/* HoldingReg4..6: Measured values */
#define HR_FUSE_VOLTAGE                 4u  /* HoldingReg4: H-bridge fuse voltage */
#define HR_HBRIDGE_TEMPERATURE          5u  /* HoldingReg5: H-bridge temperature */
#define HR_HBRIDGE_CURRENT              6u  /* HoldingReg6: H-bridge current */

/* HoldingReg7..8: Message counters (optional, can be updated by application) */
#define HR_MESSAGES_RECEIVED            7u  /* HoldingReg7: Messages received from master (non-broadcast) */
#define HR_MESSAGES_SENT                8u  /* HoldingReg8: Messages sent to master */

/* ----------------------------- CONFIG PARAMETERS --------------------------- */

/* HoldingReg9: Amplifier configuration */
#define HR_CONFIG_ID_PWM                9u  /* HoldingReg9: Amplifier ID, PWM mode, reset */

/* HoldingReg10: Accel / Decel parameters */
#define HR_ACCEL_PARAMS                10u  /* HoldingReg10: Acceleration / deceleration parameters */

/* HoldingReg11: Firmware checksum */
#define HR_SW_CHECKSUM                 11u  /* HoldingReg11: Firmware checksum / flash check value */

/* -------------------------------------------------------------------------- */
/* HR_PWM_COMMAND (HoldingReg0) bit layout                                    */
/* -------------------------------------------------------------------------- */

/* Bits 0..9: PWM duty cycle / speed command */
#define HR_PWM_SETPOINT_MASK        0x03FFu

/* Status / control bits for PWM */
#define HR_PWM_DIR_BIT              (1u << 10)  /* Bit 10: PWM direction (Fwd/Bwd) */
#define HR_PWM_BRAKE_BIT            (1u << 11)  /* Bit 11: Brake (H-bridge outputs enable) */
/* Bits 12..14 unused for track amplifier */
#define HR_PWM_EMO_BIT              (1u << 15)  /* Bit 15: EMO stop */

/* -------------------------------------------------------------------------- */
/* HR_BEMF_CTRL (HoldingReg1) bit layout                                      */
/* -------------------------------------------------------------------------- */

/* Bits 0..9: BEMF speed setpoint (ADC / km/h scaled) */
#define HR_BEMF_SETPOINT_MASK       0x03FFu

#define HR_BEMF_CSREG_ENABLE_BIT      (1u << 10)  /* Bit 10: Constant speed regulation enable */
#define HR_BEMF_CLEAR_AMP_STATUS_BIT  (1u << 11)  /* Bit 11: Clear amplifier status */
#define HR_BEMF_CLEAR_MSG_BUFFER_BIT  (1u << 12)  /* Bit 12: Clear message buffer */
/* Bits 13..14 currently unused for track amplifier */
#define HR_BEMF_ENABLE_AMPLIFIER_BIT  (1u << 15)  /* Bit 15: Enable (start) amplifier */

/* -------------------------------------------------------------------------- */
/* HR_STATUS (HoldingReg2) bit layout helpers                                 */
/* -------------------------------------------------------------------------- */

/* Bits 0..9: Back-EMF measurement (ADC units) */
#define HR_STATUS_BEMF_MASK         0x03FFu

/* For ConfigSlave usage: lower 6 bits contain the ConfigSlave ID */
#define HR_STATUS_CONFIG_ID_MASK    0x003Fu

/* Status bits (see ModBusSlaveMapping.txt) */
#define HR_STATUS_OCCUPIED_BIT      (1u << 10)  /* Bit 10: track occupied flag (TODO: implement when occupancy source known) */
#define HR_STATUS_THERMAL_BIT       (1u << 11)  /* Bit 11: H-bridge thermal flag (LM_THFLG) */
#define HR_STATUS_OVERCURRENT_BIT   (1u << 12)  /* Bit 12: overcurrent detected (TODO: implement when OC source known) */
#define HR_STATUS_ID_SET_BIT        (1u << 13)  /* Bit 13: amplifier ID programmed by master */

/* Bits 14..15 currently unused */

#endif /* _GENERAL_H */
