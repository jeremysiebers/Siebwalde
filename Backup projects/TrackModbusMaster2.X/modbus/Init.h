#ifndef _INIT_H
#define _INIT_H

// Crystal: 5MHz
#define FOSC                32000000UL                                          // Fosc=20MHz
#define BAUD_RATE           125000                                              // Baudrate

/*-------------------------------DEFINES--------------------------------------*/
extern void InitUART(void);
extern void InitTMR(void);

#endif
