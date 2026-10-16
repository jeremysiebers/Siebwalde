/* 
 * File:   main.h
 * Author: Jeremy Siebers
 *
 * Created on June 27, 2018, 9:49 PM
 */

#ifndef MAIN_H
#define	MAIN_H
#include <stdint.h>
#include <stdbool.h>

#ifdef	__cplusplus
extern "C" {
#endif

//#define _XTAL_FREQ 32000000                                                     // Fosc  frequency for _delay()  library

void Led_Blink (void);
extern volatile uint8_t Update_AmplifierTicks;
uint16_t    ReadFlashChecksum(void);
uint8_t     Led_Disco       (void);
void        Led_Convert     (uint8_t Number);
bool Amplifier_IsBootloaderWrite(uint8_t frame_address, uint16_t addr, uint16_t value);
bool Amplifier_TryBootloaderWrite(uint8_t frame_address, uint16_t addr, uint16_t value);

#ifdef	__cplusplus
}
#endif

#endif	/* MAIN_H */

