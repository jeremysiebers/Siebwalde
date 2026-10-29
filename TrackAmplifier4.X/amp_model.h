#ifndef AMP_MODEL_H
#define AMP_MODEL_H
#include <stdint.h>
#include <stdbool.h>
#define AMP_NEUTRAL 399u
#define AMP_LEASE_TICKS 294ul /* ceil(500000 / 1704) */
#define AMP_FAULT_CONFIG 0x0010u
#define AMP_FAULT_COMMS 0x0020u
#define AMP_FAULT_LEASE 0x0080u
#define AMP_FAULT_HARD 0x004Fu /* electrical causes + internal invariant */
typedef enum {
    AMP_ST_NOT_INITIALIZED, AMP_ST_READY, AMP_ST_RUN, AMP_ST_STOPPING,
    AMP_ST_FAULT, AMP_ST_ESTOP, AMP_ST_COMMS_LOST, AMP_ST_LEASE_EXPIRED
} AmpState;
typedef struct {
    uint8_t accel_step, decel_step, fast_stop_step;
    uint16_t watchdog_ms, tag;
} AmpConfig;
typedef struct {
    bool enable, dir, brake, stop_request, emo;
    uint16_t pwm;
} AmpCommand;
typedef struct {
    AmpState state;
    bool init_done, config_ok, id_set, enable_active, brake_active,
         dir_active, stopping_active, inhibited, lease_fresh;
    bool track_power_requested;
    uint16_t requested_pwm, target_pwm, applied_pwm;
} AmpStatus;
typedef struct { uint16_t causes, validity; } AmpFault;
typedef struct {
    AmpConfig pending, active;
    AmpCommand command;
    AmpStatus status;
    AmpFault fault;
    uint32_t lease_ticks, comm_watchdog_ticks, comm_timeout_ticks;
    uint16_t ramp_current, prev_control_word, command_echo, write_result;
    uint16_t voltage, temperature, current, bemf, msgs_rx, msgs_tx, checksum;
    uint8_t address;
    bool assign_window, renewal_after_inhibit, fast_stopping;
} AmpModel;
extern AmpModel amp_model;
bool AmpConfig_Validate(const AmpConfig *config);
#endif
