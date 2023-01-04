/* 
 * File:   communication.h
 * Author: jerem
 *
 * Created on January 1, 2023, 2:16 PM
 */

#ifndef COMMUNICATION_H
#define	COMMUNICATION_H

#ifdef	__cplusplus
extern "C" {
#endif

#include "mcc_generated_files/mcc.h"
    
    typedef enum STATEMACHINE{
                IDLE,
                INIT,
                RUN
    }STM;
    
    typedef enum COMMANDS{
                ENTER = 0x0D,
                DELETE = 0x7F,
                BACKSPACE = 0x08
    }CMD;

    extern void INITxCOMM(void);
    extern uint8_t PROCESSxCOMM(bool done);

    void Init_Menu(void);


#ifdef	__cplusplus
}
#endif

#endif	/* COMMUNICATION_H */

