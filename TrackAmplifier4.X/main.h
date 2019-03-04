/* 
 * File:   main.h
 * Author: Jeremy Siebers
 *
 * Created on June 27, 2018, 9:49 PM
 */

#ifndef MAIN_H
#define	MAIN_H

#ifdef	__cplusplus
extern "C" {
#endif

//#define _XTAL_FREQ 32000000                                                     // Fosc  frequency for _delay()  library

void Led_Blink (void);
extern unsigned int Update_Amplifier;
uint16_t    ReadFlashChecksum(void);
uint8_t     Led_Disco       (void);
void        Led_Convert     (uint8_t Number);

#ifdef	__cplusplus
}
#endif

#endif	/* MAIN_H */

