#ifndef PIN_MANAGER_H
#define PIN_MANAGER_H

#define INPUT   1
#define OUTPUT  0

#define HIGH    1
#define LOW     0

#define ANALOG      1
#define DIGITAL     0

#define PULL_UP_ENABLED      1
#define PULL_UP_DISABLED     0

// get/set PWM_SYNC aliases
#define PWM_SYNC_TRIS               TRISB0
#define PWM_SYNC_LAT                LATB0
#define PWM_SYNC_PORT               PORTBbits.RB0
#define PWM_SYNC_WPU                WPUB0
#define PWM_SYNC_ANS                ANSB0
#define PWM_SYNC_SetHigh()    do { LATB0 = 1; } while(0)
#define PWM_SYNC_SetLow()   do { LATB0 = 0; } while(0)
#define PWM_SYNC_Toggle()   do { LATB0 = ~LATB0; } while(0)
#define PWM_SYNC_GetValue()         PORTBbits.RB0
#define PWM_SYNC_SetDigitalInput()    do { TRISB0 = 1; } while(0)
#define PWM_SYNC_SetDigitalOutput()   do { TRISB0 = 0; } while(0)

#define PWM_SYNC_SetPullup()    do { WPUB0 = 1; } while(0)
#define PWM_SYNC_ResetPullup()   do { WPUB0 = 0; } while(0)
#define PWM_SYNC_SetAnalogMode()   do { ANSB0 = 1; } while(0)
#define PWM_SYNC_SetDigitalMode()   do { ANSB0 = 0; } while(0)
// get/set PWM aliases
#define PWM_TRIS               TRISC3
#define PWM_LAT                LATC3
#define PWM_PORT               PORTCbits.RC3
#define PWM_ANS                ANSC3
#define PWM_SetHigh()    do { LATC3 = 1; } while(0)
#define PWM_SetLow()   do { LATC3 = 0; } while(0)
#define PWM_Toggle()   do { LATC3 = ~LATC3; } while(0)
#define PWM_GetValue()         PORTCbits.RC3
#define PWM_SetDigitalInput()    do { TRISC3 = 1; } while(0)
#define PWM_SetDigitalOutput()   do { TRISC3 = 0; } while(0)

#define PWM_SetAnalogMode()   do { ANSC3 = 1; } while(0)
#define PWM_SetDigitalMode()   do { ANSC3 = 0; } while(0)
// get/set TX1 aliases
#define TX1_TRIS               TRISC6
#define TX1_LAT                LATC6
#define TX1_PORT               PORTCbits.RC6
#define TX1_ANS                ANSC6
#define TX1_SetHigh()    do { LATC6 = 1; } while(0)
#define TX1_SetLow()   do { LATC6 = 0; } while(0)
#define TX1_Toggle()   do { LATC6 = ~LATC6; } while(0)
#define TX1_GetValue()         PORTCbits.RC6
#define TX1_SetDigitalInput()    do { TRISC6 = 1; } while(0)
#define TX1_SetDigitalOutput()   do { TRISC6 = 0; } while(0)

#define TX1_SetAnalogMode()   do { ANSC6 = 1; } while(0)
#define TX1_SetDigitalMode()   do { ANSC6 = 0; } while(0)
// get/set RX1 aliases
#define RX1_TRIS               TRISC7
#define RX1_LAT                LATC7
#define RX1_PORT               PORTCbits.RC7
#define RX1_ANS                ANSC7
#define RX1_SetHigh()    do { LATC7 = 1; } while(0)
#define RX1_SetLow()   do { LATC7 = 0; } while(0)
#define RX1_Toggle()   do { LATC7 = ~LATC7; } while(0)
#define RX1_GetValue()         PORTCbits.RC7
#define RX1_SetDigitalInput()    do { TRISC7 = 1; } while(0)
#define RX1_SetDigitalOutput()   do { TRISC7 = 0; } while(0)

#define RX1_SetAnalogMode()   do { ANSC7 = 1; } while(0)
#define RX1_SetDigitalMode()   do { ANSC7 = 0; } while(0)
// get/set BRAKE aliases
#define BRAKE_TRIS               TRISD0
#define BRAKE_LAT                LATD0
#define BRAKE_PORT               PORTDbits.RD0
#define BRAKE_ANS                ANSD0
#define BRAKE_SetHigh()    do { LATD0 = 1; } while(0)
#define BRAKE_SetLow()   do { LATD0 = 0; } while(0)
#define BRAKE_Toggle()   do { LATD0 = ~LATD0; } while(0)
#define BRAKE_GetValue()         PORTDbits.RD0
#define BRAKE_SetDigitalInput()    do { TRISD0 = 1; } while(0)
#define BRAKE_SetDigitalOutput()   do { TRISD0 = 0; } while(0)

#define BRAKE_SetAnalogMode()   do { ANSD0 = 1; } while(0)
#define BRAKE_SetDigitalMode()   do { ANSD0 = 0; } while(0)
// get/set CCP4 aliases
#define CCP4_TRIS               TRISD1
#define CCP4_LAT                LATD1
#define CCP4_PORT               PORTDbits.RD1
#define CCP4_ANS                ANSD1
#define CCP4_SetHigh()    do { LATD1 = 1; } while(0)
#define CCP4_SetLow()   do { LATD1 = 0; } while(0)
#define CCP4_Toggle()   do { LATD1 = ~LATD1; } while(0)
#define CCP4_GetValue()         PORTDbits.RD1
#define CCP4_SetDigitalInput()    do { TRISD1 = 1; } while(0)
#define CCP4_SetDigitalOutput()   do { TRISD1 = 0; } while(0)

#define CCP4_SetAnalogMode()   do { ANSD1 = 1; } while(0)
#define CCP4_SetDigitalMode()   do { ANSD1 = 0; } while(0)

/**
 * @Param
    none
 * @Returns
    none
 * @Description
    GPIO and peripheral I/O initialization
 * @Example
    PIN_MANAGER_Initialize();
 */
void PIN_MANAGER_Initialize(void);

/**
 * @Param
    none
 * @Returns
    none
 * @Description
    Interrupt on Change Handling routine
 * @Example
    PIN_MANAGER_IOC();
 */
void PIN_MANAGER_IOC(void);

#endif // PIN_MANAGER_H
/**
 End of File
 */