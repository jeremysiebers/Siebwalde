/* 
 * File:   main.h
 * Author: Jeremy Siebers
 *
 * Created on June 27, 2018, 9:49 PM
 */

#ifndef MAIN_H
#define	MAIN_H

#include <stdint.h>

#ifdef	__cplusplus
extern "C" {
#endif

#define True 1
#define False 0
    
#define NUMBER_OF_SLAVES 51                                                     // 0 is for the master self. 50 Track slaves => 51  

extern unsigned int UpdateNextSlave;
extern volatile unsigned int LED_ERR;
extern unsigned int InitDone;
void Led_Blink (void);
uint8_t     Led_Disco       (void);
void        Led_Convert     (uint8_t Number);
    
#ifdef	__cplusplus
}
#endif

#endif	/* MAIN_H */

