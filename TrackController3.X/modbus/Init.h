#ifndef _INIT_H
#define _INIT_H

// Crystal: 5MHz
#define FOSC                41666700UL                                          // Fosc=41.6667MHz
#define BAUD_RATE           125000                                              // Baudrate (SPBRG = 82 --> Baudrate actual = 125502)

/*-------------------------------DEFINES--------------------------------------*/
extern void InitUART(void);
extern void InitTMR(void);

#endif
