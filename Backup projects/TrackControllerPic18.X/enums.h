/* 
 * File:   enums.h
 * Author: jerem
 *
 * Created on 12 december 2023, 20:35
 */

#ifndef ENUMS_H
#define	ENUMS_H

#ifdef	__cplusplus
extern "C" {
#endif

    #include <stdbool.h>
    
    /* factor to be set according to set timer interrupt value to calc seconds */
    const uint32_t tFactorSec = 1000; // timer set to 1ms, 10 * 1000 = 1 second
    /* 5 seconds wait time after point switch set the signal */
    const uint32_t tSignalSwitchWaitTime = (uint32_t)(5 * tFactorSec);
    /* Time to wait for servo to move to new position */
    const uint32_t tSwitchPointWaitTime = (uint32_t)(5 * tFactorSec);
    /* Time that Train waits before leaving */
    const uint32_t tTrainWaitTime = (uint32_t)(30 * tFactorSec);
    /* Time that Freight Train waits before leaving */
    const uint32_t tFreightTrainWaitTime = (uint32_t)(5 * tFactorSec);
    /* Boot wait time to get all IO read and debounced first */
    const uint32_t tReadIoSignalWaitTime = (uint32_t)(2 * tFactorSec);
    /* Time to wait after outbound, train fully left the station */
    const uint32_t tOutboundWaitTime = (uint32_t)(10 * tFactorSec);

//    enum STATES{
////        INIT,
////        INIT2,
////        RUN,
////        WAIT,
////        IDLE,
////
////        STN_TO_SIEBWALDE,
////        SIEBWALDE_TO_STN,
////        SIEBWALDE_SWITCHT4T5,
//
////        HNDL_IDLE,
////        HNDL_INBOUND,
////        HNDL_OUTBOUND,
////        HNDL_PASSING,
////        HNDL_WAIT_BLK_OUT,
//
////        STN_INBOUND,
////        STN_OUTBOUND,
////        STN_PASSING,
////        STN_WAIT,
////        STN_IDLE,
//
////        SEQ_IDLE,
////        SEQ_WAIT,
////        SEQ_SET_OCC,
////        SEQ_CHK_TRAIN,
////        SEQ_CHK_PASSED,
////        SEQ_OUTBOUND_LEFT_STATTION,
////
////        SIG_RED,
////        SIG_GREEN,
//    };

    typedef struct
    {
        volatile unsigned char      *portx_ptr;                                     // Reference to the input port used
        uint8_t                     pin_mask;                                       // Mask to point to pin used of port
    }OCC, REL;

    typedef struct
    {
        volatile unsigned char      *port1_ptr;                                     // Reference to the input port used
        uint8_t                     pin1_mask;                                      // Mask to point to pin used of port
        volatile unsigned char      *port2_ptr;                                     // Reference to the input port used
        uint8_t                     pin2_mask;                                      // Mask to point to pin used of port
        volatile unsigned char      *port3_ptr;                                     // Reference to the input port used
        uint8_t                     pin3_mask;                                      // Mask to point to pin used of port
        volatile unsigned char      *port4_ptr;                                     // Reference to the input port used
        uint8_t                     pin4_mask;                                      // Mask to point to pin used of port
        volatile unsigned char      *port5_ptr;                                     // Reference to the input port used
        uint8_t                     pin5_mask;                                      // Mask to point to pin used of port
        volatile unsigned char      *port6_ptr;                                     // Reference to the input port used
        uint8_t                     pin6_mask;                                      // Mask to point to pin used of port

    }WS, SIG;
    
    typedef enum
    {
        MAIN_LOOP                                                   = 01,
        MAIN_STATION_TOP                              				= 10,
        MAIN_STATION_BOT                              				= 20,
        WALDSEE                                                     = 30,
        SIEBWALDE                                                   = 40,
        WALDBERG                                                    = 50,
    } TASK_ID;				

    typedef enum		
    {
        HEADER                                                      = 0xAA,
        ALIVE                                                       = 0x55,
        /* FW HANDLER COMMANDS */
        STNTRACK1                                                   = 31,
        STNTRACK2                                                   = 32,
        STNTRACK3                                                   = 33,
        MTNTRACK1                                                   = 34,
        MTNTRACK2                                                   = 35,
        MTNTRACK7                                                   = 36,
        MTNTRACK8                                                   = 37,
        
    } TASK_COMMAND;
    
    typedef enum				
    {				
        busy,
        connected,
        done,
        command,
        nop,
        
        INIT,
        INIT2,
        RUN,
        WAIT,
        IDLE,

        STN_TO_SIEBWALDE,
        SIEBWALDE_TO_STN,
        SIEBWALDE_SWITCHT4T5,
        
        STN_INBOUND,
        STN_OUTBOUND,
        STN_PASSING,
        STN_WAIT,
        STN_IDLE,
                
        SEQ_IDLE,
        SEQ_WAIT,
        SEQ_SET_OCC,
        SEQ_CHK_TRAIN,
        SEQ_CHK_PASSED,
        SEQ_OUTBOUND_LEFT_STATTION,

        SIG_RED,
        SIG_GREEN,
                
        INVERT,
        
    } TASK_STATE;		

    typedef enum
    {
       NONE    = 100,
       T1      = 101,
       T2      = 102,
       T3      = 103,
       T4      = 104,
       T5      = 105,
       T7      = 107,
       T8      = 108,
       TRACK1  = 110,
       TRACK2  = 111,
       TRACK3  = 112,
       TRACK10 = 113,
       TRACK11 = 114,
       TRACK12 = 115,       
       
    } TASK_MESSAGES;


#ifdef	__cplusplus
}
#endif

#endif	/* ENUMS_H */

