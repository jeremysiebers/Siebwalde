#ifndef _INIT_H
#define _INIT_H

// Crystal: 5MHz
#define FOSC                20000000UL                                          // Fosc=20MHz
#define FCY                 5000000UL                                           // Clk= 5MIPS
#define FTMR1               1000                                                // Hz
#define BAUD_RATE           125000                                              // Baudrate


/*-------------------------------DEFINES--------------------------------------*/
extern void InitUART(void);
extern void InitTMR1(void);

#endif
