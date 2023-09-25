/* 
 * File:   milisecond_counter.h
 * Author: jeremy
 *
 * Created on March 14, 2023, 5:57 PM
 */

#ifndef MILISECOND_COUNTER_H
#define	MILISECOND_COUNTER_H

#ifdef	__cplusplus
extern "C" {
#endif
    
    extern void MILLIESxINIT(void);    
    extern uint32_t millis(void);
    
    void Millisecond_DefaultUpdateHandler(void);
    
    void SETxMILLISECONDxUPDATExHANDLER(void (* InterruptHandler)(void));
    extern void (*Millisecond_Update_Handler)(void);
    
    void SETxMILLISECONDxUPDATExHANDLER2(void (* InterruptHandler)(void));
    extern void (*Millisecond_Update_Handler2)(void);

#ifdef	__cplusplus
}
#endif

#endif	/* MILISECOND_COUNTER_H */

