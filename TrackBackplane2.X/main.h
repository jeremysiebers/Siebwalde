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

void        Get_ID          (void);
void        Set_Amplifier   (void);
void        Led_Blink       (void);
uint8_t     Led_Disco       (void);
void        Led_Convert     (uint8_t Number);

#ifdef	__cplusplus
}
#endif

#endif	/* MAIN_H */

