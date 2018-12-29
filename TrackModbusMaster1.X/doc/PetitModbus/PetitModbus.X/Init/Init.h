#ifndef _INIT_H
#define _INIT_H

// Crystal: 5MHz
#define FOSC                20000000UL                                          // Fosc=20MHz
#define FCY                 5000000UL                                           // Clk= 5MIPS
#define FTMR1               1000                                                // Hz
#define BAUD_RATE           57600                                               // Baudrate

/*-------------------------------OUTPUTS--------------------------------------*/
#define LED0                LATCbits.LATC1
#define LED1                LATCbits.LATC2
#define LED2                LATCbits.LATC3         
#define LED3                LATCbits.LATC4
/******************************************************************************/

/*-------------------------------DEFINES--------------------------------------*/
extern void InitOSC(void);
extern void InitIO(void);
extern void InitUART(void);
extern void InitTMR1(void);

#endif
