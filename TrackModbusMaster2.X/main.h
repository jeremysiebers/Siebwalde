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

#define True 1
#define False 0
    
#define NUMBER_OF_SLAVES 5                                                      // 0 is for the master self. 5 backplane slaves + 50 Track slaves => 56  

    extern unsigned int UpdateNextSlave;
    void Led_Blink (void);
    
#ifdef	__cplusplus
}
#endif

#endif	/* MAIN_H */

